using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AppInCloud.Models;
using Hangfire;

namespace AppInCloud.Controllers;

[Authorize]
[ApiController]
[Route("/api/v1/[controller]")]
public class ApkController : ControllerBase
{
    
    private readonly ILogger<ApkController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    private readonly UserManager<Models.ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly InstallationService _installationService;
    public ApkController(ILogger<ApkController> logger, InstallationService installationService, ADB adb, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, UserManager<Models.ApplicationUser> userManager)
    {
        _logger = logger;
        _adb = adb;
        _db = db;
        _userManager = userManager;
        _installationService = installationService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    [Route("")]
    public IEnumerable<Models.MobileApp> Get()
    {
        var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        return  _db.MobileApps.Where(f => f.UserId == userId).ToList();
    }


    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        var app = _db.MobileApps.Where(f => f.Id == id && f.UserId == userId).Include(f => f.Device).First();
        try{
            await _adb.delete(app.Device.getSerialNumber(), app.PackageName);
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
        var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        var user = _db.Users.Where(f => f.Id == userId).Include(f => f.Devices).First();
        if(user.Devices.Count() == 0){
            return NotFound("No device is available");
        }
        Models.Device device = user.Devices.First();
        
        var entry = _db.MobileApps.Add(new Models.MobileApp {
            Name = file.FileName,
            PackageName = "",
            UserId = userId,
            Status = Models.AppStatuses.Installing,
            Type = AppTypes.APK,
            DeviceId = user.Devices.First().Id, 
            CreatedTimestamp = DateTime.Now
        }); 
        await _db.SaveChangesAsync();   
        int id = entry.Entity.Id;
        string serial = device.getSerialNumber();
        
        if (file.Length == 0) throw new InvalidFileException() ;
        Models.AppTypes type;
        if (file.FileName.EndsWith(".aab")){
            type = Models.AppTypes.AAB;
        }else if (file.FileName.EndsWith(".apk")){
            type = Models.AppTypes.APK;
        }else {
            throw new InvalidFileException();
        }
        var filePath = Path.GetTempFileName() + "." + type.ToString();
        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }


        var jobId = BackgroundJob.Enqueue<InstallationService.InstallJob>( job => job.Run(filePath, id, serial) );
        
        return Ok(new { success=true });
    }
}