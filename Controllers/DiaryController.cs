using Diary.Data;
using Diary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Diary.Controllers
{
    public class DiaryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public DiaryController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // Zobrazí formulář pro přidání záznamu
        public async Task<IActionResult> Add()
        {
            ViewBag.AllGenres = await _dbContext.Genres.ToListAsync();
            return View();
        }

        // Přidání nového záznamu POST
        [HttpPost]
        public async Task<IActionResult> Add(DiaryEntry model, List<int> selectedGenreIds, bool? isRead = null)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                model.Username = userId;

                model.Genres = await _dbContext.Genres
                    .Where(g => selectedGenreIds.Contains(g.Id))
                    .ToListAsync();

                _dbContext.DiaryEntries.Add(model);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Index", "Home");

            }

            ViewBag.AllGenres = await _dbContext.Genres.ToListAsync();
            return View(model);
        }

        // Zobrazí formulář pro editaci záznamu
        public async Task<IActionResult> Edit(int? id, bool? isRead = null)
        {
            if (id == null) return NotFound();

            var diaryEntry = await _dbContext.DiaryEntries
                .Include(e => e.Genres)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (diaryEntry == null) return NotFound();

            ViewBag.AllGenres = await _dbContext.Genres.ToListAsync();
            ViewBag.SelectedGenreIds = diaryEntry.Genres.Select(g => g.Id).ToList();

            return View(diaryEntry);
        }

        // Uložení upraveného záznamu POST
        [HttpPost]
        public async Task<IActionResult> UpdateEntry(DiaryEntry model, List<int> selectedGenreIds, bool? isRead = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AllGenres = await _dbContext.Genres.ToListAsync();
                ViewBag.SelectedGenreIds = selectedGenreIds;
                return View("Edit", model);
            }

            var entryToUpdate = await _dbContext.DiaryEntries
                .Include(e => e.Genres)
                .FirstOrDefaultAsync(e => e.Id == model.Id);


            entryToUpdate.Title = model.Title;
            entryToUpdate.Year = model.Year;
            entryToUpdate.IsRead = model.IsRead;
            entryToUpdate.ResourceType = model.ResourceType;

            entryToUpdate.Genres.Clear();
            var selectedGenres = await _dbContext.Genres.Where(g => selectedGenreIds.Contains(g.Id)).ToListAsync();
            foreach (var genre in selectedGenres)
            {
                entryToUpdate.Genres.Add(genre);
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Home", new { isRead = isRead });

        }

        // Odstranění záznamu
        public async Task<IActionResult> Remove(int? id, bool? isRead = null)
        {
            if (id == null) return NotFound();

            var diaryEntry = await _dbContext.DiaryEntries.FindAsync(id);
            if (diaryEntry == null) return NotFound();

            _dbContext.DiaryEntries.Remove(diaryEntry);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Home", new { isRead = isRead });

        }

        // Přepne oblíbený záznam
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int entryId, bool? isRead = null)
        {
            var userId = _userManager.GetUserId(User);

            var existing = await _dbContext.FavoriteEntries
                .FirstOrDefaultAsync(f => f.UserId == userId && f.DiaryEntryId == entryId);

            if (existing != null)
            {
                _dbContext.FavoriteEntries.Remove(existing);
            }
            else
            {
                var favorite = new FavoriteEntry
                {
                    UserId = userId,
                    DiaryEntryId = entryId,
                    FavoritedAt = DateTime.UtcNow
                };
                _dbContext.FavoriteEntries.Add(favorite);
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Home", new { isRead = isRead });

        }

        // Export do JSON
        [HttpGet]
        public async Task<IActionResult> ExportToJson()
        {
            var userId = _userManager.GetUserId(User);

            var entries = await _dbContext.DiaryEntries
                .Where(e => e.Username == userId)
                .Include(e => e.Genres)
                .ToListAsync();

            var favoriteEntryIds = await _dbContext.FavoriteEntries
                .Where(f => f.UserId == userId)
                .Select(f => f.DiaryEntryId)
                .ToListAsync();

            var exportData = entries.Select(e => new
            {
                e.Title,
                e.Year,
                e.ResourceType,
                e.IsRead,
                Genres = e.Genres.Select(g => new { g.Id, g.Name }).ToList(),
                IsFavorite = favoriteEntryIds.Contains(e.Id)
            });

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });

            return Content(json, "application/json");
        }

        public IActionResult Search()
        {
            return View();
        }

        // Vrátí ViewSearch s aktuálně přihlášeným uživatelem
        public async Task<IActionResult> SearchMe()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ViewBag.Comments = new List<Comment>();
                ViewBag.UserNames = new Dictionary<string, string>();
                return View("ViewSearch", new List<DiaryEntry>());
            }

            var entries = await _dbContext.DiaryEntries
                .Include(e => e.Genres)
                .Where(e => e.Username == user.Id)
                .ToListAsync();

            var entryIds = entries.Select(e => e.Id).ToList();

            var comments = await _dbContext.Comments
                .Where(c => entryIds.Contains(c.DiaryEntryId))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var userIds = comments.Select(c => c.UserId).Distinct();

            var userNamesDict = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            ViewBag.Comments = comments;
            ViewBag.UserNames = userNamesDict;

            return View("ViewSearch", entries);
        }


        // Vrátí ViewSearch podle zadaného uživatele
        [HttpPost]
        public async Task<IActionResult> SearchUser(string searchUser)
        {
            var user = await _userManager.FindByNameAsync(searchUser);
            if (user == null)
            {
                ViewBag.Comments = new List<Comment>();
                ViewBag.UserNames = new Dictionary<string, string>();
                return View("ViewSearch", new List<DiaryEntry>());
            }

            var entries = await _dbContext.DiaryEntries
                .Include(e => e.Genres)
                .Where(e => e.Username == user.Id)
                .ToListAsync();

            var entryIds = entries.Select(e => e.Id).ToList();

            var comments = await _dbContext.Comments
                .Where(c => entryIds.Contains(c.DiaryEntryId))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var userIds = comments.Select(c => c.UserId).Distinct();

            var userNamesDict = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            ViewBag.Comments = comments;
            ViewBag.UserNames = userNamesDict;

            return View("ViewSearch", entries);
        }


        // Přidání komentáře
        [HttpPost]
        public async Task<IActionResult> AddComment(int diaryEntryId, string content)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return RedirectToAction("Search");
            }

            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Search");

            var comment = new Comment
            {
                UserId = userId,
                DiaryEntryId = diaryEntryId,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _dbContext.Comments.Add(comment);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Search");
        }

        // Vrátí stránku s statistikami
        public async Task<IActionResult> MyStats()
        {
            var userId = _userManager.GetUserId(User);

            var entries = await _dbContext.DiaryEntries
                .Where(e => e.Username == userId)
                .ToListAsync();

            var statistics = new UserStatistics
            {
                UserId = userId,
                EntriesCount = entries.Count,
                ReadBooksCount = entries.Count(e => e.IsRead && e.ResourceType == "Kniha"),
                SeenFilmsCount = entries.Count(e => e.IsRead && e.ResourceType == "Film")
            };

            return View(statistics);
        }




    }
}
