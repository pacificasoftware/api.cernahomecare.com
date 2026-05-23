using Microsoft.AspNetCore.Mvc;

namespace CernaHomeCare.AdminApi.Controllers
{
    public class AuditLogsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
