using System.Text;
using DataReceiverService.Infos;
using EzHttpListener;

namespace Game.Sdk.Report
{
    public enum LogType
    {
        Log,
        Warning,
        Error,
        Exception
    }

    public class LogWriter : IDisposable
    {
        // public static string defaultLogPath = Application.persistentDataPath + "/";



        private FileStream m_fs;
        private static readonly object m_locker = new object(); // 锁，用于同步访问共享资源
        private string m_logFileName;
        private string m_logFilePath;
        private string m_logPath;
        private StreamWriter m_sw;
        private StreamWriter m_sw_online;
        public string error;
        private Queue<string> logQueue = new Queue<string>(); // 日志队列，存储待写入的日志
        private bool isWriting = false; // 标志位，用于控制写日志线程的运行
        private Thread logWriterThread;
        
        // 文件分割相关字段
        private int m_currentFileIndex = 0; // 当前文件索引
        private string m_baseFileName; // 基础文件名（不包含索引）
        private readonly long m_maxFileSize; // 最大文件大小

        // 使用线程局部存储来复用 StringBuilder
        private static ThreadLocal<StringBuilder> threadLocalStringBuilder =
            new ThreadLocal<StringBuilder>(() => new StringBuilder());

        /// <summary>
        /// 更新文件名和路径
        /// </summary>
        private void UpdateFileNameAndPath()
        {
            if (m_currentFileIndex == 0)
            {
                m_logFileName = m_baseFileName;
            }
            else
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(m_baseFileName);
                var extension = Path.GetExtension(m_baseFileName);
                m_logFileName = $"{nameWithoutExt}_{m_currentFileIndex}{extension}";
            }
            m_logFilePath = Path.Combine(m_logPath, m_logFileName);
        }

