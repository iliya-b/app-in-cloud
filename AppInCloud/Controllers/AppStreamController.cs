using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace AppInCloud.Controllers;

// [Authorize]
[ApiController]
[Route("[controller]")]
public class AppStreamController : ControllerBase
{
    
    private readonly ILogger<AppStreamController> _logger;
    private readonly ADB _adb;

    public AppStreamController(ILogger<AppStreamController> logger, ADB adb)
    {
        _logger = logger;
        _adb = adb;
    }
 
    [HttpGet]
    public Models.App Get(int id)
    {
        return new Models.App {Id=id, Name = "App " + id, Status=Models.AppStatuses.Ready, Type=Models.AppTypes.APK};
    }
    
    [HttpPost]
    [RequestSizeLimit(1073741824)] // 1GB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Post(IFormFile apk)
    {
        return UnprocessableEntity();
    }
}
