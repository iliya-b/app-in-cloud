using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace AppInCloud.Models;

public class DefaultApp
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string PackageName { get; set; }
    public AppTypes Type { get; set; }
    public DateTime CreatedTimestamp { get; set; }
}

