using System.Text.Json;
using System.Text.Json.Serialization;
namespace AppInCloud.Models;


public enum AppStatuses
{
    Ready, Error
}     

public enum AppTypes
{
    APK, AAB
}     

public class App
{
    public int Id { get; set; }
    public string Name { get; set; }
    public AppStatuses Status { get; set; }

    public AppTypes Type { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User  { get; set; } = null!;

}
