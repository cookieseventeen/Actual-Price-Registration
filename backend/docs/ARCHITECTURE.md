# 實價通 後端架構書（Backend Architecture）

> 對應前端：`實價通 — 台中市實價登錄查詢服務`（React + Vite，部署於 GitHub Pages）
> 本文件先定義「**要做什麼、做多少、怎麼分環境跑起來**」，再進入實作。

**文件導覽**
- 本檔：總架構、技術棧、API、環境策略、工作分解（Phase 0–7）
- [`docker-compose.md`](docker-compose.md) — 容器編排整體規劃（profile / 持久化 / 指令）
- [`functional-architecture.md`](functional-architecture.md) — 功能架構 + 後端業務邏輯架構
- [`schema.dbml`](schema.dbml) — 資料庫 schema（DBML，可貼 dbdiagram.io 出圖）

---

## 1. 文件目的

前端目前是純前端 demo，資料來自 `src/data/*.ts` 的 mock。本架構書定義一個可逐步替換 mock 的**真實後端**，並明確：

- 領域模型與 API 介面（對應前端既有型別）
- 技術棧與專案分層
- **開發環境 / 正式環境的分離策略**（本機 dev 可直接串接 compose 起的 DB/Redis）
- 容器編排（PostgreSQL / Redis / API / Nginx / Cloudflare Tunnel）與**重啟、重建後資料不遺失**的保證
- 實作藍圖與工作分解（讓你掌握總工作量）

---

## 2. 系統總覽

```
                          ┌──────────────────────────── Internet ───────────────────────────┐
                          │                                                                  │
   使用者 / 前端(GH Pages) │  https://api.<your-domain>                                        │
          │               │            │                                                     │
          ▼               ▼            ▼                                                      │
   ┌─────────────┐   ┌─────────────────────────┐  Cloudflare Edge                            │
   │  瀏覽器 SPA  │──▶│   Cloudflare Tunnel      │  (DNS + TLS + WAF，無需開 Port / 公網 IP)   │
   └─────────────┘   └────────────┬────────────┘                                             │
                                  │ outbound-only 連線                                        │
   ════════════════════ Docker 內部網路 shijiatong-net ═══════════════════════════════════════
                                  │
                          ┌───────▼────────┐
                          │  cloudflared   │  (tunnel client 容器)
                          └───────┬────────┘
                                  │ http://nginx:80
                          ┌───────▼────────┐
                          │     nginx      │  反向代理 / gzip / 靜態快取 / 限流
                          └───────┬────────┘
                                  │ http://api:8080
                          ┌───────▼────────┐
                          │  api (C#/.NET) │  ASP.NET Core 8 Minimal API + EF Core
                          └───┬────────┬───┘
                  ┌───────────┘        └───────────┐
          ┌───────▼────────┐            ┌──────────▼─────────┐
          │  PostgreSQL 16 │            │     Redis 7        │
          │  (named volume)│            │  (named volume)    │
          └────────────────┘            └────────────────────┘
```

**開發時**：只在 Docker 起 `postgres` + `redis`，把 port 對外開到本機；API 直接用 `dotnet run`（或 IDE）在本機跑、串接 `localhost:5432 / localhost:6379`。Nginx 與 Cloudflared 不啟動。

---

## 3. 技術棧

| 層 | 技術 | 說明 |
|---|---|---|
| Web 框架 | **ASP.NET Core 10 (LTS)**，Minimal API | 輕量、容器友善，預設監聽 8080 |
| ORM | **EF Core 10 + Npgsql** | Code-First Migration，啟動時自動套用 |
| 資料庫 | **PostgreSQL 16** | 主資料儲存，named volume 持久化 |
| 快取 | **Redis 7** | `IDistributedCache`：熱門查詢快取、（未來）Session/Token、爬蟲佇列 |
| Web 伺服器 | **Nginx 1.27** | 反向代理、gzip、靜態快取、限流、單一對外入口 |
| 對外通道 | **Cloudflare Tunnel (cloudflared)** | 免公網 IP / 免開 Port，邊緣 TLS 與 DNS |
| 容器編排 | **Docker Compose v2** | 多檔分層（base / dev override / prod） |
| API 文件 | **Swagger / OpenAPI**（Swashbuckle） | 僅開發環境啟用 |

