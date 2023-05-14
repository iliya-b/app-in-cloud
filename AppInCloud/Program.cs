using Microsoft.EntityFrameworkCore;
using AppInCloud.Data;
using AppInCloud.Models;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Hangfire;
using Hangfire.Storage.SQLite;
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
    options.UseSqlite(connectionString));
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
builder.Services.AddScoped<AppInCloud.Services.CuttlefishLegacyService>( x => {
    return new CuttlefishLegacyService(x.GetRequiredService<ICommandRunner>(), x.GetRequiredService<IConfiguration>()["Emulator:LegacyBasePath"]);
});
builder.Services.AddScoped<AppInCloud.Services.AndroidService>();
builder.Services.AddControllersWithViews().AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add (new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddHangfire(configuration => configuration
        // .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseResultsInContinuations()
        .UseFilter(new AutomaticRetryAttribute { Attempts = 0 })
        .UseSQLiteStorage()
);
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

var app = builder.Build();

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

// using (IServiceScope scope = app.Services.CreateScope()) {
//         var RoleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//         var UserManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//         string[] roleNames = { "Admin", "Manager", "Member" };
//         IdentityResult roleResult;

//         foreach (var roleName in roleNames)
//         {
//             var roleExist = await RoleManager.RoleExistsAsync(roleName);
//             if (!roleExist)
//             {
//                 roleResult = await RoleManager.CreateAsync(new IdentityRole(roleName));
//             }
//         }

//         //Here you could create a super user who will maintain the web app
//         var poweruser = new ApplicationUser
//         {

//             UserName = app.Configuration["AppInCloud:DefaultAdminEmail"],
//             Email = app.Configuration["AppInCloud:DefaultAdminEmail"],
//             EmailConfirmed = true
//         };

//         string userPWD = app.Configuration["AppInCloud:DefaultAdminPassword"];
//         var _user = await UserManager.FindByEmailAsync(app.Configuration["AppInCloud:DefaultAdminEmail"]);

//        if(_user == null)
//        {
//             var createPowerUser = await UserManager.CreateAsync(poweruser, userPWD);
//             if (createPowerUser.Succeeded)
//             {
//                 await UserManager.AddToRoleAsync(poweruser, "Admin");
//                 Console.WriteLine("creating admin user");
//             }else{
//                 Console.WriteLine("failed creating admin user: {0}", createPowerUser.Errors.First().Description);
//             }
//        }else{
//             Console.WriteLine("not creating admin user");
//        }
// }


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
