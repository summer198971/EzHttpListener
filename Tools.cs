using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EzHttpListener;

namespace DataReceiverService;

public class Tools
{
    // 解压缩字符串
    public static string DecompressString(byte[] compressedData)
    {
        using (MemoryStream memoryStream = new MemoryStream(compressedData))
        {
            using (GZipStream gzip = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    gzip.CopyTo(decompressedStream);
                    byte[] decompressedBuffer = decompressedStream.ToArray();
                    return Encoding.UTF8.GetString(decompressedBuffer);
                }
            }
        }
    }

    public static byte[] Decompress(byte[] compressedData)
    {
        using (MemoryStream memoryStream = new MemoryStream(compressedData))
        {
            using (GZipStream gzip = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    gzip.CopyTo(decompressedStream);
                    byte[] decompressedBuffer = decompressedStream.ToArray();
                    return decompressedBuffer;
                }
            }
        }
    }

    public static byte[] CompressString(string text)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gzip.Write(buffer, 0, buffer.Length);
            }

            return memoryStream.ToArray();
        }
    }

    public static async Task SendFeiShu(string msg,string at)
    {
        if(msg.IndexOf("isOnline:false") != -1)
        {
            await SendFeiLianMessageDev(msg,at);
        }
        else
        {
            await SendFeiLianMessageOnline(msg,at);
        }
    }
    //https://open.feishu.cn/open-apis/bot/v2/hook/
    public static async Task SendFeiLianMessageOnline(string msg, string at = null)
    {
        string accessToken = "";
        string secret = "";
        string message = "\n";
        string webhookUrl = $"https://open.feishu.cn/open-apis/bot/v2/hook/{accessToken}";

        long timestamp = TimeHelper.GetUtc8UnixTimeSeconds();
        string stringToSign = $"{timestamp}\n{secret}";
        string sign;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            sign = Convert.ToBase64String(hash);
        }
        if(string.IsNullOrEmpty(at))
        {
            at = "";
        }
        string[] _atMobiles = at.Split(',');

        string encodedSign = HttpUtility.UrlEncode(sign);

        using (var client = new HttpClient())
        {
            var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            
            if (_atMobiles != null && _atMobiles.Length > 0)
            {
                var atUsers = new List<Dictionary<string, string>>();
                if (_atMobiles != null)
                {
                    foreach (var mobile in _atMobiles)
                    {
                        if(PersonIds.NameToId.ContainsKey(mobile.ToLower()))   
                        {
                            message+=string.Format(timp,PersonIds.NameToId[mobile.ToLower()],PersonIds.NameToId[mobile.ToLower()])+"\n";
                        }
                    }
                }
                message+=msg;
                var payload = new
                {
                    timestamp = timestamp,
                    sign = encodedSign,
                    msg_type = "text",
                    content = new
                    {
                        text = message
                    }
                };
                
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            }
            else
            {
                // 原来的消息格式
                var payload = new
                {
                    timestamp = timestamp,
                    sign = encodedSign,
                    msg_type = "text",
                    content = new
                    {
                        text = message + msg,
                    },
                };
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            }
            
            HttpResponseMessage response = await client.PostAsync(webhookUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
        }
    }

    static string timp = "<at user_id=\"{0}\">{1}</at>";
    public static async Task SendFeiLianMessageDev(string msg, string at = null)
    {
        string accessToken = "";
        string secret = "";
        string message = "wws \n";
        string webhookUrl = $"https://open.feishu.cn/open-apis/bot/v2/hook/{accessToken}";

        long timestamp = TimeHelper.GetUtc8UnixTimeSeconds();
        string stringToSign = $"{timestamp}\n{secret}";
        string sign;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            sign = Convert.ToBase64String(hash);
        }
        if(string.IsNullOrEmpty(at))
        {
            at = "";
        }
        string[] _atMobiles = at.Split(',');
        string encodedSign = HttpUtility.UrlEncode(sign);

        using (var client = new HttpClient())
        {
            var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            
            if (_atMobiles != null && _atMobiles.Length > 0)
            {
                var atUsers = new List<Dictionary<string, string>>();
                if (_atMobiles != null)
                {
                    foreach (var mobile in _atMobiles)
                    {
                        if(PersonIds.NameToId.ContainsKey(mobile.ToLower()))   
                        {
                            message+=string.Format(timp,PersonIds.NameToId[mobile.ToLower()],PersonIds.NameToId[mobile.ToLower()])+"\n";
                        }
                    }
                }
                message+=msg;
                var payload = new
                {
                    timestamp = timestamp,
                    sign = encodedSign,
                    msg_type = "text",
                    content = new
                    {
                        text = message
                    }
                };
                
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            }
            else
            {
                // 原来的消息格式
                var payload = new
                {
                    timestamp = timestamp,
                    sign = encodedSign,
                    msg_type = "text",
                    content = new
                    {
                        text = message,
                    },
                };
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            }
            
            HttpResponseMessage response = await client.PostAsync(webhookUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
        }
    }
}