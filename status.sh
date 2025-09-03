#!/bin/bash

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Docker 服务状态 ===${NC}"

# 检查 Docker 是否运行
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}错误: Docker 未运行${NC}"
    exit 1
fi

# 显示所有运行中的容器
echo -e "\n${BLUE}运行中的容器:${NC}"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# 显示所有 Docker Compose 项目
echo -e "\n${BLUE}Docker Compose 项目:${NC}"
for dir in $(find / -name docker-compose.yml 2>/dev/null | grep -v "/tmp/" | grep -v "/var/lib/docker/"); do
    project_dir=$(dirname $dir)
    project_name=$(basename $project_dir)
    echo -e "${YELLOW}项目: $project_name${NC}"
    echo "路径: $project_dir"
    if [ -f "$project_dir/docker-compose.yml" ]; then
        cd $project_dir
        echo "状态:"
        docker-compose ps
        echo "------------------------"
    fi
done

# 显示资源使用情况
echo -e "\n${BLUE}资源使用情况:${NC}"
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}\t{{.BlockIO}}"

# 显示存储卷信息
echo -e "\n${BLUE}存储卷信息:${NC}"
docker volume ls

# 显示网络信息
echo -e "\n${BLUE}网络信息:${NC}"
docker network ls

# 显示系统信息
echo -e "\n${BLUE}系统信息:${NC}"
docker system df

echo -e "\n${GREEN}=== 常用命令 ===${NC}"
echo "查看容器日志: docker logs <容器名>"
echo "进入容器: docker exec -it <容器名> /bin/sh"
echo "查看详细状态: docker inspect <容器名>"
echo "查看端口映射: docker port <容器名>" 