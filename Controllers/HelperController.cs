using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DataReceiverService.Controllers
{
    [ApiController]
    [Route("helper")]
    public class HelperController : ControllerBase
    {
        [HttpGet]
        [HttpPost]
        public IActionResult GetHelper()
        {
            var responseText = @"
EzHttpListener API 接口说明
=================================

配套项目: EZLogger Unity日志系统
项目地址: https://github.com/summer198971/EZLogger

=== 核心接口 ===

1. 游戏快照上传
   - 接口地址: /snapshoot
   - 请求方式: POST
   - 功能说明: 接收EZLogger发送的游戏快照数据
   - 内容类型: application/json
   - 请求体: SnapShootInfo 对象
   - 支持类型: 文本、图片、XML、二进制、CSV
   - 对应功能: EZLogger文件输出器上传

2. 错误日志上传
   - 接口地址: /errorpos
   - 请求方式: POST
   - 功能说明: 接收EZLogger压缩的错误日志数据
   - 内容类型: application/octet-stream
   - 请求体: GZip压缩的日志内容
   - 特性: 智能GZip检测、日志过滤、飞书警报
   - 对应功能: EZLogger错误上报功能

3. 日志开关控制
   - /openlog   (GET) - 查询日志开关状态
   - /closelog  (GET) - 关闭日志记录 (远程控制EZLogger.DisableAll())
   - /doopenlog (GET) - 开启日志记录 (远程控制EZLogger.EnableAll())
   - 对应功能: 配合EZLogger动态级别控制

4. 配置管理
   - /config/status (GET)  - 获取过滤配置状态和统计信息
   - /config/reload (POST) - 手动重新加载过滤配置文件
   - 返回类型: application/json
   - 对应功能: 服务端过滤配置监控和热重载

5. 帮助文档
   - 接口地址: /helper
   - 请求方式: GET/POST
   - 功能说明: 获取本API接口说明文档

=== 技术特性 ===
✓ 高性能异步处理 - 处理EZLogger的高并发日志上传
✓ 自动GZip解压 - 智能识别和解压EZLogger发送的压缩数据
✓ 智能日志过滤 - 基于正则表达式避免日志洪水
✓ 飞书警报通知 - 关键错误实时推送
✓ 配置热重载 - 无需重启服务即可更新过滤规则
✓ 文件自动分割 - 防止日志文件过大

=== 使用说明 ===
1. 在Unity项目中集成EZLogger客户端
2. 配置EZLogger连接到本服务: http://your-server:5116
3. 启用EZLogger的服务器上报功能
4. 本服务自动接收、过滤、存储日志数据

=== 数据存储 ===
- 日志文件: /share_data/logs/
- 快照文件: /share_data/snapshoot/
- 配置文件: /share_data/config/log_filter_config.json

更多EZLogger使用方法请参考: https://github.com/summer198971/EZLogger
";

            return Content(responseText, "text/plain; charset=utf-8");
        }
    }
}
