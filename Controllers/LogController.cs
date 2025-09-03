using Microsoft.AspNetCore.Mvc;
using DataReceiverService.Infos;

namespace DataReceiverService.Controllers
{
    [ApiController]
    public class LogController : ControllerBase
    {
        private static bool isOpenLog = true;

        [HttpGet(GlobalConfig.OpenLogType)]
        public IActionResult GetOpenLog()
        {
            string json = "{\"log\":\"" + (isOpenLog ? "open" : "close") + "\"}";
            return Content(json, "text/plain; charset=utf-8");
        }

        [HttpGet(GlobalConfig.CloseLogType)]
        public IActionResult CloseLog()
        {
            isOpenLog = false;
            return Content("OK close log", "text/plain; charset=utf-8");
        }

        [HttpGet(GlobalConfig.DoOpenLogType)]
        public IActionResult DoOpenLog()
        {
            isOpenLog = true;
            return Content("OK open log", "text/plain; charset=utf-8");
        }
    }
}
