using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using AppInCloud.Data;
using AppInCloud.Models;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Hangfire;
using Hangfire.Storage.SQLite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

builder.Services.AddAuthentication(options => { // todo: why it's really required when jwt is used?
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        })
    .AddIdentityServerJwt();
builder.Services.AddAuthorization(options => {
    options.AddPolicy("authorized", p => p.RequireAuthenticatedUser());
});

builder.Services.AddHttpContextAccessor();
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<AppInCloud.ADB>();
builder.Services.AddScoped<AppInCloud.InstallationService>();
builder.Services.AddControllersWithViews().AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add (new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddHangfire(configuration => configuration
        // .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSQLiteStorage());
builder.Services.AddHangfireServer();

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
app.UseIdentityServer();
app.UseAuthorization();


app.UseEndpoints(endpoints => {
        endpoints.MapReverseProxy(pipe  => {
            // pipe.Use( (HttpContext context, Func<Task> next) => {
            //         context.Request.EnableBuffering();
            //         return next();
            // });
        });
        endpoints.MapHangfireDashboard();

});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

// app.MapGet("/polled_connections", () => "wow");
app.MapRazorPages();

app.MapFallbackToFile("index.html");;
app.Run();
