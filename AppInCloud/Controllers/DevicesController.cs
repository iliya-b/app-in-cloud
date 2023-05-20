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
[Route("/api/v1/admin")]
[ApiController]
public class DevicesController : ControllerBase
{
    
    private readonly ILogger<DevicesController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DevicesController(ILogger<DevicesController> logger,IHttpContextAccessor httpContextAccessor, ADB adb, Data.ApplicationDbContext db)
    {
        _logger = logger;
        _adb = adb;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
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
    

    private ApplicationUser GetUser() {
        return _db.Users
            .Where(u => u.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)
            .Include(u=>u.Devices)
            .First();
    }
    [HttpGet]
    [Route("/api/v1/Devices")]
    [Authorize()]
    public IActionResult GetMyDevices()
    {
        var user = GetUser();
        var list =  GetUser().Devices
            .Where(d => d.IsActive)
            .Select(d => new {Id=d.Id, Status=d.Status, Serial=d.getSerialNumber(), Target=d.Target})
            .ToList()
            .Select(d => {
                var check = _adb.HealthCheck(d.Serial);
                check.Wait();
                return new {Id=d.Id, Status=d.Status, Target=d.Target, IsActive = check.Result};
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
        availableDevice.Memory = memory >= 512 ? memory : 1536;
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
    public async Task<IActionResult> deleteDevice(string deviceId){
        var device = _db.Devices.Where(d => d.Id == deviceId).Include(d => d.Users).First();
        device.IsActive = false;
        device.Status = Device.Statuses.DISABLE;
        await _adb.Halt(device.getSerialNumber());
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
    public async Task<IActionResult> resetDevice(string deviceId){
        if(!canRunTask()) return UnprocessableEntity(new { Errors = new [] {"Cannot handle this device now: running other tasks"}});
        var device = _db.Devices.Where(d => d.Id == deviceId).First();
        var deviceNumber = device.getCuttlefishNumber();
        string startJob, resetJob;
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


}
