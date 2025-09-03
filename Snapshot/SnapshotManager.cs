using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Game.Sdk.Report;

namespace DataReceiverService.Infos;

public class SnapshotManager
{
    string path => $"{GlobalConfig.path}{GlobalConfig.SnapshootType}/";
    string folderName;

    public async Task CreateDirectoryAsync(SnapShootInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.uid))
        {
            // throw new ArgumentException("UID cannot be empty or null", nameof(info.uid));
        }

        folderName = path + info.GetDictionaryName();
        if (!Directory.Exists(folderName))
        {
            Directory.CreateDirectory(folderName);
        }

        await Task.CompletedTask; // Placeholder for async compatibility
    }


    public async Task<string> GenerateSnapshotFileAsync(SnapShootInfo info)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        string fileName = info.GetSaveFileName();
        // string extension = info.fileType == SnapShootFileType.Text ? ".txt" : ".png";
        string filePath = Path.Combine(folderName, fileName + info.extension());
        switch (info.fileType)
        {
            // case SnapShootFileType.Text:
            // case SnapShootFileType.Xml:
            // case SnapShootFileType.csv:
            //     string content = Tools.DecompressString(Convert.FromBase64String(info.content));
            //     await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
            //     if(info.todingtalk)
            //     {
            //         // await toDingTalk(info.content);
            //         await Tools.SendFeiShu(content);
            //     }
            //     break;
            case SnapShootFileType.Image:
                byte[] imageBytes = Tools.Decompress(Convert.FromBase64String(info.content));
                await File.WriteAllBytesAsync(filePath, imageBytes);
                break;
            case SnapShootFileType.bytes:
                string DirPath = Path.Combine(folderName, fileName);
                if (!Directory.Exists(DirPath))
                {
                    Directory.CreateDirectory(DirPath);
                }
                byte[] bytes = Tools.Decompress(Convert.FromBase64String(info.content));
                await ParseAllDataZip(bytes,DirPath,filePath);
                break;
            default:
                string content = Tools.DecompressString(Convert.FromBase64String(info.content));
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                if(info.todingtalk)
                {
                    // await toDingTalk(info.content);
                    await Tools.SendFeiShu(content,info.GetExtraValue("FeiShuAT"));
                }
                break;
        }

        return filePath;
    }

    
    public async Task ParseAllDataZip(byte[] allData, string path,string zipName)
    {
        string zipFilePath = zipName;

        using (MemoryStream memoryStream = new MemoryStream(allData))
        {
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                int offset = 0;

                while (offset < allData.Length)
                {
                    int fileNameLength = reader.ReadInt32();
                    byte[] fileNameBytes = reader.ReadBytes(fileNameLength);
                    string fileName = Encoding.UTF8.GetString(fileNameBytes);

                    int fileDataLength = reader.ReadInt32();
                    byte[] fileData = reader.ReadBytes(fileDataLength);

                    string filePath = Path.Combine(path, fileName);
                    await File.WriteAllBytesAsync(filePath, fileData);

                    offset += sizeof(int) + fileNameLength + sizeof(int) + fileDataLength;
                }
            }
        }

        // Create zip file
        ZipFile.CreateFromDirectory(path, zipFilePath);
    }
    public async Task ParseAllData(byte[] allData, string path)
    {
        int offset = 0;

        while (offset < allData.Length)
        {
            using (MemoryStream ms = new MemoryStream(allData, offset, allData.Length - offset))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    int fileNameLength = reader.ReadInt32();
                    byte[] fileNameBytes = reader.ReadBytes(fileNameLength);
                    string fileName = Encoding.UTF8.GetString(fileNameBytes);

                    int fileDataLength = reader.ReadInt32();
                    byte[] fileData = reader.ReadBytes(fileDataLength);
                    string fileContent = Encoding.UTF8.GetString(fileData);
                    string filePath = path + "/" + fileName;
                    await File.WriteAllTextAsync(filePath, fileContent);
                    offset += sizeof(int) + fileNameLength + sizeof(int) + fileDataLength;
                }
            }
        }
    }

    public async Task StoreSnapshotAsync(SnapShootInfo info)
    {
        await CreateDirectoryAsync(info);
        await GenerateSnapshotFileAsync(info);
    }

    /// <summary>
    /// 解析JSON数据到SnapShootInfo，data和FileType
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <returns>包含解析结果的元组</returns>
    public (SnapShootInfo info, SnapShootFileType type) ParseSnapshoot(string json)
    {
        try
        {
            // 解析JSON字符串到字典对象
            var reportData = JsonDocument.Parse(json).RootElement;

            // 解析info
            var infoElement = reportData.GetProperty("info").GetRawText();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var info = JsonSerializer.Deserialize<SnapShootInfo>(infoElement, options);

            // 解析Base64编码的data
            // byte[] data = Convert.FromBase64String(reportData.GetProperty("data").GetString());

            // 解析fileType
            var typeString = reportData.GetProperty("fileType").GetString();
            SnapShootFileType type = Enum.Parse<SnapShootFileType>(typeString, true);

            return (info, type);
        }
        catch (JsonException ex)
        {
            // 处理 JSON 解析错误
            Console.WriteLine($"JSON 解析错误: {ex.Message}");
            throw;
        }
        catch (ArgumentException ex)
        {
            // 处理枚举解析错误
            Console.WriteLine($"枚举解析错误: {ex.Message}");
            throw;
        }
        catch (FormatException ex)
        {
            // 处理 Base64 解析错误
            Console.WriteLine($"Base64 解析错误: {ex.Message}");
            throw;
        }
    }
}