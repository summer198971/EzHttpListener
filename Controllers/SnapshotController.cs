using Microsoft.AspNetCore.Mvc;
using DataReceiverService.Infos;
using EzHttpListener; // Assuming TimeHelper is in this namespace

namespace DataReceiverService.Controllers
{
    [ApiController]
    [Route("")]
    public class SnapshotController : ControllerBase
    {
        private readonly SnapshotManager _snapshotManager;

        public SnapshotController(SnapshotManager snapshotManager)
        {
            _snapshotManager = snapshotManager;
        }

        [HttpPost(GlobalConfig.SnapshootType)]
        public async Task<IActionResult> UpdateSnapshot()
        {
            using var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8);
            string requestBody = await reader.ReadToEndAsync();
            var (info, type) = _snapshotManager.ParseSnapshoot(requestBody);
            Console.WriteLine($"{TimeHelper.GetUtc8Time().ToString("MM-dd HH:mm:ss:fff")} UpdateSnapshot Received type: {info.fileType} ip {HttpContext.Connection.RemoteIpAddress} len:{requestBody.Length}");
            await _snapshotManager.StoreSnapshotAsync(info);
            return Ok();
        }
    }
}
