using System.Text.Json.Serialization;
using EzHttpListener;


namespace DataReceiverService.Config
{
    /// <summary>
    /// 日志过滤配置根类
    /// </summary>
    public class LogFilterConfig
    {
        /// <summary>
        /// 最后更新时间
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = TimeHelper.GetUtc8Time();
        
        /// <summary>
        /// 日志过滤配置
        /// </summary>
        [JsonPropertyName("filterConfig")]
        public FilterConfig FilterConfig { get; set; } = new();
        
        /// <summary>
        /// 警报配置
        /// </summary>
        [JsonPropertyName("alertConfig")]
        public AlertConfig AlertConfig { get; set; } = new();
    }

    /// <summary>
    /// 日志过滤配置类
    /// </summary>
    public class FilterConfig
    {
        /// <summary>
        /// 是否启用过滤功能
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// 无效日志过滤规则列表
        /// </summary>
        [JsonPropertyName("invalidLogFilters")]
        public List<FilterRule> InvalidLogFilters { get; set; } = new();
    }

    /// <summary>
    /// 警报配置类
    /// </summary>
    public class AlertConfig
    {
        /// <summary>
        /// 是否启用警报功能
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// 飞书机器人Webhook地址
        /// </summary>
        [JsonPropertyName("feishuWebhook")]
        public string FeishuWebhook { get; set; } = "";
        
        /// <summary>
        /// 警报规则列表
        /// </summary>
        [JsonPropertyName("alertRules")]
        public List<AlertRule> AlertRules { get; set; } = new();
    }

    /// <summary>
    /// 过滤规则类
    /// </summary>
    public class FilterRule
    {
        /// <summary>
        /// 规则唯一标识符
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        
        /// <summary>
        /// 规则显示名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        /// <summary>
        /// 匹配类型：contains(包含), regex(正则), startswith(开头), endswith(结尾)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "contains";
        
        /// <summary>
        /// 匹配模式或正则表达式
        /// </summary>
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = "";
        
        /// <summary>
        /// 是否区分大小写
        /// </summary>
        [JsonPropertyName("caseSensitive")]
        public bool CaseSensitive { get; set; } = false;
        
        /// <summary>
        /// 是否启用此规则
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// 规则描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 警报规则类
    /// </summary>
    public class AlertRule
    {
        /// <summary>
        /// 规则唯一标识符
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        
        /// <summary>
        /// 规则显示名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        /// <summary>
        /// 匹配类型：contains(包含), regex(正则), startswith(开头), endswith(结尾)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "contains";
        
        /// <summary>
        /// 匹配模式或正则表达式
        /// </summary>
        [JsonPropertyName("pattern")]
        public string Pattern { get; set; } = "";
        
        /// <summary>
        /// 是否区分大小写
        /// </summary>
        [JsonPropertyName("caseSensitive")]
        public bool CaseSensitive { get; set; } = false;
        
        /// <summary>
        /// 是否启用此规则
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// 规则描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        
        /// <summary>
        /// 冷却时间（秒）- 防止频繁发送相同警报
        /// </summary>
        [JsonPropertyName("cooldown")]
        public int Cooldown { get; set; } = 300;
    }
} 