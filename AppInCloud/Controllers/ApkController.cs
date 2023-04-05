using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppInCloud.Controllers;

// [Authorize]
[ApiController]
[Route("[controller]")]
public class ApkController : ControllerBase
{
  
    private readonly ILogger<ApkController> _logger;

    public ApkController(ILogger<ApkController> logger)
    {
        _logger = logger;
    }
 
    [HttpGet]
    public IEnumerable<int> Get()
    {
        return Enumerable.Range(1, 5);
    }
    [HttpPost]
    public async Task<IActionResult> Post(IFormFile formFile)
    {
        var filePath = "";
        if (formFile.Length > 0)
        {
            filePath = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(filePath))
            {
                await formFile.CopyToAsync(stream);
            }
        }
        return Ok(new { count = 1, formFile.Length, path=filePath });
    }
}
