using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace AppInCloud.Controllers;

[Authorize]
[ApiController]
public class ApkController : ControllerBase
{
    
    private readonly ILogger<ApkController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    private readonly UserManager<Models.ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ApkController(ILogger<ApkController> logger, ADB adb, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, UserManager<Models.ApplicationUser> userManager)
    {
        _logger = logger;
        _adb = adb;
        _db = db;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    [Route("[controller]")]
    public IEnumerable<Models.MobileApp> Get()
    {
        return  _db.MobileApps.ToList();
    }
    
    [HttpPost]
    [RequestSizeLimit(1073741824)] // 1GB
    [Consumes("multipart/form-data")]
    [Route("[controller]/Upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length == 0) return UnprocessableEntity();
        Models.AppTypes type;
        if (file.FileName.EndsWith(".aab")){
            type = Models.AppTypes.AAB;
        }else if (file.FileName.EndsWith(".apk")){
            type = Models.AppTypes.APK;
        }else {
            return UnprocessableEntity();
        }
        var filePath = Path.GetTempFileName() + "." + type.ToString();
        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        
        await _adb.install(filePath);    

        // now determine package name 
        // todo: use direct method involving reading AndroidManifest.xml
        var installerHash = "";
        using (var sha256 = SHA256.Create())
        {
            using (var stream = file.OpenReadStream())
            {
                installerHash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }
        PackageInfo[] packages = await _adb.getPackages();
        var package = packages.First(p => p.InstallerHashSum == installerHash);

        var user = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        _db.MobileApps.Add(new Models.MobileApp {
            Name = file.FileName,
            PackageName = package.Name,
            UserId = user,
            Status = Models.AppStatuses.Ready,
            Type = type,
            DeviceId = "cvd-1",
            CreatedTimestamp = DateTime.Now
        });
        await _db.SaveChangesAsync();            
        return Ok(new { count = 1, file.Length, path=filePath });
    }
}
