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

        public IActionResult Add()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Search()
        {
            var userId = _userManager.GetUserId(User);
            var entries = await _dbContext.DiaryEntries
                .Where(e => e.Username == userId)
                .Include(e => e.Genres)
                .ToListAsync();

            return View(entries);
        }


        [HttpPost]
        public async Task<IActionResult> Add(DiaryEntry model, List<int> selectedGenreIds)
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
            else
            {
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int? id)
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

        [HttpPost]
        public async Task<IActionResult> UpdateEntry(DiaryEntry model, List<int> selectedGenreIds)
        {
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

            return RedirectToAction("Index", "Home");
        }



        [HttpPost]
        public async Task<IActionResult> SaveChanges(DiaryEntry model)
        {
            _dbContext.Update(model);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
            
        }

        public async Task<IActionResult> Remove(int? id)
        {
            var diaryEntry = await _dbContext.DiaryEntries.FindAsync(id);

            if (diaryEntry == null)
            {
                return RedirectToAction("Index", "Home");
            }

            _dbContext.DiaryEntries.Remove(diaryEntry);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> SearchUser(string searchUser)
        {
            var user = await _userManager.FindByNameAsync(searchUser);
            if (user == null)
            {
                return NotFound();
            }

            var userId = user.Id;

            var diaryEntries = await _dbContext.DiaryEntries
                .Where(entry => entry.Username == userId)
                .Include(entry => entry.Genres)
                .ToListAsync();

            return View("ViewSearch", diaryEntries);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToJson()
        {
            var userId = _userManager.GetUserId(User);
            var entries = await _dbContext.DiaryEntries
                .Where(e => e.Username == userId)
                .Include(e => e.Genres)
                .ToListAsync();

            var exportData = entries.Select(e => new
            {
                e.Title,
                e.Year,
                e.ResourceType,
                e.IsRead,
                Genres = e.Genres.Select(g => new { g.Id, g.Name }).ToList()
            });

            var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true //pro lepší čitelnost
            });

            return Content(json, "application/json");
        }
    }
}
