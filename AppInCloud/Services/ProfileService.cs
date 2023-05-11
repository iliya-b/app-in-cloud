using System.Security.Claims;
using AppInCloud.Models;
using Microsoft.AspNetCore.Identity;

namespace AppInCloud.Services;


// service for adding role information to json config endpoints
public class ProfileService 
{
    protected readonly UserManager<ApplicationUser> _userManager;
    public ProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    // {
    //     ApplicationUser user = await _userManager.GetUserAsync(context.Subject);
    //     IList<string> roles = await _userManager.GetRolesAsync(user);
    //     IList<Claim> roleClaims = new List<Claim>();
    //     foreach (string role in roles)
    //     {
    //         roleClaims.Add(new Claim(JwtClaimTypes.Role, role));
    //     }

    //     roleClaims.Add(new Claim(JwtClaimTypes.Name, user.UserName));
    //     context.IssuedClaims.AddRange(roleClaims);
    // }

    // public Task IsActiveAsync(IsActiveContext context)
    // {
    //     return Task.CompletedTask;
    // }
}