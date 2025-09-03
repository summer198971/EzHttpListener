using System.Text;
using System.Text.Json;
using DataReceiverService.Config;
using EzHttpListener;

namespace DataReceiverService.Services
{
    /// <summary>
    /// 飞书警报服务类 - 负责发送飞书通知和管理冷却时间
    /// </summary>
    public class FeishuAlertService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, DateTime> _lastAlertTimes; // 记录每个规则的最后警报时间
        private readonly object _lockObject = new object(); // 线程安全锁
        
        /// <summary>
        /// 构造函数 - 初始化HTTP客户端和警报时间记录
        /// </summary>
        public FeishuAlertService()
        {
            _httpClient = new HttpClient();
            _lastAlertTimes = new Dictionary<string, DateTime>();
        }
        
        /// <summary>
        /// 发送飞书警报通知
        /// </summary>
        /// <param name="webhook">飞书机器人Webhook地址</param>
        /// <param name="rule">触发的警报规则</param>
        /// <param name="logContent">日志内容</param>
        /// <param name="clientIp">客户端IP地址</param>
        public async Task SendAlertAsync(string webhook, AlertRule rule, string logContent, string clientIp)
        {
            if (string.IsNullOrEmpty(webhook) || !rule.Enabled)
                return;
                
            // 检查冷却时间 - 避免频繁发送相同警报
            if (!CanSendAlert(rule.Id, rule.Cooldown))
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Alert {rule.Name} is in cooldown period");
                return;
            }
            
            try
            {
                // 创建警报消息内容
                var message = CreateAlertMessage(rule, logContent, clientIp);
                var jsonContent = JsonSerializer.Serialize(new { msg_type = "text", content = new { text = message } });
                
                // 发送HTTP POST请求到飞书Webhook
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhook, content);
                
                if (response.IsSuccessStatusCode)
                {
                    // 更新最后警报时间，启动冷却期
                    UpdateLastAlertTime(rule.Id);
                    Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Feishu alert sent successfully for rule: {rule.Name}");
                }
                else
                {
                    Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Failed to send Feishu alert: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error sending Feishu alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查是否可以发送警报（冷却时间检查）
        /// </summary>
        /// <param name="ruleId">规则ID</param>
        /// <param name="cooldownSeconds">冷却时间（秒）</param>
        /// <returns>是否可以发送警报</returns>
        private bool CanSendAlert(string ruleId, int cooldownSeconds)
        {
            lock (_lockObject)
            {
                // 如果没有发送过警报，可以发送
                if (!_lastAlertTimes.ContainsKey(ruleId))
                    return true;
                    
                // 检查距离上次警报的时间是否超过冷却期
                var timeSinceLastAlert = DateTime.UtcNow - _lastAlertTimes[ruleId];
                return timeSinceLastAlert.TotalSeconds >= cooldownSeconds;
            }
        }
        
        /// <summary>
        /// 更新规则的最后警报时间
        /// </summary>
        /// <param name="ruleId">规则ID</param>
        private void UpdateLastAlertTime(string ruleId)
        {
            lock (_lockObject)
            {
                _lastAlertTimes[ruleId] = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// 创建飞书警报消息内容
        /// </summary>
        /// <param name="rule">警报规则</param>
        /// <param name="logContent">日志内容</param>
        /// <param name="clientIp">客户端IP</param>
        /// <returns>格式化的警报消息</returns>
        private string CreateAlertMessage(AlertRule rule, string logContent, string clientIp)
        {
            var timestamp = TimeHelper.GetUtc8Time().ToString("yyyy-MM-dd HH:mm:ss");
            // 限制日志内容预览长度，避免消息过长
            var preview = logContent.Length > 500 ? logContent.Substring(0, 500) + "..." : logContent;
            
            return $"🚨 日志警报: {rule.Name}\n" +
                   $"📅 时间: {timestamp}\n" +
                   $"🌐 客户端IP: {clientIp}\n" +
                   $"📝 规则描述: {rule.Description}\n" +
                   $"📄 日志内容预览:\n{preview}";
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 