using Microsoft.AspNetCore.Mvc;

namespace KanbanBoard.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
