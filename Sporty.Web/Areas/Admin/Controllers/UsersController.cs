using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sporty.Data.Identity;
using Sporty.Services.Identity;

namespace Sporty.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller {
    private readonly IUserAdminService userAdminService;
    private readonly UserManager<ApplicationUser> userManager;

    public UsersController(IUserAdminService userAdminService, UserManager<ApplicationUser> userManager) {
        this.userAdminService = userAdminService;
        this.userManager = userManager;
    }

    public async Task<IActionResult> Index() {
        return View(await userAdminService.ListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id) {
        var result = await userAdminService.DeleteAsync(id, userManager.GetUserId(User)!);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.First());
        }

        return RedirectToAction(nameof(Index));
    }
}
