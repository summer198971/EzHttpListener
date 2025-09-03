# EzHttpListener

一个高性能的 .NET 8.0 HTTP 数据接收服务，专门用于接收、存储和转发游戏日志和快照数据。

> 🔗 **配套项目**: 本服务与 [EZLogger](https://github.com/summer198971/EZLogger) Unity日志系统完美配合使用，为Unity游戏提供完整的日志收集和监控解决方案。

## 项目功能

### 🎮 Unity游戏日志收集方案

本项目作为 **EZLogger** 的服务端组件，为Unity游戏提供专业的日志收集服务：

- **EZLogger客户端** → **HTTP压缩传输** → **EzHttpListener服务端** → **文件存储+警报通知**

### 核心功能
- **游戏快照上传** - 接收EZLogger发送的游戏快照文件（日志、图片、XML、CSV等）
- **错误日志收集** - 自动接收和处理EZLogger压缩的错误日志数据
- **智能日志过滤** - 基于正则表达式的日志过滤和警报系统，避免日志洪水
- **飞书警报通知** - 关键错误自动推送到飞书机器人，实时监控游戏状态
- **日志开关控制** - 远程控制EZLogger的日志记录开启和关闭

### 技术特性
- **高性能异步处理** - 基于 ASP.NET Core 的异步 I/O 架构，处理EZLogger的高并发日志上传
- **自动数据解压** - 智能识别和解压 EZLogger 发送的 GZip 压缩数据
- **文件自动分割** - 日志文件按大小自动分割，防止单文件过大
- **热重载配置** - 日志过滤配置文件修改后自动重新加载，无需重启服务
- **多架构支持** - 支持 AMD64 和 ARM64 架构部署，适配各种服务器环境

### API 接口

| 接口 | 方法 | 功能说明 | EZLogger对应功能 |
|------|------|----------|------------------|
| `/snapshoot` | POST | 上传游戏快照数据 | EZLogger文件输出器上传 |
| `/errorpos` | POST | 上传通用错误日志 | EZLogger错误上报功能 |
| `/fighterror` | POST/GET | 上传/获取战斗错误日志 | EZLogger系统监控捕获 |
| `/openlog` | GET | 查询日志开关状态 | 配合EZLogger动态级别控制 |
| `/closelog` | GET | 关闭日志记录 | 远程控制EZLogger.DisableAll() |
| `/doopenlog` | GET | 开启日志记录 | 远程控制EZLogger.EnableAll() |
| `/config/status` | GET | 获取配置状态和统计 | 服务端过滤配置监控 |
| `/config/reload` | POST | 重新加载配置文件 | 热重载日志过滤规则 |
| `/helper` | GET | 查看API接口文档 | 完整接口说明 |

## 🚀 完整使用方案

### 1. Unity客户端集成 EZLogger

首先在您的Unity项目中集成 [EZLogger](https://github.com/summer198971/EZLogger)：

```csharp
// Unity项目中配置EZLogger
var config = LoggerConfiguration.CreateDefault();
config.ServerOutput.Enabled = true;
config.ServerOutput.ServerUrl = "http://your-server:5116";
EZLoggerManager.Instance.Configuration = config;

// 启用错误上报到EzHttpListener
EZLog.EnableServerReporting(true);
EZLog.OnServerReport(OnErrorReport);

// 在游戏中使用零开销日志
EZLog.Log?.Log("GamePlay", "玩家进入关卡");
EZLog.Error?.Log("Network", "连接服务器失败");
```

### 2. 服务端部署 EzHttpListener

## 快速部署

### 前置要求
- Docker 和 Docker Compose
- Linux 系统（支持 x86_64 和 ARM64）
- 管理员权限

### 一键部署

1. **创建数据存储卷**
```bash
sudo docker volume create share_data
```

2. **运行部署脚本**
```bash
sudo chmod +x deploy.sh
sudo ./deploy.sh
```

3. **验证服务**
```bash
curl -X GET http://localhost:5116/helper
```

### 服务端口
- **5116** - 数据上传端口
- **8012** - 数据下载端口

### 配置文件
编辑 `config.txt` 自定义服务配置：
```
ip:0.0.0.0
upport:5116
downport:8012
dir:/share_data
```

### 运维脚本

项目提供了完整的运维脚本，简化日常操作：

```bash
# 部署服务（支持重新部署、重启等选项）
sudo ./deploy.sh

# 停止服务（支持正常停止、强制停止、清理容器等选项）
sudo ./stop.sh

# 查看服务状态（显示容器状态、资源使用、存储卷等信息）
./status.sh
```

### 数据存储
- **日志文件**: `/share_data/logs/`
- **快照文件**: `/share_data/snapshoot/`
- **配置文件**: `/share_data/config/`

## 🔗 相关项目

- **[EZLogger](https://github.com/summer198971/EZLogger)** - Unity高性能日志系统，本项目的客户端组件
- **完整方案**: EZLogger (Unity客户端) + EzHttpListener (服务端) = 企业级Unity日志收集方案

## 📖 使用说明

服务部署完成后，可通过 `/helper` 接口查看完整的API文档和使用说明。

更多EZLogger的使用方法和配置，请参考 [EZLogger项目文档](https://github.com/summer198971/EZLogger)。
