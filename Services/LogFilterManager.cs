using System.Text.Json;
using System.Text.RegularExpressions;
using DataReceiverService.Config;
using DataReceiverService.Services;
using EzHttpListener;

namespace DataReceiverService.Services
{
    /// <summary>
    /// 日志过滤管理器 - 负责加载配置、执行过滤逻辑和管理统计信息
    /// </summary>
    public class LogFilterManager
    {
        private LogFilterConfig _config; // 当前配置
        private readonly Dictionary<string, Regex> _compiledRegexes; // 预编译的正则表达式缓存
        private readonly FeishuAlertService _feishuService; // 飞书警报服务
        private FileSystemWatcher _configWatcher; // 配置文件监控器
        private readonly string _configPath; // 配置文件路径
        private readonly object _lockObject = new object(); // 线程安全锁
        
        // 统计信息
        public int FilteredCount { get; private set; } // 已过滤的日志数量
        public int AlertCount { get; private set; } // 已发送的警报数量
        public int ProcessedCount { get; private set; } // 已处理的日志总数
        
        /// <summary>
        /// 构造函数 - 初始化日志过滤管理器
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        public LogFilterManager(string configPath = "/app/config/log_filter_config.json")
        {
            _configPath = configPath;
            _compiledRegexes = new Dictionary<string, Regex>();
            _feishuService = new FeishuAlertService();
            
            // 确保配置目录存在
            var configDir = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);
                
