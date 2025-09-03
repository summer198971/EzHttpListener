using Microsoft.AspNetCore.Mvc;
using DataReceiverService.Services;
using DataReceiverService.Infos;

namespace DataReceiverService.Controllers
{
    [ApiController]
    [Route("")]
    public class ConfigController : ControllerBase
    {
        private readonly LogFilterManager _logFilterManager;

        public ConfigController(LogFilterManager logFilterManager)
        {
            _logFilterManager = logFilterManager;
        }

        [HttpPost(GlobalConfig.ConfigReloadType)]
        public IActionResult ReloadConfig()
        {
            _logFilterManager.ReloadConfig();
            return Ok(new { message = "Config reloaded successfully" });
        }

        [HttpGet(GlobalConfig.ConfigStatusType)]
        public IActionResult GetConfigStatus()
        {
            string statistics = _logFilterManager.GetStatistics();
            return Content(statistics, "application/json");
        }
    }
}
