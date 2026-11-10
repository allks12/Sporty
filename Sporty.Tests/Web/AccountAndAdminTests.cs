using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sporty.Data.Identity;
using Sporty.Services.Identity;
using Sporty.Tests.Infrastructure;

namespace Sporty.Tests.Web;

public class AccountAndAdminTests : IClassFixture<SportyWebApplicationFactory> {
    private const string Password = "Passw0rd!";

    private readonly SportyWebApplicationFactory factory;

    public AccountAndAdminTests(SportyWebApplicationFactory factory) {
        this.factory = factory;
    }

    [Fact]
    public async Task Guest_OpeningAdminArea_IsRedirectedToLogin() {
        var client = factory.CreateClientWithoutRedirects();

        var response = await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Identity/Account/Login", response.Headers.Location!.AbsolutePath);
        Assert.Contains("ReturnUrl=%2FAdmin", response.Headers.Location.Query);
    }

    [Fact]
    public async Task User_OpeningAdminArea_GetsForbidden() {
        var client = factory.CreateClientWithoutRedirects();
        await client.RegisterAsync("Ivan Petrov", "user-forbidden@sporty.test", Password);

        var response = await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_OpeningAdminArea_GetsThePage() {
        var client = factory.CreateClientWithoutRedirects();
        var login = await client.LoginAsync(SportyWebApplicationFactory.AdminEmail, SportyWebApplicationFactory.AdminPassword);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var response = await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_CreatesUserWithUserRole() {
        const string email = "new-user@sporty.test";
        var client = factory.CreateClientWithoutRedirects();

        var response = await client.RegisterAsync("  Maria Georgieva ", email, Password);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.Equal("Maria Georgieva", user.Name);
        Assert.False(user.IsDeleted);
        Assert.Equal(SportyWebApplicationFactory.Now.UtcDateTime, user.CreatedAtUtc);
        Assert.Equal([AppRoles.User], await userManager.GetRolesAsync(user));
    }

    [Fact]
    public async Task Register_WithTakenEmail_ShowsTheFormAgain() {
        const string email = "taken@sporty.test";
        await factory.CreateClientWithoutRedirects().RegisterAsync("First", email, Password);
        var client = factory.CreateClientWithoutRedirects();

        var response = await client.RegisterAsync("Second", email, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("is already taken.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsTheFormAgain() {
        var client = factory.CreateClientWithoutRedirects();

        var response = await client.LoginAsync(SportyWebApplicationFactory.AdminEmail, "Wrong123!");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid login attempt.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_AsDeletedUser_IsRejected() {
        const string email = "deleted@sporty.test";
        await factory.CreateClientWithoutRedirects().RegisterAsync("Deleted", email, Password);
        using (var scope = factory.Services.CreateScope()) {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await userManager.FindByEmailAsync(email))!;
            user.IsDeleted = true;
            await userManager.UpdateAsync(user);
        }
        var client = factory.CreateClientWithoutRedirects();

        var response = await client.LoginAsync(email, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid login attempt.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logout_SignsTheUserOut() {
        var client = factory.CreateClientWithoutRedirects();
        await client.LoginAsync(SportyWebApplicationFactory.AdminEmail, SportyWebApplicationFactory.AdminPassword);

        var logout = await client.PostFormAsync("/", "/Identity/Account/Logout", []);

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        var response = await client.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Seeder_RunTwice_KeepsOneAdministrator() {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        var admin = Assert.Single(admins);
        Assert.Equal(SportyWebApplicationFactory.AdminEmail, admin.Email);
    }

    [Fact]
    public async Task DemoUser_IsSeededAsRegularUser() {
        var client = factory.CreateClientWithoutRedirects();
        var login = await client.LoginAsync(SportyWebApplicationFactory.DemoUserEmail, SportyWebApplicationFactory.DemoUserPassword);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var response = await client.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
