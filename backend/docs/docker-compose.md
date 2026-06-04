# Docker Compose 整體規劃

> 維度一：容器編排怎麼組、dev / prod 怎麼切、重啟重建為何不壞。
> 檔案：`docker-compose.yml`（base）、`docker-compose.override.yml`（dev）、`docker-compose.prod.yml`（prod）。

---

## 1. 服務清單

| 服務 | 映像 | 角色 | 對外 | profile |
|---|---|---|---|---|
| `postgres` | `postgres:16-alpine` | 主資料庫 | dev 開 `5433`，prod 不開 | 無（永遠啟動） |
| `redis` | `redis:7-alpine` | 快取 / ping | dev 開 `6380`，prod 不開 | 無（永遠啟動） |
| `api` | 本地 build（.NET 10） | 業務 API | 不直接對外（內網 8080） | `prod` |
| `nginx` | `nginx:1.27-alpine` | 反向代理 / gzip | prod 開 `8090` | `prod` |
| `cloudflared` | `cloudflare/cloudflared` | 對外通道 | outbound-only | `tunnel` |

`api/nginx/cloudflared` 都掛 profile → 平時（dev）不啟動，把開發環境壓到最小。

---

## 2. Profile 矩陣（哪個指令起哪些服務）

| 指令 | postgres | redis | api | nginx | cloudflared |
|---|:---:|:---:|:---:|:---:|:---:|
| `docker compose up -d`（dev） | ✅ | ✅ | – | – | – |
| `… -f prod --profile prod up`（prod） | ✅ | ✅ | ✅ | ✅ | – |
| `… --profile prod --profile tunnel up`（對外） | ✅ | ✅ | ✅ | ✅ | ✅ |

> 規則：服務只要列在「**任一個被啟用的 profile**」就會起；無 profile 者永遠起。
> `--profile tunnel` 需先在 `.env` 填 `CLOUDFLARE_TUNNEL_TOKEN`。

---

## 3. 多檔覆寫策略

```
docker-compose.yml            base：四個服務的完整定義 + profile 標記
        │
        ├── docker-compose.override.yml   ← 預設「自動」載入（dev）
        │       └ 只做一件事：把 postgres/redis 的 port 對外開
        │
        └── docker-compose.prod.yml       ← 必須「顯式 -f」載入（prod）
                └ restart: always、nginx 對外發佈 8090、DB/Redis 不開 port
```

關鍵：**一旦用 `-f` 顯式指定檔案，compose 就不再自動套用 `override.yml`** → 所以 prod 不會誤帶 dev 的對外 port。

---

## 4. 依賴順序與健康檢查

```
postgres ─(healthcheck: pg_isready)─┐
                                    ├─▶ api ─(healthcheck: curl /health)─▶ nginx ─▶ cloudflared
redis ───(healthcheck: redis ping)─┘     condition: service_healthy        depends_on
```

- `api.depends_on` 用 `condition: service_healthy` → DB/Redis 真正就緒才啟動 API。
- `nginx.depends_on api: service_healthy` → API 通過 `/health` 才放流量，避免重建後的競態。
- 雙保險：API 內部 `MigrateAndSeed` 還有退避重試（DB 慢啟動也不崩）。

---

## 5. Port 對照

| 用途 | 容器內 | dev host | prod host | 可調 env |
|---|---|---|---|---|
| API | 8080 | （本機 dotnet run 直接 8080） | 不對外 | — |
| Nginx | 80 | – | `8090` | `NGINX_HTTP_PORT` |
| PostgreSQL | 5432 | `5433` | 不對外 | `POSTGRES_PORT` |
| Redis | 6379 | `6380` | 不對外 | `REDIS_PORT` |

> host port 刻意避開常見的 5432/6379/8080（本機另有 orenote stack 佔用）。

---

## 6. 設定流（config 來源與優先序）

```
.env ──(compose 變數展開)──▶ services.*.environment ──▶ 容器內環境變數
                                                          │  ConnectionStrings__Postgres
                                                          │  ConnectionStrings__Redis
                                                          ▼
   ASP.NET 設定優先序：環境變數 > appsettings.{Environment}.json > appsettings.json
```

- 機密只放 `.env`（已 gitignore）；`.env.example` 為範本。
- dev 本機跑 API 時讀 `appsettings.Development.json`（已指向 `localhost:5433 / 6380`）。

---

## 7. 持久化與「重啟 / 重建不壞」

| 機制 | 作用 |
|---|---|
| named volume `pgdata` / `redisdata` | 資料與容器生命週期解耦；`restart`/`down`/`up`/`--build` 都不動 |
| `Database.Migrate()` 冪等 | 已套用版本自動略過，重啟不重跑 |
| `DbSeeder` 冪等（空表才寫） | 重啟不覆蓋既有資料、不報錯 |
| API 無狀態 | 容器砍掉重建無副作用 |

**已實測通過**：`restart`、`up -d --build`、`down`+`up` 後，先前對資料的修改皆保留。
⚠️ 唯一會清資料的是 `down -v`（刪 volume）。

---

## 8. 指令速查

```bash
# dev：只起資料服務
docker compose up -d
docker compose logs -f postgres redis

# prod：全套
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod up -d --build
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod ps
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod logs -f api

# 對外（填好 token 後）
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod --profile tunnel up -d

# 收掉（保留資料）
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod down
# 收掉並清空資料
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod down -v
```
