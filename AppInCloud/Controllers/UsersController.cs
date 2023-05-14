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
    private readonly IMemoryCache _cache;
    private static string REGISTRATION_ENABLED_CACHE_KEY = "registration_enabled";


    public UsersController(IMemoryCache cache, ILogger<UsersController> logger, Data.ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    [HttpPost]
    [Route("Settings")]
    public IActionResult Settings([FromForm] bool registrationEnabled)
    {
        _cache.Set(REGISTRATION_ENABLED_CACHE_KEY, registrationEnabled);
        return Ok();
    }


    private bool isRegistrationEnabled() {

        if (!_cache.TryGetValue(REGISTRATION_ENABLED_CACHE_KEY, out Boolean registrationEnabled))
        {
            _cache.Set(REGISTRATION_ENABLED_CACHE_KEY, false);
            registrationEnabled = false;
        }

        return registrationEnabled;
    }
    [HttpGet]
    [Route("")] 
    public IActionResult GetUsers()
    {

        var list =  _db.Users
            .Select(u => new {Id=u.Id, Email=u.Email})
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




}
