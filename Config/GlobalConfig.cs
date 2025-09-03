namespace DataReceiverService.Infos
{
    public class GlobalConfig
    {
        public const string SnapshootType = "snapshoot";
        public const string ErrorPosType = "errorpos";
        public const string FightErrorType = "fighterror";
        public const string HelperType = "helper";

        public const string OpenLogType = "openlog";
        public const string CloseLogType = "closelog";
        public const string DoOpenLogType = "doopenlog";
        public const string ConfigReloadType = "config/reload"; // 配置重载接口路径
        public const string ConfigStatusType = "config/status"; // 配置状态查询接口路径

        // 日志文件大小限制配置（字节）
        public static long MaxLogFileSize { get; private set; } = 200 * 1024 * 1024; // 默认100MB

        public static string Ip { get; private set; }
        public static string UpPort { get; private set; }
        public static string DownPort { get; private set; }
        public static string DownUrl => $"{Ip}:{DownPort}/{SnapshootType}/";
        public static string UpUrl => $"{Ip}:{UpPort}/{SnapshootType}/";
        
        public static string dir = "";
        public static string path => $"{dir}/";


        static GlobalConfig()
        {
            Ip = "http://+"; // 允许所有接口
            UpPort = "5117"; // Default value
            DownPort = "8012"; // Default value
            dir = "/share_data";
            MaxLogFileSize = 500 * 1024 * 1024;
            LoadConfig("./config.txt");
        }

        public static void LoadConfig(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"The config file was not found: {configFilePath}");
            }

            var configData = File.ReadAllLines(configFilePath);

            foreach (var line in configData)
            {
                var splittedLine = line.Split(':');

                if (splittedLine.Length != 2) continue;

                var id = splittedLine[0].Trim().ToLower();
                var value = splittedLine[1].Trim();
                Console.WriteLine($"id: {id}, value: {value}");

                switch (id)
                {
                    case "ip":
                        Ip = $"http://{value}";
                        break;
                    case "upport":
                        UpPort = value;
                        break;
                    case "downport":
                        DownPort = value;
                        break;
                    case "dir":
                        dir = value;
                        break;
                    case "maxlogfilesize":
                        if (long.TryParse(value, out long maxSize))
                        {
                            MaxLogFileSize = maxSize;
                        }
                        break;
                    default:
                        Console.WriteLine($"Unknown config key: {id}");
                        break;
                }
            }
        }
    }
}