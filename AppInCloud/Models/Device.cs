using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace AppInCloud.Models;



public class Device
{
    public string Id { get; set; }
    public List<ApplicationUser> Users  { get; set; }

}

