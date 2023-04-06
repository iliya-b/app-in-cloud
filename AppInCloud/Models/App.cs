using Microsoft.AspNetCore.Identity;

namespace AppInCloud.Models;

public class App
{
    public int Id { get; set; }
    public string Name { get; set; }

    public string Type { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User  { get; set; } = null!;

}
