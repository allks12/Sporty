using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sporty.Data.Identity;
using Sporty.Services.Common;

namespace Sporty.Services.Identity;

public interface IUserAdminService {
    Task<List<ApplicationUser>> ListAsync();
    Task<OperationResult<string>> DeleteAsync(string userId, string currentAdminId);
}

public class UserAdminService : IUserAdminService {
    private readonly UserManager<ApplicationUser> userManager;

    public UserAdminService(UserManager<ApplicationUser> userManager) {
        this.userManager = userManager;
    }

    public Task<List<ApplicationUser>> ListAsync() =>
        userManager.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Name)
            .ToListAsync();

    //The user is only marked as deleted, so their groups stay; the login page rejects deleted users
    public async Task<OperationResult<string>> DeleteAsync(string userId, string currentAdminId) {
        if (userId == currentAdminId) {
            return OperationResult<string>.Failure(ErrorCode.Validation, "Не можеш да изтриеш себе си.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsDeleted) {
            return OperationResult<string>.Failure(ErrorCode.NotFound, "Потребителят не съществува.");
        }

        user.IsDeleted = true;
        await userManager.UpdateAsync(user);
        return OperationResult<string>.Success(user.Id);
    }
}
