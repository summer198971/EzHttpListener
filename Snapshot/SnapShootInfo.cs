using System.Text.Json.Serialization;
using EzHttpListener;

namespace DataReceiverService.Infos
{
    public class SnapShootInfo
    {
        [JsonPropertyName("uid")] public string uid { get; set; }

        [JsonPropertyName("channel")] public string channel { get; set; }
        [JsonPropertyName("filename")] public string filename { get; set; }

        [JsonPropertyName("ip")] public string ip { get; set; }

        [JsonPropertyName("device")] public string device { get; set; }

        [JsonPropertyName("os")] public string os { get; set; }

        [JsonPropertyName("timestamp")] public long timestamp { get; set; }

        [JsonPropertyName("content")] public string content { get; set; }
        
        [JsonPropertyName("todingtalk")] public bool todingtalk { get; set; }


        [JsonPropertyName("fileType")] public SnapShootFileType fileType { get; set; }
        [JsonPropertyName("extraInfo")] public string extraInfo { get; set; }
        [JsonPropertyName("saveFileName")] public string saveFileName { get; set; }
        
        JsonBuilder extraInfoBuilder = null;

        //FeiShuAT
        public string GetExtraValue(string key)
        {
            Console.WriteLine("GetExtraValue:"+key);
            if(extraInfoBuilder == null)
            {
                extraInfoBuilder = new JsonBuilder(extraInfo);
            }
            return extraInfoBuilder.GetValue(key);
        }
        public string GetExtraInfo()
        {
            if(extraInfoBuilder == null)
            {
                extraInfoBuilder = new JsonBuilder(extraInfo);
            }
            return extraInfoBuilder.ToString();
        }

        public string GetDictionaryName()
        {
            return "IP_" + ip + "_UID_" + uid;
        }

        public string GetSaveFileName()
        {
            if(!string.IsNullOrEmpty(saveFileName))
            {
                return saveFileName;
            }
            // string fileName = $"tm_{TimeHelper.FromUnixTimeStampToUtc8(timestamp):MMdd_HH_mm_ss}_{filename}";
            string fileName = $"tm_{TimeHelper.FromUnixTimeStampToUtc(timestamp):MMddHHmmss}_{filename}";
            return fileName;
        }

        public string GetDownloadUrl()
        {
            return $"{GlobalConfig.Ip}:{GlobalConfig.DownPort}/cdn{GlobalConfig.DownPort}/snapshoot/{GetDictionaryName()}/{GetSaveFileName()}";
        }


        public string extension() => fileType switch
        {
            SnapShootFileType.Text => ".txt",
            SnapShootFileType.Image => ".png",
            SnapShootFileType.Xml => ".xml",
            SnapShootFileType.bytes => ".zip",
            SnapShootFileType.csv => ".csv",
            _ => $".{fileType}"
        };
        
    }
}