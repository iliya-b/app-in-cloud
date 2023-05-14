using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AppInCloud.Models;

namespace AppInCloud.Data;

public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<MobileApp> MobileApps { get; set; }
    public DbSet<DefaultApp> DefaultApps { get; set; }
    public DbSet<Device> Devices { get; set; } 
    public DbSet<ApplicationUser> Users {get; set; }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options) {}
}
