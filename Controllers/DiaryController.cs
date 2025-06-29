using Microsoft.AspNetCore.Mvc;

namespace Diary.Controllers
{
    public class DiaryController : Controller
    {
        public IActionResult Add()
        {
            return View();
        }
    }
}
