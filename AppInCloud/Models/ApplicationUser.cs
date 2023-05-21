using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;

namespace AppInCloud.Models;


[Table("AspNetUsers")]
public class ApplicationUser : IdentityUser
{
    public ICollection<MobileApp> MobileApps { get; set; }
    public List<Device> Devices { get; } = new();
    public bool IsAdmin {get; set; } 
    public int AllowedMachinesAmount {get; set; }
    public int AllowedRunningMachinesAmount {get; set; }
    public TimeSpan DailyLimit {get; set; }
    public TimeSpan MonthlyLimit {get; set; }

    public static string HashPassword(string password)   {
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: new byte [] {},
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
        }
}
