using Diary.Data;
using Diary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Diary.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // Základní stránka se seznamem záznamù
        public async Task<IActionResult> Index(bool? isRead = null)
        {
            var userId = _userManager.GetUserId(User);

            var query = _dbContext.DiaryEntries
                .Include(e => e.Genres)
                .Where(e => e.Username == userId);

            if (isRead.HasValue)
            {
                query = query.Where(e => e.IsRead == isRead.Value);
            }

            var entries = await query.ToListAsync();

            var favoriteIds = await _dbContext.FavoriteEntries
                .Where(f => f.UserId == userId)
                .Select(f => f.DiaryEntryId)
                .ToListAsync();

            ViewBag.FavoriteIds = favoriteIds;
            ViewBag.IsReadFilter = isRead;

            return View(entries);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
