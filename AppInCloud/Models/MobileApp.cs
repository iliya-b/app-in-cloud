using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace AppInCloud.Models;


public enum AppStatuses
{
    Ready, Installing, Error
}     

public enum AppTypes
{
    APK, AAB
}     

public class MobileApp
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PackageName { get; set; }
    public AppStatuses Status { get; set; }

    public AppTypes Type { get; set; }

    public DateTime CreatedTimestamp { get; set; }
    public string DeviceId { get; set; }
    public Device Device {get; set; } = null!;
    public string UserId { get; set; }
    public ApplicationUser User  { get; set; } = null!;

}

