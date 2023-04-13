using Microsoft.AspNetCore.Identity;

namespace AppInCloud.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<MobileApp> MobileApps { get; set; }
    

    public List<Device> Devices { get; } = new();
    
}