選型理由：與你指定的 C# / PostgreSQL / Redis / EF Core / Nginx 一致；.NET 8 為 LTS、容器基底映像穩定；Cloudflare Tunnel 讓本機或自架機器無須暴露 Port 即可對外。

---

## 4. 領域模型（對應前端型別）

來源前端檔：`src/data/mock.ts`、`src/data/members.ts`、`src/lib/query.ts`。

### 4.1 Entity 一覽

| Entity | 資料表 | 對應前端 | 主鍵 |
|---|---|---|---|
| `District` | `districts` | `District` | `Id` (string, e.g. `xitun`) |
| `Transaction` | `transactions` | `Transaction` | `Id` (string, e.g. `T0001`) |
| `Member` | `members` | `Member` | `Id` (Guid) |
| `CrawlTask` | `crawl_tasks` | `CrawlTask` | `Id` (string, e.g. `C01`) |

### 4.2 關聯與列舉

```
District 1 ──< Transaction      (Transaction.DistrictId -> District.Id)

enum Provider     { Google, Apple }          // 以字串存 DB
enum Plan         { Free, Pro, Enterprise }
enum MemberStatus { Pending, Active, Rejected, Suspended }
enum CrawlStatus  { Done, Running, Queued, Error }
```

### 4.3 欄位重點

- `Transaction`：總價 `Total`(萬)、單價 `Unit`(萬/坪)、坪數 `Ping`、`Date`(成交日)、`Lat/Lng`(地圖)、`CrawledAt`(UTC)。
- `Member`：SSO `Provider`、方案 `Plan`、審核狀態 `Status`、`Purpose`(用途)、`CreatedAt/ReviewedAt`、`Note`。
- 時間一律以 **UTC (timestamptz)** 儲存；顯示格式由前端決定。

> 狀態：domain/資料層（`Domain/Entities/*`、`Infrastructure/AppDbContext.cs`、`DbSeeder.cs`）**已建立**，種子資料完整移植自前端 mock。

---

## 5. API 介面設計

統一前綴 `/api`，回傳 JSON（camelCase）。對外經 Nginx，再經 Cloudflare。

### 5.1 查詢（對應 SearchView / ResultsView / DetailView / AnalysisView / MapView）

| Method | Path | 說明 | 對應前端邏輯 |
|---|---|---|---|
| GET | `/api/districts` | 行政區統計列表（Redis 快取） | `TC_DISTRICTS` |
| GET | `/api/transactions` | 成交查詢，支援 `district,q,type,layout,trade,sort,dir,page,pageSize` | `filterTransactions` / `sortTransactions` |
| GET | `/api/transactions/{id}` | 單筆物件詳情 | `DetailView` |
| GET | `/api/analysis/price-distribution` | 單價區間分布（由 DB 計算） | `PRICE_DISTRIBUTION` |
| GET | `/api/analysis/city-trend` | 全市月趨勢 | `CITY_TREND` |
| GET | `/api/crawl-tasks` | 爬蟲任務狀態 | `CRAWL_TASKS` |

### 5.2 認證 / 會員（對應 AuthView / AdminView）

| Method | Path | 說明 |
|---|---|---|
| POST | `/api/auth/sso/{provider}` | 模擬 SSO 同意，回傳第三方 profile（未來換真 OAuth） |
| POST | `/api/auth/register` | 新會員註冊（狀態 `pending`） |
| GET | `/api/auth/me` | 取得目前登入者 |
| GET | `/api/members` | 後台：會員列表 |
| PATCH | `/api/members/{id}/status` | 後台：核准 / 拒絕 / 停權 / 恢復 |
| PATCH | `/api/members/{id}/plan` | 後台：變更方案 |

