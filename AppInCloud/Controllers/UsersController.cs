using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppInCloud.Models;
using Hangfire;
using AppInCloud.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;

namespace AppInCloud.Controllers;

[Authorize(Policy = "admin")]
[Route("/api/v1/admin/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    
    private readonly ILogger<UsersController> _logger;
    private readonly Data.ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;


    public UsersController(IConfiguration config, ILogger<UsersController> logger, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _config = config;
    }

    [HttpPost]
    [Route("Settings")]
    public IActionResult Settings([FromForm] bool registrationEnabled)
    {
        Services.Settings.Update(Services.Settings.REGISTRATION_ENABLED_CACHE_KEY, registrationEnabled);
        return Ok();
    }


    private bool isRegistrationEnabled() {
        return _config.GetValue<bool>(Services.Settings.REGISTRATION_ENABLED_CACHE_KEY, true);
    }
    [HttpGet]
    [Route("")] 
    public IActionResult GetUsers()
    {

        var list =  _db.Users
            .Select(u => new {Id=u.Id, Email=u.Email, DailyLimit=u.DailyLimit.TotalMinutes, AllowedRunningMachinesAmount=u.AllowedRunningMachinesAmount, AllowedMachinesAmount = u.AllowedMachinesAmount})
            .OrderBy(u => u.Id)
            .ToList(); 
        return Ok(new { List=list, RegistrationEnabled = isRegistrationEnabled() });
    }


    private IActionResult saveUser(ApplicationUser user) {
        _db.Users.Add(user);
        _db.SaveChanges();
        return Ok();
    }

    [HttpPost]
    [Route("Create")]
    public IActionResult createUser([FromForm] [EmailAddress] string email, [FromForm][MinLength(8)] string password){

        return _db.Users.Where(u => u.Email == email).Count() switch {
            > 0 => UnprocessableEntity("already registered"),
            _ => saveUser(new ApplicationUser { Email = email, PasswordHash = ApplicationUser.HashPassword(password) })
        };
    }
   
    [HttpDelete]
    [Route("{userId}")]
    public IActionResult deleteUser(string userId){
        var user = _db.Users.Where(u => u.Id == userId).First();
        
        _db.Users.Remove(user);
        _db.SaveChanges();
        return Ok();
    }


   
    [HttpPost]
    [Route("{userId}")]
    public IActionResult updateUser(string userId, [FromForm] int dailyLimit, [FromForm] int allowedRunningMachinesAmount, [FromForm] int allowedMachinesAmount){
        var user = _db.Users.Where(u => u.Id == userId).First();
        if(allowedMachinesAmount < 0) return UnprocessableEntity("allowedMachinesAmount should be >= 0");
        if(allowedRunningMachinesAmount < 0) return UnprocessableEntity("allowedRunningMachinesAmount should be >= 0");
        if(allowedRunningMachinesAmount > allowedMachinesAmount ) return UnprocessableEntity("allowedRunningMachinesAmount should be <= allowedMachinesAmount");
        user.AllowedMachinesAmount = allowedMachinesAmount;
        user.AllowedRunningMachinesAmount = allowedRunningMachinesAmount;
        user.DailyLimit = TimeSpan.FromMinutes(dailyLimit);
        _db.Users.Update(user);
        _db.SaveChanges();
        return Ok();
    }




}
