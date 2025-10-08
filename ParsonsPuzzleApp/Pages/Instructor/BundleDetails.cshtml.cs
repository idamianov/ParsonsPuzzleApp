using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Data;
using ParsonsPuzzleApp.Entities;
using ParsonsPuzzleApp.Interfaces;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Pages.Instructor
{
    [Authorize]
    public class BundleDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBundleAnalysisService _analysisService;

        public BundleDetailsModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IBundleAnalysisService analysisService)
        {
            _context = context;
            _userManager = userManager;
            _analysisService = analysisService;
        }

        public Bundle Bundle { get; set; }
        public string ShareableUrl { get; set; }
        public CollectionLanguageAnalysis LanguageAnalysis { get; set; } = new CollectionLanguageAnalysis();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            Bundle = await _context.Bundles
                .Include(b => b.BundlePuzzles)
                .ThenInclude(bp => bp.Puzzle)
                .ThenInclude(p => p.Language)
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (Bundle == null)
            {
                return NotFound();
            }

            // Generate the full shareable URL
            var request = HttpContext.Request;
            ShareableUrl = $"{request.Scheme}://{request.Host}/bundle/{Bundle.ShareableLink}";

            LanguageAnalysis = _analysisService.AnalyzeLanguages(Bundle);

            return Page();
        }

        public async Task<IActionResult> OnPostPublishAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (bundle == null)
            {
                return NotFound();
            }

            bundle.IsPublished = true;
            bundle.LastModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Колекцията беше успешно публикувана!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Грешка при публикуване на колекцията.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUnpublishAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var bundle = await _context.Bundles
                .FirstOrDefaultAsync(b => b.Id == id && b.InstructorId == userId);

            if (bundle == null)
            {
                return NotFound();
            }

            bundle.IsPublished = false;
            bundle.LastModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Колекцията беше успешно скрита!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Грешка при скриване на колекцията.";
            }

            return RedirectToPage(new { id });
        }
    }
}