using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AppInCloud.Models;

namespace AppInCloud.Controllers;

[Authorize]
[Route("/api/v1/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    
    private readonly ILogger<AdminController> _logger;
    private readonly ADB _adb;
    private readonly Data.ApplicationDbContext _db;
    private readonly UserManager<Models.ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly InstallationService _installationService;

    public AdminController(ILogger<AdminController> logger, InstallationService installationService, ADB adb, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, UserManager<Models.ApplicationUser> userManager)
    {
        _installationService = installationService;
        _logger = logger;
        _adb = adb;
        _db = db;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    [Route("Devices")]
    public IEnumerable<object> GetDevices()
    {
        return  _db.Devices.Include(f => f.Users).Select(d => new {Id=d.Id, Users = d.Users.Select(u => u.Email)}).ToList();
    }
    


    [HttpGet]
    [Route("DefaultApps")]
    public IEnumerable<object> getDefaultApps()
    {
        return  _db.DefaultApps.ToList();
    }
    
    [HttpPost]
    [RequestSizeLimit(1073741824)] // 1GB
    [Consumes("multipart/form-data")]
    [Route("Upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {

            
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
   
        PackageInfo? package = null;
        try{
            foreach(Models.Device device in _db.Devices){
                package = await _installationService.install(filePath, device.getSerialNumber());
            }
        }catch(InvalidFileException){
            return UnprocessableEntity();
        }
        if(package is null) return UnprocessableEntity();

        _db.DefaultApps.Add(new Models.DefaultApp {
            Name = file.FileName,
            PackageName = package.Name,
            Type = package.Type == "aab" ? AppTypes.AAB : AppTypes.APK,
            CreatedTimestamp = DateTime.Now
        });

        await _db.SaveChangesAsync();    
           
        return Ok(new { success=true });
    }


    [Route("/admin/add-device")]
    public async Task<IActionResult> addDevice(int N){
        
        // HOME=$PWD ./bin/launch_cvd --num_instances ${N + 1} --base_instance_num 1

        // first, reboot all the devices to save user data   (it's neccessairy due to the cuttlefish copy-on-write mechanism)
        {
            var tasks = _db.Devices.ToList().Select(device => _adb.reboot(device.getSerialNumber()));
            await Task.WhenAll(tasks);
        }

        // wait for devices to reboot
        {
            IEnumerable<Task<bool>> tasks;
            do{
                tasks = _db.Devices.ToList().Select(device => _adb.healthCheck(device.getSerialNumber()));
                await Task.WhenAll(tasks);
            }while(!tasks.All(t => t.Result));
        }
        // stop cuttlefish
        // start cuttlefish server with new N

        await _installationService.start(N);
        return Ok(new {});
    }
    [Route("/admin/reset-device")]
    public async Task<IActionResult> resetDevice(){


        return Ok(new {});
    }
}
