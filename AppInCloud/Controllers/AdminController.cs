using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppInCloud.Models;
using Hangfire;
using AppInCloud.Services;
using System.ComponentModel.DataAnnotations;

namespace AppInCloud.Controllers;

[Authorize(Policy = "admin")]
[Route("/api/v1/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    
    private readonly ILogger<AdminController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    // private readonly UserManager<Models.ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly InstallationService _installationService;
    private readonly AndroidService _androidService;

    public AdminController(ILogger<AdminController> logger, InstallationService installationService, ADB adb, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, AndroidService androidService)
    {
        _installationService = installationService;
        _logger = logger;
        _adb = adb;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _androidService = androidService;
    }


    private long tasksCount(){
        var processing = JobStorage.Current.GetMonitoringApi().ProcessingJobs(0, int.MaxValue).Where(j=>j.Value.ServerId.StartsWith("cuttlefish:")).Count();
        var enqueued = JobStorage.Current.GetMonitoringApi().EnqueuedCount("cuttlefish");
        return processing + enqueued;
    }

    private bool canRunTask(){
        return tasksCount() == 0;
    }
    [HttpGet]
    [Route("Devices")]
    public IActionResult GetDevices()
    {
        var list =  _db.Devices
            .Where(d => d.IsActive)
            .Include(f => f.Users)
            .Select(d => new {Id=d.Id, Status=d.Status, Serial=d.getSerialNumber(), Target=d.Target, Users = d.Users.Select(u => u.Email)})
            .ToList()
            .Select(d => { // todo check the devices concurrently
                var check = _adb.HealthCheck(d.Serial);
                check.Wait();
                return new {Id=d.Id, Status=d.Status, Target=d.Target, Users=d.Users, IsActive = check.Result};
            }); 
            
        return Ok(new {
            Count=tasksCount(),
            List=list,
            targets=new Dictionary<Device.Targets, string>() {
                 {Device.Targets._12_x86_64, "Android 12 x86_64"}, 
                 {Device.Targets._13_x86_64, "Android 13 x86_64"}
                }
        });
    }
    


    [HttpGet]
    [Route("DefaultApps")]
    public IEnumerable<object> getDefaultApps()
    {
        return  _db.DefaultApps.ToList();
    }
    [HttpDelete]
    [Route("DefaultApps/{id}")]
    public IActionResult deleteDefaultApps(int id)
    {
        // var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext!.User);
        var app = _db.DefaultApps.Find(id);
        _db.DefaultApps.Remove(app);
        _db.SaveChanges();
        return Ok(new {});
    }
    
    [HttpPost]
    [RequestSizeLimit(1073741824)] // 1GB
    [Consumes("multipart/form-data")]
    [Route("Upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        AppTypes type = file.FileName.EndsWith(".aab") ? AppTypes.AAB : AppTypes.APK;
        string filePath = await _installationService.CopyInstaller(file);
        string packageName = _androidService.getInstallerPackageName(filePath);
        if(packageName is null) return UnprocessableEntity();

        foreach(Models.Device device in _db.Devices){
            string deviceSerial = device.getSerialNumber();
            var jobId = BackgroundJob.Enqueue<ADB>(job => job.Install(deviceSerial, filePath));
        }

        _db.DefaultApps.Add(new Models.DefaultApp {
            Name = file.FileName,
            PackageName = packageName,
            Type = type,
            InstallerPath = filePath, // todo: save in non-temporal folder
            CreatedTimestamp = DateTime.Now
        });

        await _db.SaveChangesAsync();    
           
        return Ok(new { success=true });
    }
    private IActionResult assignDevice(Device device, ApplicationUser user, bool unassign)
    {
        if(unassign) {
            device!.Users.Remove(user); 
        }else{
            device!.Users.Add(user);
        }
        _db.Devices.Update(device);
        _db.SaveChanges();
        return Ok();
    }

    [HttpPost]
    [Route("Devices/{deviceId}/Assign")]
    public IActionResult assignDevice(string deviceId, [FromForm] [EmailAddress] string userEmail, [FromForm] bool unassign ){

        return (
            _db.Devices.Where(d => d.Id == deviceId).Include(d => d.Users).FirstOrDefault(), 
            _db.Users.Where(u => u.Email == userEmail).FirstOrDefault()
        ) switch {
            (null, _) or (_, null) => NotFound(new {Errors = new [] {"device or user not found"}}),
            (var device, var user) => assignDevice(device, user, unassign)
        };
    }

    private string? restartJob(IEnumerable<Device> devices){
        // var devices = _db.Devices.Where(d => d.IsActive && d.Status == Device.Statuses.ENABLE).ToList();
        var supportedTarget = Device.Targets._13_x86_64;
        if(devices.Count() == 1 && devices.First().Target == Device.Targets._12_x86_64) return restartJob(devices.First());
        if(devices.Count() > 1 && devices.Any(device => device.Target != supportedTarget)) return null;
        if(devices.Any(device => !device.IsActive || device.Status == Device.Statuses.DISABLE)) return null;

        var launchOptions = Device.GetLaunchOptions(devices);
        if(launchOptions is null) return null;
        var serials = devices.Select(d => d.getSerialNumber());
        var rebootJob = BackgroundJob.Enqueue<ADB>(job => job.RebootAndWait(serials));
        var stopJob = BackgroundJob.ContinueJobWith<CuttlefishService>(rebootJob, job => job.Stop());
        return BackgroundJob.ContinueJobWith<CuttlefishService>(
                stopJob, 
                job => job.Launch(launchOptions), 
                JobContinuationOptions.OnAnyFinishedState
        );
    }

    private string? restartJob(Device device){
        // var devices = _db.Devices.Where(d => d.IsActive && d.Status == Device.Statuses.ENABLE).ToList();
        if(device.Target == Device.Targets._13_x86_64) return restartJob(new Device[] {device});
        if(!device.IsActive || device.Status == Device.Statuses.DISABLE) return null;

        var launchOptions = Device.GetLaunchOptions(device);
        if(launchOptions is null) return null;
        var serial = device.getSerialNumber();
        var N = device.getCuttlefishNumber();
        var rebootJob = BackgroundJob.Enqueue<ADB>(job => job.RebootAndWait(new string[] {serial}));
        var stopJob = BackgroundJob.ContinueJobWith<CuttlefishLegacyService>(rebootJob, job => job.Stop(N));
        return BackgroundJob.ContinueJobWith<CuttlefishLegacyService>(
                stopJob, 
                job => job.Launch(N, launchOptions), 
                JobContinuationOptions.OnAnyFinishedState
        );
    }


    [HttpPost]
    [Route("Devices/Add")]
    public IActionResult createDevice([FromForm] string target, [FromForm] int memory){
        if(!canRunTask()) return UnprocessableEntity(new { Errors = new [] {"Cannot handle this device now: running other tasks"}});
        if(target != Device.Targets._12_x86_64.ToString() && target != Device.Targets._13_x86_64.ToString()) return UnprocessableEntity("Invalid target");
        var chosenTarget = Device.ParseTarget(target);
        Device? availableDevice = _db.Devices.Where(d => !d.IsActive).Where(d => d.Target == chosenTarget).FirstOrDefault();
        if(availableDevice is null)  return NotFound();
        availableDevice.IsActive = true;
        _db.Devices.Update(availableDevice);
        _db.SaveChanges();

        string resetJob;

        if(chosenTarget == Device.Targets._13_x86_64){ // for >=13 android we need to reboot devices and then relaunch the cluster
            var launchOptions = Device.GetLaunchOptions(
                _db.Devices
                    .Where(d => d.Target == chosenTarget)
                    .Where(d => d.IsActive)
            );
            if(launchOptions is null) return UnprocessableEntity();
            var serials = _db.Devices.Where(d => d.IsActive).Where(d => d.Target == chosenTarget).Select(d => d.getSerialNumber());
            var rebootJob = BackgroundJob.Enqueue<ADB>(job => job.RebootAndWait(serials));
            var stopJob = BackgroundJob.ContinueJobWith<CuttlefishService>(rebootJob, job => job.Stop());
            var launchJob = BackgroundJob.ContinueJobWith<CuttlefishService>(
                stopJob, 
                job => job.Launch(launchOptions), 
                JobContinuationOptions.OnAnyFinishedState
            );
            resetJob = BackgroundJob.ContinueJobWith<CuttlefishService>(
                launchJob,
                job => job.Powerwash(availableDevice.getCuttlefishNumber())
            );

        } else if(chosenTarget == Device.Targets._12_x86_64){ // for <=12 we can just start it independently
            var launchOptions = Device.GetLaunchOptions(availableDevice);
            if(launchOptions is null) return UnprocessableEntity();

            var N = availableDevice.getCuttlefishNumber();
            var startJob = BackgroundJob.Enqueue<CuttlefishLegacyService>(
                job => job.Launch(N, launchOptions)
            );
            resetJob = BackgroundJob.ContinueJobWith<CuttlefishLegacyService>(
                startJob,
                job => job.Powerwash(N),
                JobContinuationOptions.OnAnyFinishedState // reset even if launch failed (for it can be already launched)
            );
        }else return UnprocessableEntity();

        var defaultApps = _db.DefaultApps.ToList();
        foreach(var app in defaultApps){
            var installJob = BackgroundJob.ContinueJobWith<ADB>(
                resetJob, 
                adb => adb.Install(availableDevice.getSerialNumber(), app.InstallerPath)
            );
        }
        
        return Ok("ok");
    }
   
    [HttpDelete]
    [Route("Devices/{deviceId}")]
    public IActionResult deleteDevice(string deviceId){
        var device = _db.Devices.Where(d => d.Id == deviceId).Include(d => d.Users).First();
        device.IsActive = false;
        _db.Update(device);
        _db.SaveChanges();
        return Ok();
    }


    [HttpPost]
    [Route("Devices/{deviceId}/switch")]
    public async Task<IActionResult> switchDevice(string deviceId){
        if(!canRunTask()) return UnprocessableEntity(new { Errors = new [] {"Cannot handle this device now: running other tasks"}});
        var device = _db.Devices.Where(d => d.Id == deviceId).First();
        var N = device.getCuttlefishNumber();
        var isOn = await _adb.HealthCheck(device.getSerialNumber());
        var launchOptions = Device.GetLaunchOptions(device);
        if(launchOptions is null) return UnprocessableEntity();
        string switchJob;
        device.Status = isOn ? Device.Statuses.DISABLE : Device.Statuses.ENABLE;
        _db.Devices.Update(device);
        _db.SaveChanges();
        
        if(device.Target == Device.Targets._12_x86_64){
            switchJob = BackgroundJob.Enqueue<CuttlefishLegacyService>(
                isOn ? job => job.Stop(N) : job => job.Launch(N, launchOptions)
            );
            // for android 12 we can stop/launch just the related instance
        }else if(device.Target == Device.Targets._13_x86_64){
            restartJob(_db.Devices.Where(d => d.IsActive && d.Status == Device.Statuses.ENABLE));
            // for android 13 we need to restart a whole cluster
        }else return UnprocessableEntity();
        return Ok();
    }
    [HttpPost]
    [Route("Devices/{deviceId}/Reset")]
    public IActionResult resetDevice(string deviceId){
        if(!canRunTask()) return UnprocessableEntity(new { Errors = new [] {"Cannot handle this device now: running other tasks"}});
        var device = _db.Devices.Where(d => d.Id == deviceId).First();
        var deviceNumber = device.getCuttlefishNumber();
        string resetJob;
        if(device.Target == Device.Targets._12_x86_64){
            resetJob = BackgroundJob.Enqueue<CuttlefishLegacyService>(job => job.Powerwash(deviceNumber));
        }else if(device.Target == Device.Targets._13_x86_64){
            resetJob = BackgroundJob.Enqueue<CuttlefishService>(job => job.Powerwash(deviceNumber));
        }else return UnprocessableEntity();
        
        var defaultApps = _db.DefaultApps.ToList();
        foreach(var app in defaultApps){
            var installJob = BackgroundJob.ContinueJobWith<ADB>(resetJob, adb => adb.Install(device.getSerialNumber(), app.InstallerPath));
        }

        return Ok(new {});
    }




    class UserAppInstallationJob {
        ADB _adb;
        Data.ApplicationDbContext _db;
        public UserAppInstallationJob (ADB adb, Data.ApplicationDbContext db) => (_adb, _db) = (adb, db);
        public async Task InstallAndNotify(int appId, string filePath, string deviceSerial) {
            var response = await _adb.Install(deviceSerial, filePath);

            var app = _db.MobileApps.Find(appId); // todo: handle not found case
            app!.Status = response switch {
                BridgeResponse.Success => AppStatuses.Ready,
                _ => AppStatuses.Error,
            };
            _db.MobileApps.Update(app);
            _db.SaveChanges();
        }
    }

}