            // 加载配置并设置文件监控
            LoadConfig();
            SetupFileWatcher();
        }
        
        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    // 从文件读取配置
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<LogFilterConfig>(json) ?? CreateDefaultConfig();
                }
                else
                {
                    // 文件不存在，创建默认配置
                    _config = CreateDefaultConfig();
                    SaveConfig();
                }
                
                // 预编译正则表达式以提高性能
                CompileRegexes();
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} LogFilter config loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error loading config, using default: {ex.Message}");
                _config = CreateDefaultConfig();
                try
                {
                    CompileRegexes();
                }
                catch (Exception regexEx)
                {
                    Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error compiling default regexes: {regexEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认的日志过滤配置</returns>
        private LogFilterConfig CreateDefaultConfig()
        {
            return new LogFilterConfig
            {
                FilterConfig = new FilterConfig
                {
                    Enabled = true,
                    InvalidLogFilters = new List<FilterRule>
                    {
                        new FilterRule
                        {
                            Id = "debug_filter",
                            Name = "过滤调试信息",
                            Type = "contains",
                            Pattern = "[DEBUG]",
                            CaseSensitive = false,
                            Enabled = true,
                            Description = "过滤调试级别日志"
                        },
                        new FilterRule
                        {
                            Id = "heartbeat_filter",
                            Name = "过滤心跳日志",
                            Type = "regex",
                            Pattern = "^heartbeat.*alive$",
                            CaseSensitive = false,
                            Enabled = false,
                            Description = "过滤心跳检测日志"
                        }
                    }
                },
                AlertConfig = new AlertConfig
                {
                    Enabled = true,
                    FeishuWebhook = "",
                    AlertRules = new List<AlertRule>
                    {
                        new AlertRule
                        {
                            Id = "error_alert",
                            Name = "错误日志警报",
                            Type = "contains",
                            Pattern = "ERROR",
                            CaseSensitive = false,
                            Enabled = true,
                            Description = "检测到错误日志时发送警报",
                            Cooldown = 300
                        },
                        new AlertRule
                        {
                            Id = "crash_alert",
                            Name = "崩溃日志警报",
                            Type = "regex",
                            Pattern = "(crash|exception|fatal)",
                            CaseSensitive = false,
                            Enabled = false,
                            Description = "检测到崩溃相关日志",
                            Cooldown = 60
                        }
                    }
                }
            };
        }
        
        /// <summary>
        /// 预编译正则表达式以提高匹配性能
        /// </summary>
        private void CompileRegexes()
        {
            lock (_lockObject)
            {
                _compiledRegexes.Clear();
                
                // 编译过滤规则中的正则表达式
                foreach (var rule in _config.FilterConfig.InvalidLogFilters.Where(r => r.Enabled && r.Type == "regex"))
                {
                    try
                    {
                        var options = rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                        _compiledRegexes[$"filter_{rule.Id}"] = new Regex(rule.Pattern, options | RegexOptions.Compiled);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error compiling regex for filter {rule.Id}: {ex.Message}");
                    }
                }
                
                // 编译警报规则中的正则表达式
                foreach (var rule in _config.AlertConfig.AlertRules.Where(r => r.Enabled && r.Type == "regex"))
                {
                    try
                    {
                        var options = rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                        _compiledRegexes[$"alert_{rule.Id}"] = new Regex(rule.Pattern, options | RegexOptions.Compiled);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error compiling regex for alert {rule.Id}: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 设置配置文件监控器，实现热重载功能
        /// </summary>
        private void SetupFileWatcher()
        {
            try
            {
                var configDir = Path.GetDirectoryName(_configPath);
                var configFileName = Path.GetFileName(_configPath);
                
                if (string.IsNullOrEmpty(configDir) || string.IsNullOrEmpty(configFileName))
                {
                    Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Invalid config path for file watcher: {_configPath}");
                    return;
                }
                
                // 创建文件系统监控器
                _configWatcher = new FileSystemWatcher(configDir, configFileName);
                _configWatcher.Changed += OnConfigChanged;
                _configWatcher.EnableRaisingEvents = true;
                
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Config file watcher setup for: {_configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error setting up file watcher: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 配置文件变更事件处理器
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">文件系统事件参数</param>
        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            // 延迟加载，避免文件正在写入时读取
            _ = Task.Delay(1000).ContinueWith(_ => 
            {
                try
                {
                    ReloadConfig();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error in OnConfigChanged: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// 手动重新加载配置
        /// </summary>
        public void ReloadConfig()
        {
            try
            {
                LoadConfig();
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Config reloaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error reloading config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理日志内容，执行过滤和警报检查
        /// </summary>
        /// <param name="logContent">日志内容</param>
        /// <param name="clientIp">客户端IP地址</param>
        /// <returns>是否应该记录这条日志</returns>
        public async Task<bool> ProcessLogAsync(string logContent, string clientIp)
        {
            ProcessedCount++;
            
            // 1. 检查是否应该过滤（无效日志）
            if (ShouldFilterLog(logContent))
            {
                FilteredCount++;
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Log filtered, total filtered: {FilteredCount}");
                return false; // 返回false表示不应该记录这条日志
            }
            
            // 2. 检查是否需要发送警报
            await CheckAndSendAlertsAsync(logContent, clientIp);
            
            return true; // 返回true表示应该记录这条日志
        }
        
        /// <summary>
        /// 检查日志是否应该被过滤
        /// </summary>
        /// <param name="logContent">日志内容</param>
        /// <returns>是否应该过滤</returns>
        private bool ShouldFilterLog(string logContent)
        {
            if (!_config.FilterConfig.Enabled)
                return false;
                
            // 遍历所有启用的过滤规则
            foreach (var rule in _config.FilterConfig.InvalidLogFilters.Where(r => r.Enabled))
            {
                if (MatchesRule(logContent, rule, $"filter_{rule.Id}"))
                    return true; // 匹配任一规则即过滤
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查并发送警报通知
        /// </summary>
        /// <param name="logContent">日志内容</param>
        /// <param name="clientIp">客户端IP地址</param>
        private async Task CheckAndSendAlertsAsync(string logContent, string clientIp)
        {
            if (!_config.AlertConfig.Enabled || string.IsNullOrEmpty(_config.AlertConfig.FeishuWebhook))
                return;
                
            // 遍历所有启用的警报规则
            foreach (var rule in _config.AlertConfig.AlertRules.Where(r => r.Enabled))
            {
                if (MatchesRule(logContent, rule, $"alert_{rule.Id}"))
                {
                    AlertCount++;
                    // 异步发送警报，不阻塞主流程
                    _ = Task.Run(async () => await _feishuService.SendAlertAsync(_config.AlertConfig.FeishuWebhook, rule, logContent, clientIp));
                }
            }
        }
        
        /// <summary>
        /// 检查内容是否匹配过滤规则
        /// </summary>
        /// <param name="content">要检查的内容</param>
        /// <param name="rule">过滤规则</param>
        /// <param name="regexKey">正则表达式缓存键</param>
        /// <returns>是否匹配</returns>
        private bool MatchesRule(string content, FilterRule rule, string regexKey)
        {
            try
            {
                if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(rule.Pattern))
                    return false;
                    
                var comparison = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                
                return rule.Type switch
                {
                    "contains" => content.Contains(rule.Pattern, comparison),
                    "startswith" => content.StartsWith(rule.Pattern, comparison),
                    "endswith" => content.EndsWith(rule.Pattern, comparison),
                    "regex" => _compiledRegexes.ContainsKey(regexKey) && _compiledRegexes[regexKey].IsMatch(content),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error matching rule {rule.Id}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查内容是否匹配警报规则
        /// </summary>
        /// <param name="content">要检查的内容</param>
        /// <param name="rule">警报规则</param>
        /// <param name="regexKey">正则表达式缓存键</param>
        /// <returns>是否匹配</returns>
        private bool MatchesRule(string content, AlertRule rule, string regexKey)
        {
            try
            {
                if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(rule.Pattern))
                    return false;
                    
                var comparison = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                
                return rule.Type switch
                {
                    "contains" => content.Contains(rule.Pattern, comparison),
                    "startswith" => content.StartsWith(rule.Pattern, comparison),
                    "endswith" => content.EndsWith(rule.Pattern, comparison),
                    "regex" => _compiledRegexes.ContainsKey(regexKey) && _compiledRegexes[regexKey].IsMatch(content),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error matching rule {rule.Id}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 保存配置到文件
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error saving config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取统计信息的JSON字符串
        /// </summary>
        /// <returns>包含处理统计的JSON字符串</returns>
        public string GetStatistics()
        {
            return $"{{\"processed\":{ProcessedCount},\"filtered\":{FilteredCount},\"alerts\":{AlertCount},\"configLoaded\":\"{_config.LastUpdated:yyyy-MM-dd HH:mm:ss}\"}}";
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _configWatcher?.Dispose();
            _feishuService?.Dispose();
        }
    }
} 