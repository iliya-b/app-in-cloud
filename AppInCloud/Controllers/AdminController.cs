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
public class AdminController : ControllerBase
{
    
    private readonly ILogger<AdminController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
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
  
  
}
