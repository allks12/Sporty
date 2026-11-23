using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sporty.Data.Identity;
using Sporty.Services.Groups;

namespace Sporty.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class GroupsController : Controller {
    private readonly IGroupService groupService;
    private readonly UserManager<ApplicationUser> userManager;

    public GroupsController(IGroupService groupService, UserManager<ApplicationUser> userManager) {
        this.groupService = groupService;
        this.userManager = userManager;
    }

    public async Task<IActionResult> Index() {
        return View(await groupService.ListAllAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id) {
        var result = await groupService.DeleteAsync(id, userManager.GetUserId(User)!, isAdmin: true);
        if (!result.Succeeded) {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}
