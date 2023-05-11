using System.Security.Claims;
using AppInCloud.Data;
using AppInCloud.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppInCloud.Controllers;


[Authorize]
public class AuthenticationController : Controller
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _db;

    public AuthenticationController(
        ApplicationDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationController> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _db = db;
    }


    [HttpGet]
    [Route("/api/v1/user")]

    public IActionResult GetClientRequestParameters()
    {
        var user = _db.Users.Where(u => u.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name).First();
        return Ok(new { Email = user.Email, isAdmin = user.IsAdmin });
    }
}
