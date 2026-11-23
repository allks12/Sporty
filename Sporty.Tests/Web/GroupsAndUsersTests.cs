using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sporty.Data.Contexts;
using Sporty.Data.Identity;
using Sporty.Tests.Infrastructure;

namespace Sporty.Tests.Web;

public class GroupsAndUsersTests : IClassFixture<SportyWebApplicationFactory> {
    private const string Password = "Passw0rd!";

    private readonly SportyWebApplicationFactory factory;

    public GroupsAndUsersTests(SportyWebApplicationFactory factory) {
        this.factory = factory;
    }

    [Theory]
    [InlineData("/Groups/Create")]
    [InlineData("/Groups/Mine")]
    public async Task Guest_IsRedirectedToLogin(string url) {
        var client = factory.CreateClientWithoutRedirects();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Identity/Account/Login", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task User_CannotDeleteSomeoneElsesGroup() {
        var owner = await RegisterAsync("owner@sporty.test");
        var groupId = await CreateGroupAsync(owner, "Owner Park");
        var other = await RegisterAsync("intruder@sporty.test");

        var response = await other.PostFormAsync("/Groups/Mine", $"/Groups/Delete/{groupId}", new());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(GroupExists(groupId));
    }

    [Fact]
    public async Task User_DeletesOwnGroup() {
        var client = await RegisterAsync("deleter@sporty.test");
        var groupId = await CreateGroupAsync(client, "Delete Park");

        var response = await client.PostFormAsync("/Groups/Mine", $"/Groups/Delete/{groupId}", new());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(GroupExists(groupId));
    }

    [Fact]
    public async Task Admin_DeletesAnyGroup() {
        var owner = await RegisterAsync("group-owner@sporty.test");
        var groupId = await CreateGroupAsync(owner, "Admin Park");
        var admin = await LoginAdminAsync();

        var response = await admin.PostFormAsync("/Admin/Groups", $"/Admin/Groups/Delete/{groupId}", new());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(GroupExists(groupId));
    }

    [Theory]
    [InlineData("/Admin/Users")]
    [InlineData("/Admin/Groups")]
    public async Task AdminPages_AreForAdminsOnly(string url) {
        var guest = factory.CreateClientWithoutRedirects();
        var user = await RegisterAsync($"plain-{Guid.NewGuid():N}@sporty.test");
        var admin = await LoginAdminAsync();

        Assert.Equal(HttpStatusCode.Redirect, (await guest.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Admin_DeletesUser_WhoCanNoLongerLogIn() {
        const string email = "to-delete@sporty.test";
        await RegisterAsync(email);
        var userId = await GetUserIdAsync(email);
        var admin = await LoginAdminAsync();

        var response = await admin.PostFormAsync("/Admin/Users", $"/Admin/Users/Delete/{userId}", new());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var login = await factory.CreateClientWithoutRedirects().LoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("Invalid login attempt.", await login.Content.ReadAsStringAsync());

    }

    [Fact]
    public async Task Admin_CannotDeleteThemselves() {
        var adminId = await GetUserIdAsync(SportyWebApplicationFactory.AdminEmail);
        var admin = await LoginAdminAsync();

        var response = await admin.PostFormAsync("/Admin/Users", $"/Admin/Users/Delete/{adminId}", new());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> RegisterAsync(string email) {
        var client = factory.CreateClientWithoutRedirects();
        var response = await client.RegisterAsync("Test User", email, Password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    private async Task<HttpClient> LoginAdminAsync() {
        var client = factory.CreateClientWithoutRedirects();
        await client.LoginAsync(SportyWebApplicationFactory.AdminEmail, SportyWebApplicationFactory.AdminPassword);
        return client;
    }

    private async Task<int> CreateGroupAsync(HttpClient client, string location) {
        var date = DateOnly.FromDateTime(SportyWebApplicationFactory.Now.DateTime).AddDays(2);
        var response = await client.PostFormAsync("/Groups/Create", "/Groups/Create", new() {
            ["SportId"] = "1",
            ["CustomLocation"] = location,
            ["DistrictId"] = "9",
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["StartTime"] = "18:30",
            ["MinMembers"] = "4",
            ["MaxMembers"] = "10"
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SportyDbContext>();
        return db.Groups.Single(g => g.CustomLocation == location).GroupId;
    }

    private bool GroupExists(int groupId) {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SportyDbContext>().Groups.Any(g => g.GroupId == groupId);
    }

    private async Task<string> GetUserIdAsync(string email) {
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
        return user!.Id;
    }
}
