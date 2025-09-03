using DataReceiverService.Infos;
using Game.Sdk.Report;
using EzHttpListener;
using DataReceiverService.Services;

namespace DataReceiverService
{
    class Program
    {
        static string filename = $"log_{TimeHelper.GetUtc8Time().ToString("yyyyMMdd")}.txt";
        public static LogWriter logWriter;

        private static string today = "";


        public static LogWriter GetlogWriter()
        {
            string _s = TimeHelper.GetUtc8Time().ToString("yyyyMMdd");
            if (string.IsNullOrEmpty(today) || today != _s)
            {
                if (logWriter != null)
                {
                    logWriter.Release();
                    logWriter = null;
                }

                today = _s;
                filename = $"log_{today}.txt";
                // FileMode.Append确保如果文件存在则追加，LogWriter内部会处理文件分割
                logWriter = new LogWriter("log", filename, FileMode.Append);
            }

            return logWriter;
        }


        static void Main(string[] args)
        {
            GlobalConfig.LoadConfig("./config.txt"); // Ensure config is loaded

            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddSingleton<LogFilterManager>(new LogFilterManager(GlobalConfig.path + "/config/log_filter_config.json"));
            builder.Services.AddSingleton<SnapshotManager>();

            var app = builder.Build();

            app.UseRouting();
            app.MapControllers();

            app.Run();
        }
    }
}