        /// <summary>
        /// 检查文件大小并在需要时创建新文件
        /// </summary>
        private void CheckAndRotateFileIfNeeded()
        {
            try
            {
                // 检查当前文件大小
                if (m_fs != null && m_fs.Length >= m_maxFileSize)
                {
                    // 关闭当前文件
                    m_sw?.Close();
                    m_sw?.Dispose();
                    m_fs?.Close();
                    m_fs?.Dispose();

                    // 增加文件索引
                    m_currentFileIndex++;
                    
                    // 更新文件名和路径
                    UpdateFileNameAndPath();
                    
                    // 创建新文件
                    m_fs = new FileStream(m_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    m_sw = new StreamWriter(m_fs);
                    
                    Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Log file rotated to: {m_logFileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{TimeHelper.GetUtc8Time():MM-dd HH:mm:ss:fff} Error checking file size: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找当前应该使用的文件索引
        /// </summary>
        private void FindCurrentFileIndex()
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(m_baseFileName);
            var extension = Path.GetExtension(m_baseFileName);
            
            // 查找现有的分割文件
            m_currentFileIndex = 0;
            while (true)
            {
                UpdateFileNameAndPath();
                var fullPath = Path.Combine(m_logPath, m_logFileName);
                
                if (!File.Exists(fullPath))
                {
                    // 如果文件不存在，就使用当前索引
                    break;
                }
                
                // 检查文件大小，如果小于最大限制，则使用当前文件
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length < m_maxFileSize)
                {
                    break;
                }
                
                // 如果文件已满，尝试下一个索引
                m_currentFileIndex++;
            }
        }

        public LogWriter(string directoryName, string fileName, FileMode mode = FileMode.Append)
        {
            if (string.IsNullOrEmpty(directoryName) || string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Directory name and file name can't be null or empty.");
            }

            m_logPath = Path.Combine(GlobalConfig.path+"logs/", directoryName); // 构建日志目录路径
            m_baseFileName = fileName; // 保存基础文件名
            m_maxFileSize = GlobalConfig.MaxLogFileSize; // 获取最大文件大小配置
            
            // 初始化文件名和路径
            UpdateFileNameAndPath();

            if (!Directory.Exists(this.m_logPath))
            {
                Directory.CreateDirectory(this.m_logPath); // 如果日志目录不存在，则创建目录
            }

            // 查找现有的分割文件，确定当前应该使用的文件索引
            FindCurrentFileIndex();

            try
            {
                this.m_fs = new FileStream(this.m_logFilePath, mode, FileAccess.Write, FileShare.ReadWrite); // 打开文件流
                this.m_sw = new StreamWriter(this.m_fs); // 创建StreamWriter以便写入

                WriteLog("start.....", LogType.Log); // 写入初始日志
                error = "success";

                // 启动日志写入线程
                logWriterThread = new Thread(new ThreadStart(ProcessLogQueue))
                {
                    IsBackground = true // 设置为后台线程
                };
                logWriterThread.Start();
            }
            catch (Exception exception)
            {
                // Debug.LogError(exception.Message);
                Console.WriteLine(exception.Message);
                error = exception.Message;
            }
        }

        public void Release()
        {
            lock (m_locker)
            {
                isWriting = false; // 设置标志位，终止写日志线程
            }

            // 等待日志写入线程终止
            logWriterThread?.Join();

            // 锁定队列并同步写入剩余日志
            lock (m_locker)
            {
                lock (logQueue)
                {
                    while (logQueue.Count > 0)
                    {
                        string logMessage = logQueue.Dequeue();
                        try
                        {
                            if (this.m_sw != null)
                            {
                                this.m_sw.WriteLine(logMessage); // 写入日志消息
                                this.m_sw.Flush(); // 刷新缓冲区
                                
                                // 检查文件大小，如果需要则分割文件
                                CheckAndRotateFileIfNeeded();
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception.Message);
                            // Debugger.Log(exception.Message);
                            // Debug.log.LogError(exception.Message);
                        }
                    }
                }

                // 关闭和释放流和文件
                if (this.m_sw != null)
                {
                    this.m_sw.Close();
                    this.m_sw.Dispose();
                }

                if (this.m_fs != null)
                {
                    this.m_fs.Close();
                    this.m_fs.Dispose();
                }
            }
        }

        private void ProcessLogQueue()
        {
            isWriting = true; // 设置标志位，表示写日志线程正在运行
            while (isWriting)
            {
                string logMessage = null;

                lock (logQueue)
                {
                    if (logQueue.Count > 0)
                    {
                        logMessage = logQueue.Dequeue(); // 从队列中取出一条日志
                    }
                }

                if (logMessage != null)
                {
                    lock (m_locker)
                    {
                        try
                        {
                            if (this.m_sw != null)
                            {
                                this.m_sw.WriteLine(logMessage); // 写入日志消息
                                this.m_sw.Flush(); // 刷新缓冲区
                                
                                // 检查文件大小，如果需要则分割文件
                                CheckAndRotateFileIfNeeded();
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // 忽略当 writer 已经被关闭时的异常
                            break;
                        }
                        catch (Exception exception)
                        {
                            // Debug.LogError(exception.Message);
                            Console.WriteLine(exception.Message);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(10); // 若队列为空，则休眠一小段时间，避免忙轮询
                }
            }
        }

        public int Count;

        public void WriteLog(string msg, LogType level)
        {
            // 检查日志消息是否为空
            if (msg == null)
            {
                // Debug.LogWarning("Attempt to log a null message.");
                Console.WriteLine("Attempt to log a null message.");
                return;
            }

            // 获取线程局部的 StringBuilder 实例
            StringBuilder logBuilder = threadLocalStringBuilder.Value;
            logBuilder.Clear(); // 清空 StringBuilder 以便复用

            logBuilder.Append("[@@@]");
            // 日期格式化
            logBuilder.Append(TimeHelper.GetUtc8Time().ToString("MM-dd HH:mm:ss:fff"));
            logBuilder.Append(" [");
            logBuilder.Append(level.ToString());
            logBuilder.Append("]: ");
            logBuilder.Append(msg);

            string newMsg = logBuilder.ToString();
            Count++;
            // 将日志消息加入队列
            lock (logQueue)
            {
                logQueue.Enqueue(newMsg);
            }
        }

        // 属性，用于获取日志路径
        public string LogPath => this.m_logPath;

        /// <summary>
        /// 实现IDisposable接口
        /// </summary>
        public void Dispose()
        {
            Release();
        }

        public string ReadAllLogs()
        {
            // 锁定以确保线程安全的读取操作
            lock (m_locker)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(new FileStream(this.m_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    {
                        return sr.ReadToEnd(); // 读取并返回所有日志内容
                    }
                }
                catch (FileNotFoundException)
                {
                    // Debug.LogError("Log file not found.");  // 处理文件未找到异常
                    Console.WriteLine("Log file not found.");
                    return null;
                }
                catch (Exception exception)
                {
                    // Debug.LogError(exception.Message);
                    Console.WriteLine(exception.Message);

                    return null;
                }
            }
        }
    }
}