### 5.3 維運

| Method | Path | 說明 |
|---|---|---|
| GET | `/health` | Liveness（永遠 200） |
| GET | `/health/ready` | Readiness（檢查 DB 可連線 + Redis ping） |
| GET | `/swagger` | OpenAPI（僅 Development） |

---

## 6. 專案結構

```
backend/
├── docs/
│   └── ARCHITECTURE.md            ← 本文件
├── .env.example                   ← compose 與 API 設定範本（勿提交真 secret）
├── docker-compose.yml             ← base：postgres + redis（dev/prod 共用）
├── docker-compose.override.yml    ← dev：對外開 DB/Redis port，供本機 dotnet 串接（自動載入）
├── docker-compose.prod.yml        ← prod：api + nginx + cloudflared（含安全強化）
├── nginx/
│   └── conf.d/api.conf            ← 反向代理設定
├── cloudflared/
│   └── README.md                  ← tunnel token 設定說明
└── src/
    └── Shijiatong.Api/
        ├── Shijiatong.Api.csproj
        ├── Program.cs              ← 組裝、自動 migrate+seed、map endpoints
        ├── appsettings.json        ← 預設（本機）
        ├── appsettings.Development.json
        ├── Dockerfile              ← multi-stage（sdk build → aspnet runtime）
        ├── Domain/Entities/        ← District / Transaction / Member / CrawlTask   ✅
        ├── Infrastructure/
        │   ├── AppDbContext.cs      ✅
        │   ├── AppDbContextFactory.cs（design-time）✅
        │   ├── DbSeeder.cs          ✅
        │   └── Migrations/          ← 由 dotnet ef 產生
        ├── Features/                ← 依功能分組的 Minimal API endpoints
        │   ├── Districts/  Transactions/  Analysis/  Members/  Auth/  Health/
        ├── Common/Caching/         ← Redis 快取輔助
        └── Contracts/              ← 對外 DTO
```

分層原則：`Domain`（純模型）← `Infrastructure`（EF/Redis）← `Features`（HTTP 端點）。先單一專案、資料夾分層；規模變大再拆 `.Domain / .Infrastructure / .Api` 多專案。

---

## 7. 環境策略（Dev / Prod 分離）★

核心原則：**同一份程式碼，靠設定切環境**；設定優先序 `環境變數 > appsettings.{Env}.json > appsettings.json`。

### 7.1 兩種跑法

| | 開發環境 (Development) | 正式環境 (Production) |
|---|---|---|
| API 在哪跑 | **本機** `dotnet run` / IDE（可中斷除錯、熱重載） | **容器** `api`（compose build） |
| Postgres / Redis | **容器**，port 對外開（`5432`/`6379`） | **容器**，**不對外開 port**（僅內網） |
| Nginx | 不啟動（前端 Vite `5173` 直連 `localhost:8080`） | 啟動，單一對外入口 |
| Cloudflared | 不啟動 | 啟動，對外通道 |
| Swagger / CORS | 開啟、允許 `localhost:5173` | 關閉 Swagger、CORS 限白名單 |
| 連線字串 | `Host=localhost` | `Host=postgres` / `redis:6379` |

> 重點回應你的需求：**本機開發時仍可直接串接 compose 起的 DB/Redis**——只跑資料服務容器，API 留在本機，省去每次改碼都 rebuild 映像。

### 7.2 compose 檔案分層

採 **Docker Compose profiles + 多檔覆寫**：

- `docker-compose.yml`（base）：定義 `postgres`、`redis`（無 profile，永遠啟動）；`api`、`nginx`、`cloudflared` 標記 `profiles: [prod]`（預設不啟動）。
- `docker-compose.override.yml`（自動載入，dev 用）：把 `postgres`/`redis` 的 port 對外開到本機。
- `docker-compose.prod.yml`（顯式指定，prod 用）：移除 DB/Redis 對外 port、設 `restart: always`、注入正式連線字串與 tunnel token。

