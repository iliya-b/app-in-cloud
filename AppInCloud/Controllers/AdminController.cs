using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AppInCloud.Models;
using Hangfire;
using AppInCloud.Services;

namespace AppInCloud.Controllers;

[Authorize]
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

    [HttpGet]
    [Route("Devices")]
    public IEnumerable<object> GetDevices()
    {
        return  _db.Devices.Include(f => f.Users).Select(d => new {Id=d.Id, Users = d.Users.Select(u => u.Email)}).ToList();
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
            var jobId = BackgroundJob.Enqueue<ADB>(job => job.install(deviceSerial, filePath));
        }

        _db.DefaultApps.Add(new Models.DefaultApp {
            Name = file.FileName,
            PackageName = packageName,
            Type = type,
            CreatedTimestamp = DateTime.Now
        });

        await _db.SaveChangesAsync();    
           
        return Ok(new { success=true });
    }


    [Route("/admin/add-device")]
    public async Task<IActionResult> addDevice(int N){
        
        // HOME=$PWD ./bin/launch_cvd --num_instances ${N + 1} --base_instance_num 1

        // first, reboot all the devices to save user data   (it's neccessairy due to the cuttlefish copy-on-write mechanism)
        {
            var tasks = _db.Devices.ToList().Select(device => _adb.reboot(device.getSerialNumber()));
            await Task.WhenAll(tasks);
        }

        // wait for devices to reboot
        {
            IEnumerable<Task<bool>> tasks;
            do{
                tasks = _db.Devices.ToList().Select(device => _adb.healthCheck(device.getSerialNumber()));
                await Task.WhenAll(tasks);
            }while(!tasks.All(t => t.Result));
        }
        // stop cuttlefish
        // start cuttlefish server with new N

        //await start(N);
        return Ok(new {});
    }
    [Route("/admin/reset-device")]
    public async Task<IActionResult> resetDevice(){


        return Ok(new {});
    }
}
