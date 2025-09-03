namespace DataReceiverService.Infos;

public class ErrorConfig
{
    public const string type = "Error";
    public const string ip = "http://0.0.0.0";
    public const string upPort = "5116";
    public const string downPort = "8012";
    public static readonly string downUrl = $"{ip}:{downPort}/{type}/";
    public static readonly string upUrl = $"{ip}:{upPort}/{type}/";
}