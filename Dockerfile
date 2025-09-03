FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
ARG TARGETARCH
RUN dotnet restore
RUN if [ "$TARGETARCH" = "arm64" ]; then \
      dotnet publish EzHttpListener.csproj -c Release -r linux-arm64 -p:PublishSingleFile=true --self-contained true -o /app/publish; \
    elif [ "$TARGETARCH" = "amd64" ]; then \
      dotnet publish EzHttpListener.csproj -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true -o /app/publish; \
    fi

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime-deps:8.0
WORKDIR /app
COPY --from=build /app/publish .
COPY config.txt .

# 确保程序有执行权限
RUN chmod +x EzHttpListener

# 添加必要的系统权限
ENV ASPNETCORE_URLS=http://+:5116
ENV COMPlus_EnableDiagnostics=0

# 暴露配置文件中定义的端口
EXPOSE 5116
EXPOSE 8012

ENTRYPOINT ["./EzHttpListener"] 