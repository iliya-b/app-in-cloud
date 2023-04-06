using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public IEnumerable<int> Get()
    {
        return Enumerable.Range(1, 5);
    }
    [HttpPost]
    [RequestSizeLimit(1073741824)] 
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Post(IFormFile apk)
    {
        var filePath = "";
        if (apk.Length > 0)
        {
            filePath = Path.GetTempFileName() + ".apk";
            using (var stream = System.IO.File.Create(filePath))
            {
                await apk.CopyToAsync(stream);
            }
            await _adb.install(filePath);            
        }
        return Ok(new { count = 1, apk.Length, path=filePath });
    }
}
