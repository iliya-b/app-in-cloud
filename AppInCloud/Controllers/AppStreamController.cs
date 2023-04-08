using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AppInCloud.Controllers;

// [Authorize]
[ApiController]
[Route("[controller]")]
public class AppStreamController : ControllerBase
{
    
    private readonly ILogger<AppStreamController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    private readonly UserManager<Models.ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppStreamController(ILogger<AppStreamController> logger, ADB adb, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, UserManager<Models.ApplicationUser> userManager)
    {
        _logger = logger;
        _adb = adb;
        _db = db;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }
 

 /**
    Start application and return web rtc parameters
    todo: use real webrtc, not iframe
    */
    [HttpGet]
    public async Task<string> Get(int id)
    {
        var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        var app  = _db.MobileApps.First(f => f.Id == id && f.UserId == userId);
        await _adb.start(app.PackageName);
        
        return "https://localhost:8443/devices/" + app.DeviceId + "/files/client.html";
    }

}
