# ── build 階段：用 .NET SDK 還原 + 發佈 ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先複製 csproj 還原，善用 layer 快取
COPY ["Shijiatong.Api.csproj", "./"]
RUN dotnet restore "Shijiatong.Api.csproj"

# 再複製其餘原始碼並發佈
COPY . .
RUN dotnet publish "Shijiatong.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── runtime 階段：僅 ASP.NET runtime，映像精簡 ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl 供容器 healthcheck 使用
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Shijiatong.Api.dll"]