**啟動指令**

```bash
# ── 開發：只起 DB + Redis（對外開 port），API 留本機 ──
docker compose up -d                 # 讀 yml + override.yml
cd src/Shijiatong.Api && dotnet run  # 本機 API 串接 localhost:5432/6379

# ── 正式：全套容器（含 nginx + cloudflared）──
docker compose -f docker-compose.yml -f docker-compose.prod.yml --profile prod up -d --build
```

設定來源集中在 `.env`（compose 變數展開）與容器 `environment:`（`ConnectionStrings__Postgres`、`ConnectionStrings__Redis`、`ASPNETCORE_ENVIRONMENT`、`CLOUDFLARE_TUNNEL_TOKEN` 等）。

---

## 8. 容器編排與「重啟 / 重建不壞」保證

| 服務 | 映像 | 持久化 / 健康檢查 | 重啟策略 |
|---|---|---|---|
| postgres | `postgres:16-alpine` | named volume `pgdata`；`pg_isready` healthcheck | `unless-stopped` |
| redis | `redis:7-alpine` | named volume `redisdata`（AOF）；`redis-cli ping` | `unless-stopped` |
| api | 本地 build（multi-stage） | `depends_on` DB/Redis `condition: service_healthy`；`/health/ready` | `unless-stopped`(prod `always`) |
| nginx | `nginx:1.27-alpine` | `depends_on` api | `unless-stopped` |
| cloudflared | `cloudflare/cloudflared:latest` | token 由 env 注入 | `unless-stopped` |

**重啟 / 重建後仍正常的關鍵設計**

1. **資料存在 named volume**：`docker compose restart` / `down` / `up` / 重 build 映像都不動 volume → 資料保留。（只有 `down -v` 會清，文件會明確警告。）
2. **啟動順序靠 healthcheck**：API 等 DB/Redis `service_healthy` 才啟動，避免重建後競態。
3. **Migration 冪等**：API 啟動時 `Database.Migrate()`，已套用的版本自動略過；外加重試迴圈（DB 尚未就緒時退避重試）。
4. **Seed 冪等**：`DbSeeder` 僅在資料表為空時寫入，重啟不重覆灌、不報錯。
5. **無狀態 API**：API 容器本身不存任何本地狀態，砍掉重建無副作用。

驗收（建好後實測）：
```bash
docker compose ... up -d            # 起服務、灌種子
curl localhost:8080/api/districts   # 有資料
docker compose ... restart          # 重啟
docker compose ... up -d --build    # 重建映像
curl localhost:8080/api/districts   # 仍有相同資料 → 通過
```

---

## 9. Cloudflare Tunnel

讓 API 在**沒有公網 IP、不開任何 Port** 的情況下對外（適合本機 / 家用網路 / 內網主機）。

- 容器 `cloudflared` 以 **token 模式**啟動：`cloudflared tunnel --no-autoupdate run --token $CLOUDFLARE_TUNNEL_TOKEN`。
- 流量路徑：`Cloudflare Edge → cloudflared → http://nginx:80 → api:8080`。
- 設定步驟（寫在 `cloudflared/README.md`）：
  1. Cloudflare Zero Trust → Networks → Tunnels → 建立 tunnel，取得 **Tunnel Token**。
  2. Public Hostname：`api.<your-domain>` → Service `http://nginx:80`。
  3. 把 token 放進 `.env` 的 `CLOUDFLARE_TUNNEL_TOKEN`（**勿提交**）。
  4. `--profile prod up -d` 即生效；DNS 由 Cloudflare 自動建立。
- 安全：tunnel 為 outbound-only，主機防火牆可全關入站；正式環境 DB/Redis/Nginx 皆不對公網開 Port。

---

## 10. 設定與機密管理

