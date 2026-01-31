using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ModelExchange.Data;
using ModelExchange.Models;
using Microsoft.EntityFrameworkCore;
using ModelExchange.ViewModels;
using System.Security.Claims;

namespace ModelExchange.Controllers
{
    public class ModelsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ModelsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Upload()
        {
            return View(new UploadModel3DVm());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadModel3DVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var ext = Path.GetExtension(vm.File.FileName).ToLowerInvariant();
            if (ext != ".glb" && ext != ".gltf")
            {
                ModelState.AddModelError("File", "Dozvoljeni su samo .glb i .gltf modeli.");
                return View(vm);
            }

            const long maxBytes = 100 * 1024 * 1024;
            if (vm.File.Length > maxBytes)
            {
                ModelState.AddModelError("File", "Datoteka je prevelika (max 100 MB).");
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var fileName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(_env.WebRootPath, "uploads", "models", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

            using (var stream = System.IO.File.Create(savePath))
            {
                await vm.File.CopyToAsync(stream);
                Console.WriteLine("FILE EXISTS AFTER SAVE: " + System.IO.File.Exists(savePath));
            }

            var dbItem = new Model3D
            {
                OwnerUserId = userId,
                Name = vm.Name,
                Description = vm.Description,
                Tags = vm.Tags,
                Category = vm.Category,
                Visibility = vm.Visibility,
                FilePath = $"/uploads/models/{fileName}",
                CreatedAt = DateTime.UtcNow
            };

            _db.Models3D.Add(dbItem);
            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new { id = dbItem.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await _db.Models3D.FirstOrDefaultAsync(m => m.Id == id);
            if (model == null) return NotFound();

            if (model.Visibility == "private")
            {
                if (!(User.Identity?.IsAuthenticated ?? false))
                    return Challenge();

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != model.OwnerUserId)
                    return Forbid();
            }

            var owner = await _userManager.FindByIdAsync(model.OwnerUserId);
            ViewBag.OwnerDisplayName = owner?.DisplayName ?? owner?.UserName ?? "unknown";

            bool isFav = false;
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                isFav = await _db.Favorites.AnyAsync(f => f.UserId == userId && f.Model3DId == id);
            }
            ViewBag.IsFavorite = isFav;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? category, string? sort)
        {
            var q = _db.Models3D.AsQueryable();

            q = q.Where(m => m.Visibility == "public");

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(m => m.Name.Contains(search));

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(m => m.Category == category);

            q = sort switch
            {
                "name" => q.OrderBy(m => m.Name),
                "oldest" => q.OrderBy(m => m.CreatedAt),
                _ => q.OrderByDescending(m => m.CreatedAt)
            };

            var models = await q.ToListAsync();

            var categories = await _db.Models3D
                .Where(m => m.Visibility == "public" && m.Category != null && m.Category != "")
                .Select(m => m.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Search = search ?? "";
            ViewBag.Category = category ?? "";
            ViewBag.Sort = sort ?? "";

            var ownerIds = models.Select(m => m.OwnerUserId).Distinct().ToList();

            var owners = await _userManager.Users
                .Where(u => ownerIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName, u.UserName })
                .ToListAsync();

            var ownerMap = owners.ToDictionary(
                o => o.Id,
                o => o.DisplayName ?? o.UserName ?? "unknown"
            );

            var cards = models.Select(m => new ModelCardVm
            {
                Id = m.Id,
                Name = m.Name,
                Category = m.Category,
                Tags = m.Tags,
                CreatedAt = m.CreatedAt,

                OwnerUserName = ownerMap.TryGetValue(m.OwnerUserId, out var un) ? un : "unknown"
            }).ToList();

            return View(cards);
        }

        [HttpGet]
        public IActionResult DebugFile(string name)
        {
            var physical = Path.Combine(_env.WebRootPath, "uploads", "models", name);
            var exists = System.IO.File.Exists(physical);

            return Content($"WEBROOT={_env.WebRootPath}\nPHYSICAL={physical}\nEXISTS={exists}\nURL=/uploads/models/{name}", "text/plain");
        }

        [Authorize]
        public async Task<IActionResult> My(string? visibility, string? search)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var q = _db.Models3D.Where(m => m.OwnerUserId == userId);

            if (!string.IsNullOrWhiteSpace(visibility))
                q = q.Where(m => m.Visibility == visibility);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(m => m.Name.Contains(search));

            var models = await q
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Visibility = visibility ?? "";
            ViewBag.Search = search ?? "";

            return View(models);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _db.Models3D.FirstOrDefaultAsync(m => m.Id == id);
            if (model == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != model.OwnerUserId) return Forbid();

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _db.Models3D.FirstOrDefaultAsync(m => m.Id == id);
            if (model == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != model.OwnerUserId) return Forbid();

            var relative = model.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physical = Path.Combine(_env.WebRootPath, relative);

            if (System.IO.File.Exists(physical))
                System.IO.File.Delete(physical);

            _db.Models3D.Remove(model);
            await _db.SaveChangesAsync();

            return RedirectToAction("My");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _db.Models3D.FirstOrDefaultAsync(m => m.Id == id);
            if (model == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != model.OwnerUserId) return Forbid();

            var vm = new EditModel3DVm
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Tags = model.Tags,
                Category = model.Category,
                Visibility = model.Visibility
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditModel3DVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var model = await _db.Models3D.FirstOrDefaultAsync(m => m.Id == vm.Id);
            if (model == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != model.OwnerUserId) return Forbid();

            model.Name = vm.Name;
            model.Description = vm.Description;
            model.Tags = vm.Tags;
            model.Category = vm.Category;
            model.Visibility = vm.Visibility;

            await _db.SaveChangesAsync();

            return RedirectToAction("My");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.Model3DId == id);

            if (existing == null)
            {
                _db.Favorites.Add(new Favorite { UserId = userId!, Model3DId = id });
            }
            else
            {
                _db.Favorites.Remove(existing);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Details", new { id });
        }

        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var models = await _db.Favorites
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.Model3D)
                .ToListAsync();

            return View(models);
        }
    }
}
