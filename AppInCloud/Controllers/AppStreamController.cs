using System.Text.Json;
using AppInCloud.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppInCloud.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
[Route("/api/v1/[controller]")]
[ApiController]
public class AppStreamController : ControllerBase
{
    
    private readonly ILogger<AppStreamController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    private readonly UserManager<Models.ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    static readonly HttpClient client = new HttpClient( 
        new HttpClientHandler {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = 
                (httpRequestMessage, cert, cetChain, policyErrors) => true 
        }
    );

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
    [Route("")]
    public async Task<string> Get(int id)
    {
        var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        var app  = _db.MobileApps.First(f => f.Id == id && f.UserId == userId);
        var user = _db.Users.Where(f => f.Id == userId).Include(f => f.Devices).First();
        if(user.Devices.Count() == 0){
            return ("No device is available");
        }
        Models.Device device = user.Devices.First();
        _adb.Serial = device.getSerialNumber();
        await _adb.Start(app.PackageName);
        
        return "/devices/" + app.DeviceId + "/files/client.html";
    }


    public class DeviceRequest
    {
        public string device_id { get; set; }
    }
    [HttpPost]
    [Route("/polled_connections")]
    public async Task<IActionResult> PolledConnection(DeviceRequest deviceRequest)
    {
        var userId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        bool allowed = _db.Users.Where(f => f.Id == userId).Include(f => f.Devices).First().Devices.Exists(f => f.Id == deviceRequest.device_id);
        if(!allowed) {
            return new JsonResult("error");
        }
        
        try	
        {
            HttpResponseMessage response = await client.PostAsync("https://localhost:8532/polled_connections", new StringContent(JsonSerializer.Serialize(deviceRequest)));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ContentResult { Content = responseBody, ContentType = "application/json" };
        }
        catch (HttpRequestException)
        {
            return new JsonResult("error");
        }
    }

}
