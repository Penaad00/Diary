using Diary.Data;
using Diary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var diaryEntry = await _dbContext.DiaryEntries.FindAsync(id);

            var currentUserId = _userManager.GetUserId(User);

            return View(diaryEntry);
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


    }
}
