using System.Text.Json;
using AppInCloud.Models;
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    static readonly HttpClient client = new HttpClient( 
        new HttpClientHandler {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = 
                (httpRequestMessage, cert, cetChain, policyErrors) => true 
        }
    );

    public AppStreamController(ILogger<AppStreamController> logger, ADB adb, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _adb = adb;
        _db = db;
        _httpContextAccessor = httpContextAccessor;

    }   
 
    private ApplicationUser GetUser() {
        return _db.Users.Where(u => u.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name).Include(u=>u.Devices).First();
    }

 /**
    Start application and return web rtc parameters
    todo: use real webrtc, not iframe
    */
    // [HttpGet]
    // [Route("")]
    // public async Task<IActionResult> Get(int id)
    // {
    //     var userId = GetUser().Id;
    //     var app  = _db.MobileApps.First(f => f.Id == id && f.UserId == userId);
    //     var user = _db.Users.Where(f => f.Id == userId).Include(f => f.Devices).First();

    //     Models.Device device = app.Device;
    //     if(device is null || !user.Devices.Any(d => d.Id == device.Id)){
    //         return Unauthorized("No device is available"); // no device or app assigned to a device to which user has no access
    //                                             // e.g. if access was drawned
    //     }
    //     await _adb.Start(device.getSerialNumber(), app.PackageName);
        
    //     return Ok("/devices/" + app.DeviceId + "/files/client.html");
    // }
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Get(int id)
    {
        var userId = GetUser().Id;
        var app  = _db.MobileApps.First(f => f.Id == id && f.UserId == userId);
        var user = _db.Users.Where(f => f.Id == userId).Include(f => f.Devices).First();
        
        Models.Device? device = app.Device;
        if(device is null || !device.IsActive || device.Status == Device.Statuses.DISABLE){
            return Unauthorized("No device is available: " + (device is null ? "device not found" : "device is down or disabled"));
        }

        return device.Target switch {
            Device.Targets._13_x86_64 => Ok("/devices/" + device.Id + "/files/client.html"),
            Device.Targets._12_x86_64 => Ok("/devices/" + device.Id + "/"),
            _ => Ok(),
        };
    }


    public class DeviceRequest
    {
        public string device_id { get; set; }
    }

    
    [HttpPost]
    [Route("/polled_connections")]
    public async Task<IActionResult> PolledConnection(DeviceRequest deviceRequest)
    {
        var userId = GetUser().Id;
        var device = _db.Users.Where(f => f.Id == userId).Include(f => f.Devices).First().Devices.Find(f => f.Id == deviceRequest.device_id);
        if(device is null) {
            return new JsonResult("error device");
        }
        
        try	
        {
            HttpResponseMessage response = await client.PostAsync("https://localhost:" + (8442 + CuttlefishLaunchOptions.getBaseNumber(device.Target)) + "/polled_connections", new StringContent(JsonSerializer.Serialize(deviceRequest)));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ContentResult { Content = responseBody, ContentType = "application/json" };
        }
        catch (HttpRequestException)
        {
            return new JsonResult("error proxy");
        }
    }

}