- `.env.example` 提交範本；真正的 `.env` 加入 `.gitignore`。
- 機密項目：`POSTGRES_PASSWORD`、`CLOUDFLARE_TUNNEL_TOKEN`、（未來）`JWT_SIGNING_KEY`、OAuth client secret。
- 連線字串以雙底線環境變數覆寫（`ConnectionStrings__Postgres`）。

---

## 11. 實作藍圖與工作分解（總工作量）

> 圖例：✅ 已完成　🔲 待辦。各階段可獨立交付、逐步替換前端 mock。

### Phase 0 — 基礎建設（Infra）
- 🔲 `Dockerfile`（multi-stage）
- 🔲 `docker-compose.yml` / `override`（dev）/ `prod`
- 🔲 `.env.example`、`.gitignore`、`.dockerignore`
- 🔲 Nginx 反向代理設定
- 🔲 啟動自動 `Migrate + Seed`（含重試）
- 🔲 `/health`、`/health/ready`
- 🔲 **驗收：重啟 / 重建資料不遺失**

### Phase 1 — 領域層 / 資料層
- ✅ Entities（District / Transaction / Member / CrawlTask）
- ✅ `AppDbContext` + 設計時 factory
- ✅ `DbSeeder`（移植前端 mock）
- 🔲 產生 `InitialCreate` migration（透過 docker 內 dotnet-ef）

### Phase 2 — 查詢 API
- 🔲 `GET /api/districts`（+ Redis 快取）
- 🔲 `GET /api/transactions`（篩選 / 排序 / 分頁）
- 🔲 `GET /api/transactions/{id}`
- 🔲 `GET /api/analysis/*`、`GET /api/crawl-tasks`
- 🔲 DTO + 前端 `src/lib/*` 串接

### Phase 3 — 認證 / 會員
- 🔲 SSO 模擬端點 → 之後換真 OAuth（Google / Apple）
- 🔲 註冊 / 取得目前使用者
- 🔲 後台會員列表 / 審核 / 變更方案
- 🔲 JWT 與授權（admin 角色保護後台端點）

### Phase 4 — 快取與效能
- 🔲 Redis 快取熱門查詢、輸出快取、快取失效策略

### Phase 5 — 對外
- 🔲 Cloudflare Tunnel（cloudflared 容器 + hostname）
- 🔲 CORS 白名單（含 GitHub Pages origin）、Nginx 限流 / gzip

### Phase 6 — 爬蟲任務（對應 CrawlTask）
- 🔲 背景服務 / 排程（`BackgroundService`），任務狀態寫回 DB（先以模擬實作）

### Phase 7 — 品質與交付
- 🔲 單元 / 整合測試（Testcontainers）
- 🔲 GitHub Actions CI（build + test + 映像）
- 🔲 結構化日誌 / 基本觀測

**建議交付順序**：Phase 0 → 1（先讓整套 compose 在 dev / prod 都能跑起來、資料持久），再依需求推進 2 → 3。

---

## 12. 已確認決策（2026-06）

1. **.NET 版本**：**.NET 10 (LTS)**。
2. **認證**：採**真 OAuth + JWT**（Google / Apple），於 **Phase 3** 實作；目前後台端點未受保護，待 Phase 3 補上 admin 角色保護。
3. **Cloudflare Tunnel**：cloudflared 服務已配好（profile `tunnel`），**token 之後填**，填好加 `--profile tunnel` 即生效。
4. **爬蟲**：暫列 Phase 6，後續再實作（先以模擬）。

### 進度
- **Phase 0（基礎建設）= 完成**：compose(base/dev/prod) + Dockerfile + nginx + cloudflared + 自動 migrate/seed + health；已實測 restart / rebuild / down+up 資料皆保留。
- **Phase 1（資料層）= 完成**：Entities + DbContext + Seeder + `InitialCreate` migration。
- **Phase 2（查詢 API）= 大致完成**：districts / transactions / analysis / crawl-tasks / members（讀寫）已可用，待與前端 `src/lib/*` 串接。
- 下一步：**Phase 3 認證（真 OAuth + JWT）** 與前端串接。
