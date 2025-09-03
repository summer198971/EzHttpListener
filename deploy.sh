#!/bin/bash

# 设置错误时退出
set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}开始部署 EzHttpListener 服务...${NC}"

# 检查是否以足够权限运行
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}错误: 请使用 sudo 运行此脚本${NC}"
    exit 1
fi

# 检查 Docker 是否运行
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}错误: Docker 未运行，请先启动 Docker${NC}"
    exit 1
fi

# 检查 docker-compose 是否安装
if ! command -v docker-compose > /dev/null 2>&1; then
    echo -e "${RED}错误: docker-compose 未安装${NC}"
    exit 1
fi

# 检查 share_data 卷是否存在
if ! docker volume inspect share_data > /dev/null 2>&1; then
    echo -e "${RED}错误: share_data 存储卷不存在！${NC}"
    echo -e "${YELLOW}请先确保 share_data 存储卷已正确创建和配置${NC}"
    exit 1
fi

# 检测系统架构并设置构建参数
echo -e "\n${GREEN}=== 系统信息 ===${NC}"
echo "操作系统: $(uname -s)"
echo "内核版本: $(uname -r)"
echo "系统架构: $(uname -m)"
if [ -f /etc/os-release ]; then
    echo "发行版本: $(cat /etc/os-release | grep PRETTY_NAME | cut -d'"' -f2)"
fi

echo -e "\n${GREEN}=== Docker 信息 ===${NC}"
docker version --format 'Docker 版本: {{.Server.Version}}'
docker version --format 'API 版本: {{.Server.APIVersion}}'

echo "检测系统架构..."
ARCH=$(uname -m)
case $ARCH in
    x86_64)
        echo "检测到 x86_64 架构"
        export BUILDPLATFORM="linux/amd64"
        export TARGETPLATFORM="linux/amd64"
        ;;
    aarch64|arm64)
        echo "检测到 ARM64 架构"
        export BUILDPLATFORM="linux/arm64"
        export TARGETPLATFORM="linux/arm64"
        ;;
    *)
        echo -e "${RED}错误: 不支持的系统架构: $ARCH${NC}"
        exit 1
        ;;
esac
echo -e "${GREEN}将使用 $TARGETPLATFORM 进行构建${NC}"

# 检查当前服务状态
if docker-compose ps | grep -q "ezhttplistener.*Up"; then
    echo -e "${YELLOW}检测到服务正在运行${NC}"
    echo -e "请选择操作："
    echo -e "1) 完全重新构建并部署（清理缓存）"
    echo -e "2) 快速重新构建并部署"
    echo -e "3) 仅重启服务"
    echo -e "4) 取消操作"
    read -p "请输入选项 (1-4): " -n 1 -r
    echo
    case $REPLY in
        1)
            echo "停止旧的服务实例..."
            docker-compose down --remove-orphans
            echo "清理 Docker 缓存..."
            docker builder prune -f
            docker system prune -f --filter "until=24h"
            echo "构建新的 Docker 镜像..."
            docker-compose build --no-cache --pull
            if [ $? -ne 0 ]; then
                echo -e "${RED}构建失败${NC}"
                exit 1
            fi
            ;;
        2)
            echo "停止旧的服务实例..."
            docker-compose down
            echo "快速构建新的 Docker 镜像..."
            docker-compose build
            if [ $? -ne 0 ]; then
                echo -e "${RED}构建失败${NC}"
                exit 1
            fi
            ;;
        3)
            echo "重启服务..."
            docker-compose restart
            ;;
        *)
            echo -e "${GREEN}部署已取消${NC}"
            exit 0
    esac
fi

# 创建必要的目录结构
echo "创建必要的目录结构..."
docker run --rm -v share_data:/share_data alpine sh -c '
# 检查并创建 Logs 目录
if [ ! -d "/share_data/logs" ]; then
    echo "创建 Logs 目录..."
    mkdir -p /share_data/logs
else
    echo "Logs 目录已存在"
fi

# 检查并创建 snapshoot 目录
if [ ! -d "/share_data/snapshoot" ]; then
    echo "创建 snapshoot 目录..."
    mkdir -p /share_data/snapshoot
else
    echo "snapshoot 目录已存在"
fi

# 确保权限正确
echo "设置目录权限..."
chmod -R 755 /share_data

# 显示目录结构
echo "当前目录结构:"
ls -R /share_data/
'

# 启动服务
echo "启动服务..."
# 如果服务没有运行，则启动它
if ! docker-compose ps | grep -q "ezhttplistener.*Up"; then
    echo "构建并启动服务..."
    # 添加错误处理
    if ! docker-compose up -d --build; then
        echo -e "${RED}构建失败，查看详细错误信息：${NC}"
        docker-compose logs
        exit 1
    fi
fi

# 等待服务启动
echo "等待服务启动..."
# 增加重试次数和超时检查
max_retries=30
retry_count=0
while ! docker-compose ps | grep -q "ezhttplistener.*Up" && [ $retry_count -lt $max_retries ]; do
    echo "等待服务启动... (${retry_count}/${max_retries})"
    sleep 2
    retry_count=$((retry_count + 1))
done

# 检查服务状态
if docker-compose ps | grep -q "ezhttplistener.*Up"; then
    echo -e "${GREEN}服务已成功启动${NC}"
    
    # 显示容器日志
    echo "显示最近的日志..."
    docker-compose logs --tail=5
    
    # 检查端口监听状态
    echo "检查端口状态..."
    if netstat -an | grep -q "LISTEN.*:5116"; then
        echo -e "${GREEN}端口 5116 正在监听${NC}"
    else
        echo -e "${RED}警告: 端口 5116 未在监听${NC}"
    fi
    
    if netstat -an | grep -q "LISTEN.*:8012"; then
        echo -e "${GREEN}端口 8012 正在监听${NC}"
    else
        echo -e "${RED}警告: 端口 8012 未在监听${NC}"
    fi
    
    # 检查数据目录权限
    echo "检查数据目录权限..."
    docker-compose exec ezhttplistener sh -c 'ls -la /share_data/logs'
    
    # 输出服务信息和帮助
    echo -e "\n${GREEN}=== EzHttpListener 服务信息 ===${NC}"
    echo -e "上传服务地址: http://localhost:5116/snapshoot"
    echo -e "下载服务地址: http://localhost:8012"
    echo -e "日志目录: /share_data/logs"
    echo -e "快照目录: /share_data/snapshoot"
    
    echo -e "\n${GREEN}=== 常用命令 ===${NC}"
    echo "查看日志: docker-compose logs -f"
    echo "重启服务: docker-compose restart"
    echo "停止服务: docker-compose down"
    echo "查看状态: docker-compose ps"
    
    echo -e "\n${GREEN}=== 测试命令 ===${NC}"
    echo "测试上传: curl -X POST http://localhost:5116/snapshoot"
    echo "测试下载: curl http://localhost:8012"
    
    echo -e "\n${GREEN}部署完成!${NC}"
else
    echo -e "${RED}错误: 服务启动失败${NC}"
    echo "查看详细日志："
    docker-compose logs
    exit 1
fi 