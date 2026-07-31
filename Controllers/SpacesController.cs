using CoworkingSpace.Web.Data;
using CoworkingSpace.Web.Models;
using CoworkingSpace.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoworkingSpace.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SpacesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SpacesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Spaces
    public async Task<IActionResult> Index()
    {
        var spaces = await _context.Spaces
            .Include(s => s.Images.OrderBy(i => i.DisplayOrder))
            .Include(s => s.SpaceEquipments)
                .ThenInclude(se => se.Equipment)
            .OrderBy(s => s.SpaceName)
            .ToListAsync();
        return View(spaces);
    }


    // GET: Spaces/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var space = await _context.Spaces
            .Include(s => s.SpaceEquipments).ThenInclude(se => se.Equipment)
            .Include(s => s.Images.OrderBy(i => i.DisplayOrder))
            .Include(s => s.Prices)
            .Include(s => s.Maintenances).ThenInclude(m => m.Staff)
            .FirstOrDefaultAsync(s => s.SpaceId == id);

        if (space == null) return NotFound();
        return View(space);
    }

    // GET: Spaces/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Spaces/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("SpaceName,SpaceType,Capacity,IsActive,Location")] Space space,
        List<IFormFile> imageFiles,
        string imageData)
    {
        // ===== دیباگ: بررسی مقادیر دریافتی =====
        var debugInfo = new List<string>();
        debugInfo.Add($"ModelState.IsValid: {ModelState.IsValid}");
        debugInfo.Add($"imageFiles Count: {imageFiles?.Count ?? 0}");
        debugInfo.Add($"imageData: {(string.IsNullOrEmpty(imageData) ? "EMPTY" : imageData.Substring(0, Math.Min(100, imageData.Length)))}...");
        TempData["DebugInfo"] = string.Join(" | ", debugInfo);

        // ===== اعتبارسنجی =====
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["Error"] = $"Validation failed: {string.Join(" | ", errors)}";
            return View(space);
        }

        // ===== بررسی وجود تصویر =====
        if (imageFiles == null || !imageFiles.Any())
        {
            ModelState.AddModelError(string.Empty, "At least one image is required.");
            TempData["Error"] = "At least one image is required.";
            return View(space);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            space.CreatedAt = DateTime.UtcNow;
            space.UpdatedAt = DateTime.UtcNow;
            _context.Spaces.Add(space);
            await _context.SaveChangesAsync();

            // ===== پردازش تصاویر =====
            var imageInfoList = JsonSerializer.Deserialize<List<ImageUploadInfo>>(imageData ?? "[]");
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "spaces");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            for (int i = 0; i < imageFiles.Count; i++)
            {
                var file = imageFiles[i];
                var info = imageInfoList?.FirstOrDefault(x => x.FileName == file.FileName);

                if (file != null && file.Length > 0)
                {
                    var fileName = $"{space.SpaceId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var spaceImage = new SpaceImage
                    {
                        SpaceId = space.SpaceId,
                        ImagePath = $"/images/spaces/{fileName}",
                        DisplayOrder = info?.Order ?? i,
                        IsPrimary = info?.IsPrimary ?? (i == 0)
                    };
                    _context.SpaceImages.Add(spaceImage);
                }
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            TempData["Success"] = "Space created successfully with images.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // حذف فضای ایجادشده
            var existingSpace = await _context.Spaces.FindAsync(space.SpaceId);
            if (existingSpace != null)
            {
                _context.Spaces.Remove(existingSpace);
                await _context.SaveChangesAsync();
            }
            TempData["Error"] = $"Error: {ex.Message}";
            if (ex.InnerException != null)
                TempData["Error"] += $" | Inner: {ex.InnerException.Message}";
            return View(space);
        }
    }

    // GET: Spaces/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var space = await _context.Spaces
            .Include(s => s.Images.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(s => s.SpaceId == id);

        if (space == null) return NotFound();
        return View(space);
    }

    // POST: Spaces/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("SpaceId,SpaceName,SpaceType,Capacity,IsActive,Location,CreatedAt")] Space space,
        List<IFormFile> imageFiles,
        string imageData,
        string deletedImageIds)
    {
        //****************************************************************************************
        // ===== دیباگ: تعداد فایل‌های دریافتی =====
        var fileCount = Request.Form.Files.Count;
        TempData["DebugInfo"] = $"Request.Form.Files.Count: {fileCount} | imageFiles.Count: {imageFiles?.Count ?? 0}";


        if (id != space.SpaceId) return NotFound();

        // ===== دیباگ =====
        var debugInfo = new List<string>();
        debugInfo.Add($"ModelState.IsValid: {ModelState.IsValid}");
        debugInfo.Add($"imageFiles Count: {imageFiles?.Count ?? 0}");
        debugInfo.Add($"imageData: {(string.IsNullOrEmpty(imageData) ? "EMPTY" : imageData.Substring(0, Math.Min(100, imageData.Length)))}...");
        debugInfo.Add($"deletedImageIds: {(string.IsNullOrEmpty(deletedImageIds) ? "EMPTY" : deletedImageIds)}");
        TempData["DebugInfo"] = string.Join(" | ", debugInfo);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["Error"] = $"Validation failed: {string.Join(" | ", errors)}";
            // بارگذاری مجدد تصاویر موجود
            var existingSpace = await _context.Spaces
                .Include(s => s.Images.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(s => s.SpaceId == id);
            if (existingSpace != null)
                space.Images = existingSpace.Images;
            return View(space);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            space.UpdatedAt = DateTime.UtcNow;
            _context.Update(space);
            await _context.SaveChangesAsync();

            // ---- حذف تصاویر ----
            if (!string.IsNullOrEmpty(deletedImageIds))
            {
                var deletedIds = JsonSerializer.Deserialize<List<int>>(deletedImageIds);
                if (deletedIds != null && deletedIds.Any())
                {
                    var imagesToDelete = await _context.SpaceImages
                        .Where(i => deletedIds.Contains(i.ImageId) && i.SpaceId == space.SpaceId)
                        .ToListAsync();
                    if (imagesToDelete.Any())
                    {
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        foreach (var img in imagesToDelete)
                        {
                            var filePath = Path.Combine(uploadDir, img.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(filePath))
                                System.IO.File.Delete(filePath);
                        }
                        _context.SpaceImages.RemoveRange(imagesToDelete);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // ---- اضافه کردن تصاویر جدید ----
            var imageInfoList = JsonSerializer.Deserialize<List<ImageUploadInfo>>(imageData ?? "[]");
            if (imageFiles != null && imageFiles.Any())
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "spaces");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                var newFiles = imageInfoList?.Where(x => x.Source == "new").ToList() ?? new List<ImageUploadInfo>();

                for (int i = 0; i < imageFiles.Count; i++)
                {
                    var file = imageFiles[i];
                    var info = newFiles.FirstOrDefault(x => x.FileName == file.FileName);

                    if (file != null && file.Length > 0)
                    {
                        var fileName = $"{space.SpaceId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(uploadDir, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var spaceImage = new SpaceImage
                        {
                            SpaceId = space.SpaceId,
                            ImagePath = $"/images/spaces/{fileName}",
                            DisplayOrder = info?.Order ?? i,
                            IsPrimary = info?.IsPrimary ?? false
                        };
                        _context.SpaceImages.Add(spaceImage);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // ---- به‌روزرسانی اطلاعات تصاویر موجود ----
            if (imageInfoList != null && imageInfoList.Any(x => x.Source == "existing"))
            {
                var existingInfo = imageInfoList.Where(x => x.Source == "existing").ToList();
                foreach (var info in existingInfo)
                {
                    var image = await _context.SpaceImages.FindAsync(info.ImageId);
                    if (image != null && image.SpaceId == space.SpaceId)
                    {
                        image.DisplayOrder = info.Order;
                        image.IsPrimary = info.IsPrimary;
                    }
                }
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            TempData["Success"] = "Space updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = $"Error: {ex.Message}";
            if (ex.InnerException != null)
                TempData["Error"] += $" | Inner: {ex.InnerException.Message}";

            var existingSpace = await _context.Spaces
                .Include(s => s.Images.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(s => s.SpaceId == id);
            if (existingSpace != null)
                space.Images = existingSpace.Images;
            return View(space);
        }
    }

    // GET: Spaces/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var space = await _context.Spaces
            .Include(s => s.Prices)
            .Include(s => s.SpaceEquipments)
            .Include(s => s.Maintenances)
            .Include(s => s.Reservations)
            .FirstOrDefaultAsync(s => s.SpaceId == id);

        if (space == null) return NotFound();

        var hasRelatedPrices = space.Prices != null && space.Prices.Any();
        var hasRelatedEquipment = space.SpaceEquipments != null && space.SpaceEquipments.Any();
        var hasRelatedMaintenance = space.Maintenances != null && space.Maintenances.Any();
        var hasRelatedReservations = space.Reservations != null && space.Reservations.Any();

        ViewBag.HasRelatedPrices = hasRelatedPrices;
        ViewBag.HasRelatedEquipment = hasRelatedEquipment;
        ViewBag.HasRelatedMaintenance = hasRelatedMaintenance;
        ViewBag.HasRelatedReservations = hasRelatedReservations;

        var errorMessages = new List<string>();
        if (hasRelatedPrices) errorMessages.Add($"{space.Prices.Count()} price(s)");
        if (hasRelatedEquipment) errorMessages.Add($"{space.SpaceEquipments.Count()} equipment assignment(s)");
        if (hasRelatedMaintenance) errorMessages.Add($"{space.Maintenances.Count()} maintenance record(s)");
        if (hasRelatedReservations) errorMessages.Add($"{space.Reservations.Count()} reservation(s)");

        ViewBag.ErrorMessage = errorMessages.Any()
            ? $"This space has related records: {string.Join(", ", errorMessages)}. Please delete them first."
            : null;

        return View(space);
    }

    // POST: Spaces/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var space = await _context.Spaces
            .Include(s => s.Prices)
            .Include(s => s.SpaceEquipments)
            .Include(s => s.Maintenances)
            .Include(s => s.Reservations)
            .FirstOrDefaultAsync(s => s.SpaceId == id);

        if (space == null) return NotFound();

        if (space.Prices != null && space.Prices.Any() ||
            space.SpaceEquipments != null && space.SpaceEquipments.Any() ||
            space.Maintenances != null && space.Maintenances.Any() ||
            space.Reservations != null && space.Reservations.Any())
        {
            TempData["Error"] = "Cannot delete this space because it has related records (prices, equipment, maintenance, or reservations). Please delete them first.";
            return RedirectToAction(nameof(Delete), new { id = space.SpaceId });
        }

        _context.Spaces.Remove(space);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Space deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private bool SpaceExists(int id) => _context.Spaces.Any(e => e.SpaceId == id);

    // ===== کلاس کمکی برای Deserialize کردن داده‌های JSON =====
    public class ImageUploadInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // "existing" یا "new"
        public int ImageId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int Order { get; set; }
    }
    
}