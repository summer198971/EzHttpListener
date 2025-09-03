#!/bin/bash

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}准备停止 EzHttpListener 服务...${NC}"

# 检查是否以足够权限运行
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}错误: 请使用 sudo 运行此脚本${NC}"
    exit 1
fi

# 检查服务是否在运行
if ! docker-compose ps | grep -q "ezhttplistener.*Up"; then
    echo -e "${YELLOW}服务当前未运行${NC}"
    exit 0
fi

# 提供停止选项
echo -e "请选择停止方式："
echo -e "1) 正常停止服务"
echo -e "2) 强制停止服务"
echo -e "3) 停止并清理容器"
echo -e "4) 取消操作"
read -p "请输入选项 (1-4): " -n 1 -r
echo

case $REPLY in
    1)
        echo -e "${YELLOW}正常停止服务...${NC}"
        docker-compose stop
        ;;
    2)
        echo -e "${YELLOW}强制停止服务...${NC}"
        docker-compose kill
        ;;
    3)
        echo -e "${YELLOW}停止并清理容器...${NC}"
        docker-compose down
        ;;
    *)
        echo -e "${GREEN}操作已取消${NC}"
        exit 0
        ;;
esac

# 等待服务完全停止
echo "等待服务停止..."
max_retries=15
retry_count=0
while docker-compose ps | grep -q "ezhttplistener.*Up" && [ $retry_count -lt $max_retries ]; do
    echo "等待服务停止... (${retry_count}/${max_retries})"
    sleep 1
    retry_count=$((retry_count + 1))
done

# 检查服务是否已经停止
if ! docker-compose ps | grep -q "ezhttplistener.*Up"; then
    echo -e "${GREEN}服务已成功停止${NC}"
    
    # 显示当前 Docker 状态
    echo -e "\n${GREEN}=== 当前服务状态 ===${NC}"
    docker-compose ps
    
    # 显示帮助信息
    echo -e "\n${GREEN}=== 常用命令 ===${NC}"
    echo "启动服务: ./deploy.sh"
    echo "查看状态: docker-compose ps"
    echo "查看日志: docker-compose logs"
else
    echo -e "${RED}警告: 服务可能未完全停止${NC}"
    echo -e "当前服务状态:"
    docker-compose ps
    exit 1
fi 