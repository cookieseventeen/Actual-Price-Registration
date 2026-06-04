# 實價通 後端

ASP.NET Core 8/10 + EF Core(PostgreSQL) + Redis + Nginx + Cloudflare Tunnel。
架構詳見 [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)。

## 快速開始

```bash
cd backend
cp .env.example .env        # 視需要修改密碼
```

### 開發環境（API 跑本機，DB/Redis 跑容器）

```bash
docker compose up -d                       # 只起 postgres + redis（port 對外開）
cd src/Shijiatong.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run
# API: http://localhost:8080  OpenAPI: http://localhost:8080/openapi/v1.json
```

### 正式環境（全套容器）

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod up -d --build
# 經 nginx 對外：http://localhost:8080
```

接 Cloudflare Tunnel（先在 .env 填 token，見 `cloudflared/README.md`）：

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml \
  --profile prod --profile tunnel up -d --build
```

## 主要 API

| Method | Path |
|---|---|
| GET | `/health` · `/health/ready` |
| GET | `/api/districts` |
| GET | `/api/transactions?district=&q=&type=&layout=&trade=&sort=&dir=&page=&pageSize=` |
| GET | `/api/transactions/{id}` |
| GET | `/api/analysis/price-distribution` · `/api/analysis/city-trend` |
| GET | `/api/crawl-tasks` |
| GET | `/api/members` · PATCH `/api/members/{id}/status` · `/plan` |

## 重啟 / 重建驗證

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod up -d --build
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod restart
# 資料存於 named volume（pgdata / redisdata），restart 與 rebuild 後仍保留。
# 注意：docker compose down -v 會刪除 volume → 清空資料。
```

## 產生 / 更新 EF Migration（本機無 dotnet 時用容器跑）

```bash
docker run --rm -v "$PWD/src/Shijiatong.Api":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc "dotnet tool install -g dotnet-ef && export PATH=\$PATH:/root/.dotnet/tools && \
            dotnet ef migrations add <Name> -o Infrastructure/Migrations"
```
