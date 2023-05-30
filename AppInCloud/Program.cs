using Microsoft.EntityFrameworkCore;
using AppInCloud.Data;
using AppInCloud.Models;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Hangfire;
using Hangfire.PostgreSql;
using AppInCloud.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
//     .AddRoles<IdentityRole>()
//     .AddEntityFrameworkStores<ApplicationDbContext>();

// builder.Services.AddIdentityServer()
//     .AddApiAuthorization<ApplicationUser, ApplicationDbContext>()
//     .AddProfileService<ProfileService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization(options => {
    options.AddPolicy("authorized", p => p.RequireAuthenticatedUser());
    options.AddPolicy("admin", p => p.RequireClaim(ClaimTypes.Role, "Admin"));

});

builder.Services.AddHttpContextAccessor();
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options => {
    // options.Conventions.AuthorizePage("/Account/Register", "registeration_enabled");
});
builder.Services.AddScoped<AppInCloud.Services.ICommandRunner, AppInCloud.Services.LocalCommandRunner>();
builder.Services.AddScoped<AppInCloud.Services.ADB>();
builder.Services.AddScoped<AppInCloud.Services.InstallationService>();

builder.Services.AddScoped<AppInCloud.Services.CuttlefishService>( x => {
    return new CuttlefishService(x.GetRequiredService<ICommandRunner>(), x.GetRequiredService<IConfiguration>()["Emulator:BasePath"]);
});
builder.Services.AddScoped<AppInCloud.Services.VirtualDeviceService>( x => {
    return new VirtualDeviceService(x.GetRequiredService<ICommandRunner>(), (int N) => {
        if(N <= 10){
            return x.GetRequiredService<IConfiguration>()["Emulator:LegacyBasePath"] + N;
        }else{
            return x.GetRequiredService<IConfiguration>()["Emulator:BasePath"] + N;
        }
    });
});
builder.Services.AddScoped<AppInCloud.Services.AndroidService>();
builder.Services.AddControllersWithViews().AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add (new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddHangfireServer(x => 
        {
            x.ServerName = "cuttlefish";
            x.Queues = new[] {"cuttlefish"};
            x.WorkerCount = 1; // do not allow run multiple cuttlefish operations concurrently
        });


builder.Services.AddHangfireServer(x =>
        {
            x.Queues = new[] {"default"};
            x.WorkerCount = 3;
        });

builder.Services.AddHangfire(configuration => configuration
        .UseSimpleAssemblyNameTypeSerializer()
        // .UseRecommendedSerializerSettings()
        .UseResultsInContinuations()
        .UseFilter(new AutomaticRetryAttribute { Attempts = 0 })
        .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions()
                {
                    PrepareSchemaIfNecessary = true,
                    SchemaName = "schema"
                })
);
var app = builder.Build();
    app.UseHttpLogging();

app.UseHangfireDashboard();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
using (IServiceScope scope = app.Services.CreateScope()) 
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();


    for(int i=90; i <= 92 ; ++i){
        var Id = "cvd-" + i;
        var device = new Device() {
            Id=Id,
            Memory=1536,
            IsActive=false,
            Status=Device.Statuses.ENABLE,
            Target=Device.Targets._13_x86_64
        };
        
        if(!context.Devices.Any(d => d.Id == Id)) context.Devices.Add(device);
    }
    
    for(int i=1; i <= 2 ; ++i){
        var Id = "cvd-" + i;
        var device = new Device() {
            Id=Id,
            Memory=1536,
            IsActive=false,
            Status=Device.Statuses.ENABLE,
            Target=Device.Targets._12_x86_64
        };
        
        if(!context.Devices.Any(d => d.Id == Id)) context.Devices.Add(device);
    }


    string adminPass = app.Configuration["AppInCloud:DefaultAdminPassword"];

    var admin = new ApplicationUser
    {
        UserName = app.Configuration["AppInCloud:DefaultAdminEmail"],
        Email = app.Configuration["AppInCloud:DefaultAdminEmail"],
        EmailConfirmed = true,
        PasswordHash = ApplicationUser.HashPassword(adminPass)
    };


    if(!context.Users.Any(u=>u.Email == app.Configuration["AppInCloud:DefaultAdminEmail"])){
        context.Users.Add(admin);
        Console.WriteLine("creating admin user");  
    }

    context.SaveChanges();
}


app.UseEndpoints(endpoints => {
        endpoints.MapReverseProxy(pipe  => {  });
        endpoints.MapHangfireDashboard();

});


foreach (var c in builder.Configuration.AsEnumerable())
{
    Console.WriteLine(c.Key + " = " + c.Value);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapFallbackToFile("index.html");
app.Run();
