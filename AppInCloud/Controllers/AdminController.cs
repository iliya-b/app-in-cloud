using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppInCloud.Models;
using Hangfire;
using AppInCloud.Services;

namespace AppInCloud.Controllers;

[Authorize(Roles = "Admin")]
[Route("/api/v1/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    
    private readonly ILogger<AdminController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    private readonly UserManager<Models.ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly InstallationService _installationService;
    private readonly AndroidService _androidService;

    public AdminController(ILogger<AdminController> logger, InstallationService installationService, ADB adb, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, UserManager<Models.ApplicationUser> userManager, AndroidService androidService)
    {
        _installationService = installationService;
        _logger = logger;
        _adb = adb;
        _db = db;
        _userManager = userManager;
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
        var list =  _db.Devices.Include(f => f.Users).Select(d => new {Id=d.Id, Users = d.Users.Select(u => u.Email)}).ToList();
        return Ok(new {
            Count=tasksCount(),
            List=list
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
        var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext!.User);
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


    [HttpPost]
    [Route("Devices/{deviceId}/Assign")]
    public IActionResult assignDevice(string deviceId, [FromForm] string userEmail, [FromForm] bool unassign ){
        var device = _db.Devices.Where(d => d.Id == deviceId).Include(d => d.Users).First();
        if(unassign) {
            device!.Users.Remove(device!.Users.Where(u => u.Email == userEmail).First());
        }else{
            var user = _db.Users.Where(u => u.Email == userEmail).First();
            device!.Users.Add(user!);
        }
        _db.Devices.Update(device);
        _db.SaveChanges();
        return Ok(new {});
    }

    [HttpPost]
    [Route("Devices")]
    public IActionResult changeAmountOfDevices([FromForm] int N){
        if(!canRunTask()) return UnprocessableEntity("Cannot add/remove devices: running other tasks");
        var devices = _db.Devices.ToList();
        var serials = devices.Select(d => d.getSerialNumber());
        _db.Devices.RemoveRange(devices.Where(cvd => cvd.getCuttlefishNumber() > CuttlefishLaunchOptions.BaseNumber + N - 1).ToArray());
        var newDevices = Enumerable.Range(
            CuttlefishLaunchOptions.BaseNumber, 
            N
        ).Where(n => !devices.Any(cvd => cvd.getCuttlefishNumber() == n))
        .Select(n => new Device { Id = "cvd-" + n, Memory=1024});

        _db.Devices.AddRange(newDevices);
        _db.SaveChanges(); // todo: add status to devices: provision / error / up / down


        // before restart, we reboot all the devices to save user data
        // (it's neccessairy due to the cuttlefish disk overlay mechanism)
        var rams = _db.Devices.ToList().OrderBy(f => f.Id).Select(d => d.Memory);
        var newSerials = _db.Devices.ToList().Select(d => d.getSerialNumber());
        var launchOptions = new CuttlefishLaunchOptions {
            InstancesNumber = N, 
            Memory = rams
        };
        var rebootJob = BackgroundJob.Enqueue<ADB>(job => job.RebootAndWait(serials));
        var stopJob = BackgroundJob.ContinueJobWith<CuttlefishService>(rebootJob, job => job.Stop());
        var launchJob = BackgroundJob.ContinueJobWith<CuttlefishService>(stopJob, job => job.Launch(launchOptions), JobContinuationOptions.OnAnyFinishedState);


        var defaultApps = _db.DefaultApps.ToList();
        foreach(var newSerial in newSerials){
            foreach(var app in defaultApps){
                var installJob = BackgroundJob.ContinueJobWith<ADB>(launchJob, adb => adb.Install(newSerial, app.InstallerPath));
            }
        }
        return Ok(new {});
    }

    [HttpPost]
    [Route("Devices/{deviceId}/Reset")]
    public IActionResult resetDevice(string deviceId){
        if(!canRunTask()) return UnprocessableEntity("Cannot handle this device now: running other tasks");
        var device = _db.Devices.Where(d => d.Id == deviceId).First();
        var deviceNumber = device.getCuttlefishNumber();
        var rebootJob = BackgroundJob.Enqueue<CuttlefishService>(job => job.Powerwash(deviceNumber));
        
        var defaultApps = _db.DefaultApps.ToList();
        foreach(var app in defaultApps){
            var installJob = BackgroundJob.ContinueJobWith<ADB>(rebootJob, adb => adb.Install(device.getSerialNumber(), app.InstallerPath));
        }

        return Ok(new {});
    }
}
