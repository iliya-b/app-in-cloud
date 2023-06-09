using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AppInCloud.Models;
using Hangfire;
using Hangfire.Server;
using AppInCloud.Services;

namespace AppInCloud.Controllers;

[Authorize]
[ApiController]
[Route("/api/v1/[controller]")]
public class ApkController : ControllerBase
{
    
    private readonly ILogger<ApkController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly InstallationService _installationService;
    private readonly AndroidService _androidService;

    public ApkController(ILogger<ApkController> logger, InstallationService installationService, ADB adb, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, AndroidService androidService)
    {
        _installationService = installationService;
        _logger = logger;
        _adb = adb;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _androidService = androidService;
    }
    [HttpGet]
    [Route("")]
    public IActionResult Get()
    {
        return Ok(
            _db.MobileApps.IgnoreAutoIncludes().Where(f => f.UserId == GetUser().Id).ToList().Select(
                app => new {
                    Id = app.Id, Name=app.Name, 
                    PackageName = app.PackageName, 
                    Type=app.Type, Status = app.Status,
                    DeviceId = app.DeviceId
            })
        );
    }

    private ApplicationUser GetUser() {
        return _db.Users.Where(u => u.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name).First();
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = GetUser();
        var app = _db.MobileApps.Where(f => f.Id == id && f.UserId == user.Id).Include(f => f.Device).First();
        try{
            await _adb.Uninstall(app.Device.getSerialNumber(), app.PackageName);
        }catch(Exception e){
            var packages = await _adb.getPackages(app.Device.getSerialNumber());
            if(packages.Any(p => p.Name == app.PackageName)){
                throw e;
            }
        }
        _db.MobileApps.Remove(app);
        _db.SaveChanges();
        return Ok(new {});
    }

    [HttpPost]
    [RequestSizeLimit(1073741824)] // 1GB
    [Consumes("multipart/form-data")]
    [Route("Upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromServices]IServiceScopeFactory scopeFactory)
    {
        var userId = GetUser().Id;
        var user = _db.Users.Where(f => f.Id == userId).Include(f => f.Devices).First();
        if(user.Devices.Count() == 0){
            return NotFound("No device is available");
        }
        string filePath;
        string packageName;
        AppTypes type = file.FileName.EndsWith(".aab") ? AppTypes.AAB : AppTypes.APK;
        try{
            filePath = await _installationService.CopyInstaller(file);
            packageName = _androidService.getInstallerPackageName(filePath);
        }catch(Exception e){
            return UnprocessableEntity(e.ToString());
        }

        Models.Device device = user.Devices.First();
        var appEntity = _db.MobileApps.Add(new Models.MobileApp {
            Name = file.FileName,
            PackageName = packageName,
            UserId = userId,
            Status = Models.AppStatuses.Installing,
            Type = type,
            DeviceId = device.Id, 
            CreatedTimestamp = DateTime.Now
        }); 
        await _db.SaveChangesAsync();   
        int appId = appEntity.Entity.Id;
        string serial = device.getSerialNumber();
        var jobId = BackgroundJob.Enqueue<UserAppInstallationJob>(job => job.InstallAndNotify(appId, filePath, serial));

        return Ok(new { success=true });
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