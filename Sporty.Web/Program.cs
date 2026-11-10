using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sporty.Data.Contexts;
using Sporty.Data.Identity;
using Sporty.Services.Identity;
using Sporty.Web.Weather;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddDbContext<SportyDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register Geocoding service and MemoryCache (used for 24-hour caching of Nominatim coordinates)
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

// Register Weather service stub (used for mock weather forecast and 5-day horizon validation)
builder.Services.AddScoped<IWeatherService, OpenWeatherMapServiceStub>();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options => {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<SportyDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();
builder.Services.AddRazorPages();

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";

    //A signed-in user without the needed role gets 403 instead of a redirect
    options.Events.OnRedirectToAccessDenied = context => {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IdentitySeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();


app.Run();
