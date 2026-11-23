using Microsoft.AspNetCore.Identity;
using Sporty.Data.Identity;

namespace Sporty.Services.Identity;

//Creates the roles, the administrator and the demo user. Safe to run on every start.
public class IdentitySeeder {
    public const string AdminName = "Administrator";
    public const string AdminEmail = "admin@sporty.bg";
    public const string AdminPassword = "Admin123!";

    public const string DemoUserName = "Demo User";
    public const string DemoUserEmail = "user@sporty.bg";
    public const string DemoUserPassword = "User123!";

    private readonly RoleManager<IdentityRole> roleManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly TimeProvider timeProvider;

    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        TimeProvider timeProvider) {
        this.roleManager = roleManager;
        this.userManager = userManager;
        this.timeProvider = timeProvider;
    }

    public async Task SeedAsync() {
        foreach (var role in AppRoles.All) {
            if (!await roleManager.RoleExistsAsync(role)) {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"create role {role}");
            }
        }

        await SeedUserAsync(AdminName, AdminEmail, AdminPassword, AppRoles.Admin);
        await SeedUserAsync(DemoUserName, DemoUserEmail, DemoUserPassword, AppRoles.User);
    }

    private async Task SeedUserAsync(string name, string email, string password, string role) {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) {
            user = new ApplicationUser {
                Name = name,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
            };
            EnsureSucceeded(await userManager.CreateAsync(user, password), $"create user {email}");
        }

        if (!await userManager.IsInRoleAsync(user, role)) {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, role), $"add user {email} to the {role} role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string action) {
        if (!result.Succeeded) {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not {action}: {errors}");
        }
    }
}
