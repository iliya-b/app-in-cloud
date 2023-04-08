using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace AppInCloud.Controllers;

// [Authorize]
[ApiController]
[Route("[controller]")]
public class ApkController : ControllerBase
{
    
    private readonly ILogger<ApkController> _logger;
    private readonly ADB _adb;

    public ApkController(ILogger<ApkController> logger, ADB adb)
    {
        _logger = logger;
        _adb = adb;
    }
 
    [HttpGet]
    public IEnumerable<Models.App> Get()
    {
        return Enumerable.Range(1, 5).Select(
            i => new Models.App {Id=i, Name = "App " + i, Status=Models.AppStatuses.Ready, Type=Models.AppTypes.APK}
        );
    }
    
    [HttpPost]
    [RequestSizeLimit(1073741824)] // 1GB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Post(IFormFile apk)
    {
        if (apk.Length == 0) return UnprocessableEntity();
        
        var filePath = Path.GetTempFileName() + ".apk";
        using (var stream = System.IO.File.Create(filePath))
        {
            await apk.CopyToAsync(stream);
        }
        await _adb.install(filePath);                
        return Ok(new { count = 1, apk.Length, path=filePath });
    }
}
