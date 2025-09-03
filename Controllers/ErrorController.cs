using Microsoft.AspNetCore.Mvc;
using DataReceiverService.Services;
using EzHttpListener;
using Game.Sdk.Report;
using DataReceiverService.Infos;

namespace DataReceiverService.Controllers
{
    [ApiController]
    [Route("")]
    public class ErrorController : ControllerBase
    {
        private readonly LogFilterManager _logFilterManager;

        public ErrorController(LogFilterManager logFilterManager)
        {
            _logFilterManager = logFilterManager;
        }

        [HttpPost(GlobalConfig.ErrorPosType)]
        public async Task<IActionResult> UpdateError()
        {
            using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            var requestBodyBytes = memoryStream.ToArray();
            
            // 智能检测数据格式，避免异常作为控制流
            string content;
            if (IsGZipData(requestBodyBytes))
            {
                try
                {
                    content = Tools.DecompressString(requestBodyBytes);
                    Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Decompressed data, original: {requestBodyBytes.Length}, after: {content.Length}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} GZip decompression failed: {ex.Message}");
                    return BadRequest($"Invalid compressed data: {ex.Message}");
                }
            }
            else
            {
                // 直接作为文本处理
                content = System.Text.Encoding.UTF8.GetString(requestBodyBytes);
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Processing raw text data, length: {content.Length}");
            }
            
            if (string.IsNullOrEmpty(content)) return BadRequest("Empty content after processing");
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            bool shouldLog = await _logFilterManager.ProcessLogAsync(content, clientIp);

            if (shouldLog)
            {
                Program.GetlogWriter().WriteLog(content, LogType.Log);
                Console.WriteLine($"{TimeHelper.GetUtc8Time().ToString("MM-dd HH:mm:ss:fff")} UpdateError Count:{Program.logWriter.Count} len:{requestBodyBytes.Length} after {content.Length} Received ip {clientIp}");
            }
            else
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time().ToString("MM-dd HH:mm:ss:fff")} UpdateError Filtered len:{requestBodyBytes.Length} after {content.Length} ip {clientIp}");
            }

            return Ok("OK");
        }
        /// <summary>
        /// 快速检测数据是否为GZip格式，避免异常开销
        /// </summary>
        /// <param name="data">待检测的字节数组</param>
        /// <returns>如果是GZip格式返回true</returns>
        private static bool IsGZipData(byte[] data)
        {
            // GZip 文件头：0x1f, 0x8b
            return data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b;
        }
    }
}
