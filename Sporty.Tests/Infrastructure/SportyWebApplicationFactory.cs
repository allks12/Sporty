using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Sporty.Data.Contexts;
using Sporty.Services.Identity;

namespace Sporty.Tests.Infrastructure;

//Runs the real app with an in-memory database and a fixed clock
public class SportyWebApplicationFactory : WebApplicationFactory<Program> {
    public const string AdminEmail = IdentitySeeder.AdminEmail;
    public const string AdminPassword = IdentitySeeder.AdminPassword;
    public const string DemoUserEmail = IdentitySeeder.DemoUserEmail;
    public const string DemoUserPassword = IdentitySeeder.DemoUserPassword;
    public static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly string databaseName = $"sporty-tests-{Guid.NewGuid()}";

    public FakeTimeProvider TimeProvider {get;} = new(Now);

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused");

        builder.ConfigureServices(services => {
            services.RemoveAll<DbContextOptions<SportyDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SportyDbContext>>();
            services.AddDbContext<SportyDbContext>(options => options.UseInMemoryDatabase(databaseName));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }

    public HttpClient CreateClientWithoutRedirects() =>
        CreateClient(new WebApplicationFactoryClientOptions {AllowAutoRedirect = false});
}
