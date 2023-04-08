using Microsoft.AspNetCore.Identity;

namespace AppInCloud.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<MobileApp> MobileApps { get; set; }
        
}
