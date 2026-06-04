# 功能架構 & 後端業務邏輯架構

> 維度二：專案有哪些功能模組、每個請求怎麼流動、業務規則長怎樣。

---

## A. 功能架構（前端 ↔ 後端對應）

前端每個畫面對應到一組後端端點，逐步把 mock 換成真 API：

| 前端模組 (src/components) | 功能 | 後端端點 | 資料表 |
|---|---|---|---|
| `SearchView` | 行政區 / 關鍵字 / 篩選條件 | `GET /api/districts`、餵參數給查詢 | districts |
| `ResultsView` | 成交清單（卡片/表格、排序） | `GET /api/transactions` | transactions |
| `DetailView` | 單一物件詳情 | `GET /api/transactions/{id}` | transactions |
| `AnalysisView` | 價格分布 / 全市趨勢 | `GET /api/analysis/*` | transactions（聚合） |
| `MapView` | 縣市 3D 房價地圖 | `GET /api/districts`（+ 交易經緯度） | districts / transactions |
| `DataView` | 資料來源 / 爬蟲狀態 | `GET /api/crawl-tasks` | crawl_tasks |
| `AuthView` | SSO 登入 / 註冊 | `POST /api/auth/*`（Phase 3） | members |
| `AdminView` | 後台會員審核 | `GET /api/members`、`PATCH …/status`、`…/plan` | members |

對應前端純函式：`src/lib/query.ts`（`filterTransactions`/`sortTransactions`）的邏輯已搬到 `GET /api/transactions` 的伺服器端查詢。

---

## B. 後端分層架構

```
            HTTP 請求
               │
        ┌──────▼───────────────────────────────────────┐
        │  Features/  (Minimal API endpoints，依功能分組) │  ← 對外介面、參數解析、回傳 DTO
        │  Districts / Transactions / Analysis /         │
        │  CrawlTasks / Members / Health                 │
        └──────┬───────────────────────────┬────────────┘
               │                           │
        ┌──────▼─────────┐         ┌────────▼─────────┐
        │ Infrastructure │         │ Common/Caching   │  ← Redis get-or-set
        │  AppDbContext  │         │ (IDistributedCache)│
        │  DbSeeder      │         └────────┬─────────┘
        └──────┬─────────┘                  │
               │                            │
        ┌──────▼─────────┐         ┌────────▼─────────┐
        │  PostgreSQL    │         │     Redis        │
        └────────────────┘         └──────────────────┘

        Domain/Entities ── 純模型（District/Transaction/Member/CrawlTask），無框架相依
        Contracts ──────── 對外 DTO（DistrictDto/TransactionDto/…），與 Entity 解耦
```

**依賴方向**：`Features → Infrastructure/Common → Domain`。Domain 不依賴任何外層。
**現況**：單一專案、資料夾分層（規模變大再拆 `.Domain / .Infrastructure / .Api` 多專案）。

### 各層職責

| 層 | 資料夾 | 職責 | 不該做 |
|---|---|---|---|
| 介面層 | `Features/*` | 路由、參數驗證、呼叫資料/快取、組 DTO | 不寫複雜領域規則 |
| 契約層 | `Contracts` | 對外 JSON 形狀、Entity→DTO 映射 | 不含邏輯 |
| 基礎設施 | `Infrastructure` | EF DbContext、Migration、Seeder | 不處理 HTTP |
| 快取 | `Common/Caching` | Redis JSON get-or-set | 不知道業務語意 |
| 領域 | `Domain/Entities` | 實體與列舉 | 不依賴 EF/HTTP |

---

## C. 請求生命週期（以「查詢成交」為例）

```
GET /api/transactions?district=西屯區&sort=total&dir=desc&page=1&pageSize=20
   │
   ▼ TransactionEndpoints
   1. 夾住分頁 (page≥1, pageSize 1..200)
   2. 組 IQueryable：district→ Where(名稱或代碼)；q→ 跨欄位 Contains；type/layout/trade→ Where
   3. 排序：total/unit/ping/age/date（預設 date desc）
   4. CountAsync() 取總筆數 → Skip/Take → Select(TransactionDto.From)
   ▼
   PagedResult<TransactionDto> { items, total, page, pageSize }  → JSON
```

行政區清單走快取路徑：

```
GET /api/districts → IDistributedCache.GetOrSetAsync("districts:all", 5min, () => DB 查詢)
   命中 → 直接回 Redis JSON；未命中 → 查 DB、寫快取、回傳
```

---

## D. 業務規則

### D1. 會員狀態機（對應前端審核流程，見 AdminView）

```
            register
   (SSO) ─────────────▶ [pending] ──核准──▶ [active] ──停權──▶ [suspended]
                            │                  ▲                    │
                            └──拒絕──▶[rejected]└────────恢復────────┘
```

- `PATCH /api/members/{id}/status`：合法值 `pending|active|rejected|suspended`，每次變更寫 `ReviewedAt = now(UTC)`。
- `PATCH /api/members/{id}/plan`：`free|pro|enterprise`。
- 新註冊一律 `pending`（Phase 3 由 `/api/auth/register` 建立）。
- TODO(Phase 3)：以 JWT + admin 角色保護；目前端點未授權。

### D2. 查詢語意（與前端一致）
- `district` 同時比對中文名與代碼（`西屯區` 或 `xitun`）。
- `q` 對 community / road / section / 行政區名做 `Contains`。
- 排序 key 白名單化，未知值 fallback 到 `date desc`，避免任意欄位排序。
- 分頁上限 200，避免一次拉爆。

### D3. 分析聚合
- `price-distribution`：把交易 `Unit`（萬/坪）分桶 `~20 / 20-30 / … / 80+` 即時計數。
- `city-trend`：目前回代表值，未來改為「月度聚合表」由排程產生。

### D4. 資料初始化
- 啟動先 `Migrate()`（建/更新 schema），再 `Seed()`（空表才灌）。
- 種子完整移植自前端 `src/data/mock.ts` 與 `members.ts`，讓前端可平滑切換。

---

## E. 橫切關注點

| 項目 | 現況 | 位置 |
|---|---|---|
| CORS | 白名單（dev 允許 `localhost:5173`，可設定） | `Program.cs` + appsettings `Cors:AllowedOrigins` |
| API 文件 | OpenAPI（僅 Development 開 `/openapi/v1.json`） | `AddOpenApi`/`MapOpenApi` |
| 健康檢查 | `/health`（liveness）、`/health/ready`（DB+Redis） | `Features/Health` |
| 設定優先序 | 環境變數 > appsettings.{Env} > appsettings | ASP.NET 預設 |
| 快取失效 | districts 5 分鐘 TTL（尚無主動失效，列為待辦） | `Common/Caching` |

---

## F. 後續擴充（依架構書 Phase）

- **Phase 3 認證**：真 Google/Apple OAuth + JWT、admin 角色保護後台、`/api/auth/*`。
- **Phase 4 快取**：寫操作後主動失效、輸出快取、熱點查詢快取。
- **Phase 6 爬蟲**：`BackgroundService` 排程，任務狀態寫回 `crawl_tasks`（先模擬）。
- **Phase 7 品質**：Testcontainers 整合測試、CI、結構化日誌。
