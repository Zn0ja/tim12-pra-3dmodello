using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        ViewBag.CurrentDisplayName = user.DisplayName ?? user.UserName ?? "";

        return View(model: "");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? displayName)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);

            ViewBag.CurrentDisplayName = user.DisplayName ?? user.UserName ?? "";
            return View(model: displayName ?? "");
        }

        TempData["Message"] = "ProfileUpdated";
        return RedirectToAction(nameof(Edit));
    }
}
