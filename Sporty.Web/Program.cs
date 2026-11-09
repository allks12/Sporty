using Microsoft.EntityFrameworkCore;
using Sporty.Data.Contexts;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();