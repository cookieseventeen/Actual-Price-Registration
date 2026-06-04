# 後端認證／授權分階段實作計劃

> **文件性質**：實作藍圖（本文件不產生任何 production 程式碼）。  
> **對齊基準**：2026-06-04 之 `backend/src/Shijiatong.Api` 與 `backend/tests/Shijiatong.Api.Tests` 現況。  
> **最高原則**：可開關、預設不改現狀、可逐階段獨立交付與回滾。

---

## 0. 現況摘要（計劃 grounded 依據）

### 0.1 專案與執行環境

| 項目 | 現況 |
|------|------|
| 目標框架 | `net10.0`（`Shijiatong.Api.csproj`） |
| 認證相關 NuGet | **尚無**（僅 EF、Redis、OpenAPI、Serilog） |
| 設定慣例 | `appsettings.json` + `appsettings.Development.json`；compose 以環境變數覆寫（`ConnectionStrings__Postgres` 等，見 `backend/docs/docker-compose.md` §6） |
| 測試 | `WebApplicationFactory<Program>` + Testcontainers Postgres/Redis；`public partial class Program { }` 為測試鉤子 |

### 0.2 `Program.cs` 現有管線（**必須保留**）

```text
builder: AddDbContext → AddRedisCache → AddCors → AddOpenApi → AddProblemDetails
app:     MigrateAndSeedAsync → UseExceptionHandler → UseSerilogRequestLogging
         → (Dev) MapOpenApi/Scalar → UseCors → Map*Endpoints → Run
```

- **不可移除**：`AddProblemDetails`、`UseExceptionHandler`、`MigrateAndSeedAsync`、`public partial class Program { }`。
- **新增 middleware 建議插入點**：`UseCors()` **之後**、`Map*Endpoints()` **之前** 加入 `UseAuthentication()` / `UseAuthorization()`（僅在 flag 啟用時註冊，見各 Stage）。

### 0.3 端點盤點與保護範圍

#### 公開讀取（**各 Stage 均不得要求認證**）

| Method | Path | 實作檔 | 備註 |
|--------|------|--------|------|
| GET | `/health` | `Features/Health/HealthEndpoints.cs` | Liveness |
| GET | `/health/ready` | 同上 | Readiness（DB + Redis） |
| GET | `/api/districts` | `Features/Districts/DistrictEndpoints.cs` | Redis 快取 5 分鐘 |
| GET | `/api/districts/{id}` | 同上 | |
| GET | `/api/transactions` | `Features/Transactions/TransactionEndpoints.cs` | 篩選／排序／分頁 |
| GET | `/api/transactions/{id}` | 同上 | |
| GET | `/api/analysis/price-distribution` | `Features/Analysis/AnalysisEndpoints.cs` | |
| GET | `/api/analysis/city-trend` | 同上 | 靜態 mock 趨勢 |
| GET | `/api/analysis/ping-price` | 同上 | 未納入現有 15 測試，仍屬公開 |
| GET | `/api/analysis/district-summary` | 同上 | |
| GET | `/api/analysis/trend` | 同上 | |
| GET | `/api/crawl-tasks` | `Features/CrawlTasks/CrawlTaskEndpoints.cs` | |

#### 後台／會員（**Stage 2 起才在 flag ON 時保護**）

| Method | Path | 實作檔 | 現有契約 |
|--------|------|--------|----------|
| GET | `/api/members` | `Features/Members/MemberEndpoints.cs` | `200` + `MemberDto[]` |
| PATCH | `/api/members/{id:guid}/status` | 同上 | body: `{ "status": "<MemberStatus>" }`；合法值見 `MemberStatus` enum |
| PATCH | `/api/members/{id:guid}/plan` | 同上 | body: `{ "plan": "<Plan>" }` |

**請求／回應契約（現有，Stage 2 前後 JSON 形狀不變）**：

```csharp
// MemberEndpoints.cs（route handler 內嵌 record）
public record UpdateStatusRequest(string Status);
public record UpdatePlanRequest(string Plan);

// Contracts/Dtos.cs
public record MemberDto(
    string Id, string Name, string Email, string Avatar, string Provider, string Plan,
    string Status, string Purpose, string CreatedAt, string? ReviewedAt, string? Note);
// Provider/Plan/Status 序列化為小寫字串（ToLowerInvariant）
```

- 驗證失敗：`400` + RFC 9457 `ValidationProblem`（`status` / `plan` 欄位錯誤訊息含合法值列表）。
- 找不到會員：`404` + `Results.Problem(title: "找不到會員")`。

#### 尚未實作、架構文件已規劃（**Stage 3 新增**）

| Method | Path | 對應前端 |
|--------|------|----------|
| POST | `/api/auth/sso/{provider}` | `mockSSO(provider)` → `SsoProfile` |
| POST | `/api/auth/register` | `registerMember({ profile, name, plan, purpose })` |
| GET | `/api/auth/me` | `getCurrentUser()` |

> 參考：`backend/docs/ARCHITECTURE.md` §5.2；`Features/Auth/` 目錄尚不存在。

### 0.4 領域模型：`Member` **尚無 role**

```csharp
// Domain/Entities/Member.cs — 現有欄位
public Guid Id { get; set; }
public string Name, Email, Avatar { get; set; }
public Provider Provider { get; set; }      // Google, Apple
public Plan Plan { get; set; }              // Free, Pro, Enterprise
public MemberStatus Status { get; set; }  // Pending, Active, Rejected, Suspended
public string Purpose { get; set; }
public DateTime CreatedAt { get; set; }
public DateTime? ReviewedAt { get; set; }
public string? Note { get; set; }
```

- DB：`members` 表（migration `20260604000146_InitialCreate`）；`Email` unique index。
- 種子：`DbSeeder.Members()` 8 筆，與前端 `SEED_MEMBERS` 對齊（email／狀態／方案一致；**後端 Id 為 Guid，前端為 `m-1001` 字串**——串接時以前端改呼叫 API 或後端回傳 Guid 字串為準）。

### 0.5 前端模擬 SSO／會員模型（Stage 3 對齊目標）

| 前端 | 後端對應 |
|------|----------|
| `Provider = 'google' \| 'apple'` | `Provider` enum → JSON 小寫 |
| `Plan` / `MemberStatus` 小寫字串 | 同 `MemberDto` |
| `mockSSO('google')` → `demo.user@gmail.com` | Stage 3 mock 端點應回傳相同 profile |
| `mockSSO('apple')` → `demo.user@icloud.com` | 同上 |
| `registerMember` → `status: 'pending'` | `POST /api/auth/register` 建立 `MemberStatus.Pending` |
| `AdminView` 無前端 role 檢查 | 後台保護靠 JWT `role=admin` claim（Stage 2+） |
| `localStorage` 存 token（**待 Stage 3 前端串接**） | `Authorization: Bearer <jwt>` |

### 0.6 既有整合測試（15 案例）

| # | 測試方法 | 端點 | Stage 2 flag ON 時預期變化 |
|---|----------|------|---------------------------|
| 1 | `Health_ReturnsOk` | GET `/health` | 無 |
| 2 | `HealthReady_WhenDbAndRedisUp_ReturnsOk` | GET `/health/ready` | 無 |
| 3 | `Districts_ReturnsNonEmptyArray` | GET `/api/districts` | 無 |
| 4–8 | `Transactions_*` | GET `/api/transactions`… | 無 |
| 9–10 | `TransactionById_*` | GET `/api/transactions/{id}` | 無 |
| 11–12 | `Analysis_*` | GET `/api/analysis/*` | 無 |
| 13 | `CrawlTasks_ReturnsNonEmpty` | GET `/api/crawl-tasks` | 無 |
| 14 | `Members_ReturnsNonEmpty` | GET `/api/members` | **需 Bearer admin JWT 或 flag OFF** |
| 15 | `Members_PatchStatus_*` | PATCH status | **需 Bearer** |
| 16 | `Members_PatchPlan_*` | PATCH plan | **需 Bearer** |

（編號 14–16 對應檔內 3 個 Members 相關測試，合計 **15** 個 `[Fact]`。）

### 0.7 建議設定鍵（全計劃共用）

```json
"Auth": {
  "Enabled": false,
  "ProtectAdminEndpoints": false,
  "MockSsoEnabled": false,
  "Jwt": {
    "Issuer": "shijiatong",
    "Audience": "shijiatong-web",
    "SigningKey": "",
    "AccessTokenMinutes": 60
  }
}
```

| 設定鍵 | 預設 | 啟用階段 | 說明 |
|--------|------|----------|------|
| `Auth:Enabled` | `false` | Stage 1+ | 註冊認證服務與 middleware；`false` 時零行為變更 |
| `Auth:ProtectAdminEndpoints` | `false` | Stage 2 | 僅在 `Enabled=true` 且本項 `true` 時，三個 admin 端點需 `admin` 角色 |
| `Auth:MockSsoEnabled` | `false` | Stage 3 | 開啟 `/api/auth/*` mock 登入／註冊 |
| `Auth:Jwt:SigningKey` | `""` | Stage 3 | 空值時 Stage 3 **不得啟動簽發**（fail-fast 或保持端點 503） |
| `Auth:Jwt:*` | 見上 | Stage 3 | Issuer／Audience／過期分鐘 |

**環境變數對應**（沿用專案慣例）：

- `Auth__Enabled=true`
- `Auth__ProtectAdminEndpoints=true`
- `Auth__Jwt__SigningKey=<secret>`（僅 dev/staging；prod 走 secret manager）

**dev / prod 切換建議**：

| 環境 | `Enabled` | `ProtectAdminEndpoints` | `MockSsoEnabled` | 備註 |
|------|-----------|-------------------------|------------------|------|
| 本機預設 | false | false | false | 與現況相同，15 測試免改 |
| 本機驗證 Stage 2 | true | true | false | `appsettings.Development.json` 區塊或 user-secrets |
| CI 整合測試 | false（預設） | false | false | Stage 2+ 另加「flag ON」專用測試類別 |
| prod（Stage 4 前） | true | true | false | 僅 JWT 保護 admin；mock SSO 關閉 |

---

## Stage 1｜授權骨架 + Feature Flag（預設 OFF、零行為變更）

### 1. 目標與完成定義

**目標**：建立可開關的認證／授權基礎建設（設定模型、DI 擴充、空 middleware 掛點），**預設完全不改變任何 HTTP 回應**。

**完成定義（可驗收）**：

- [ ] `Auth:Enabled=false`（預設）時，15 個整合測試全部綠燈，且行為與現況位元組級一致（無 401）。
- [ ] `dotnet build` / `dotnet test` 通過；未新增對外 API 路由。
- [ ] `Program.cs` 仍含 `MigrateAndSeedAsync`、`AddProblemDetails`、`UseExceptionHandler`、`partial Program`。
- [ ] 新增設定區塊與 `.env.example` 註解，但 production 預設值皆 OFF。

### 2. 要改／新增的檔案與內容要點

| 動作 | 路徑 | 類型 | 要點 |
|------|------|------|------|
| 新增 | `Infrastructure/Auth/AuthOptions.cs` | **additive** | 對應 `Auth` 設定區塊；`IOptions<AuthOptions>` |
| 新增 | `Infrastructure/Auth/AuthServiceExtensions.cs` | **additive** | `AddShijiatongAuth(this IServiceCollection, IConfiguration)`：僅在 `Enabled` 時 `AddAuthentication().AddJwtBearer(...)` 占位（可先不驗證簽章） |
| 新增 | `Infrastructure/Auth/AuthConstants.cs` | **additive** | `PolicyNames.Admin`、`ClaimTypes` 包裝（`role`, `sub`, `email`） |
| 修改 | `Program.cs` | **additive** | `builder.Services.Configure<AuthOptions>(...)`；`AddShijiatongAuth`；條件 `UseAuthentication`/`UseAuthorization`（僅 `Enabled==true`） |
| 修改 | `appsettings.json` | **additive** | 加入 `Auth` 區塊，預設全 `false` |
| 修改 | `appsettings.Development.json` | **additive**（可選） | 註解範例，預設仍 false |
| 修改 | `backend/.env.example` | **additive** | 文件化 `Auth__*` 變數 |
| **不修改** | `MemberEndpoints.cs`、各公開 `*Endpoints.cs` | — | 無 `.RequireAuthorization()` |

**現有契約**：無 API 變更。

**NuGet（本 Stage 可選擇先不加，僅加空殼；若加則為後續 Stage 鋪路）**：

| 套件 | 版本 | 用途 |
|------|------|------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.0` | 與 `Microsoft.AspNetCore.OpenApi` 同 major |

> Stage 1 若引入 JwtBearer 套件但未啟用 `Enabled`，行為仍應為零影響。

### 3. 設定／Flag

- 僅使用 `Auth:Enabled`（預設 `false`）。
- `ProtectAdminEndpoints`、`MockSsoEnabled` 在本 Stage **尚未讀取**（可預先出現在設定檔，實作為 no-op）。

### 4. 對既有 15 個整合測試的影響

| 影響 | 作法 |
|------|------|
| **無**（預設 flag OFF） | `IntegrationTestFixture` 不必改 |
| 新增測試（建議） | `Auth_Skeleton_WhenDisabled_DoesNotRegisterAuthMiddleware`：反射或發送請求確認無 401（可選，1 例） |
| 新增測試（建議） | `Auth_Skeleton_WhenEnabled_StillAllowsPublicEndpoints`：`Enabled=true` 但無 `ProtectAdminEndpoints`，確認 GET `/api/districts` 仍 200 |

### 5. 回滾方式

- 設定 `Auth:Enabled=false`（或移除 `Auth` 區塊，程式應 treat 為 false）。
- **無 DB migration**。
- 若需完全移除骨架：刪除 `Infrastructure/Auth/*` 與 `Program.cs` 兩行註冊即可。

### 6. 風險與注意

- **勿**在 Stage 1 對任何 `MapGet`/`MapPatch` 加 `.RequireAuthorization()`。
- `UseAuthentication` 順序錯誤可能影響 CORS preflight；維持 `UseCors` → `UseAuthentication` → `UseAuthorization`。
- 引入 JwtBearer 後，確保 `Enabled=false` 時不呼叫 `AddJwtBearer` 或不用 `ValidateIssuer` 等會在啟動時 fail 的設定。
- OpenAPI／Scalar 在 dev 仍可正常瀏覽；本 Stage 可不處理 security scheme。

---

## Stage 2｜只保護 Admin 寫入端點

> 保護範圍：`GET /api/members`、`PATCH /api/members/{id}/status`、`PATCH /api/members/{id}/plan`。  
> 語意上 GET 為「後台讀取」，與架構文件「後台會員管理」一致，**整組 `/api/members` 在 Protect 開啟時皆需 admin**。

### 1. 目標與完成定義

**目標**：在 `Auth:Enabled=true` 且 `Auth:ProtectAdminEndpoints=true` 時，上述三端點必須具備有效 JWT 且 `role=admin`；其餘端點與 flag OFF 時行為不變。

**完成定義**：

- [ ] flag OFF：15 測試全過（與現況相同）。
- [ ] flag ON、無 token：三端點回 `401`（或 `403`，團隊擇一後文件化；建議未帶 token → `401`，token 無 admin → `403`）。
- [ ] flag ON、有效 admin JWT：三端點回應 JSON 與現有契約一致（含 400/404 語意不變）。
- [ ] 公開端點（§0.3 表）在 flag ON 時仍無需 token。

### 2. 要改／新增的檔案與內容要點

| 動作 | 路徑 | 類型 | 要點 |
|------|------|------|------|
| 修改 | `Infrastructure/Auth/AuthServiceExtensions.cs` | **行為變更（flag 下）** | `Enabled && ProtectAdminEndpoints` 時設定 JWT Bearer 驗證（SigningKey 可與 Stage 3 共用）；`AddAuthorization` + policy `Admin`（`RequireRole("admin")`） |
| 修改 | `Features/Members/MemberEndpoints.cs` | **行為變更（flag 下）** | 對 `MapGroup("/api/members")` 套用條件式 `.RequireAuthorization("Admin")`（建議抽 `ApplyAdminProtectionIfEnabled(IEndpointConventionBuilder)` 擴充，避免重複 if） |
| 新增 | `Infrastructure/Auth/AdminAuthorizationExtensions.cs` | **additive** | 封裝「僅當 `ProtectAdminEndpoints` 才 RequireAuthorization」 |
| 修改 | `Program.cs` | **additive** | 確保 `UseAuthentication`/`UseAuthorization` 在 flag ON 時執行 |

**JWT Claims 契約（Stage 2 測試用，Stage 3 正式簽發沿用）**：

```json
{
  "sub": "<member-guid>",
  "email": "admin@example.com",
  "role": "admin"
}
```

**仍不變的 HTTP 契約**：`MemberDto`、`UpdateStatusRequest`、`UpdatePlanRequest`、400/404 形狀。

**NuGet**：

| 套件 | 版本 | 備註 |
|------|------|------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.0` | Stage 1 未加則本 Stage 必加 |
| `System.IdentityModel.Tokens.Jwt` | `8.x`（随 JwtBearer 传递） | 測試內簽發 token 用 |

### 3. 設定／Flag

```json
"Auth": {
  "Enabled": true,
  "ProtectAdminEndpoints": true,
  "Jwt": {
    "SigningKey": "dev-only-min-32-chars-secret!!",
    "Issuer": "shijiatong",
    "Audience": "shijiatong-web"
  }
}
```

- **dev**：本機驗證時於 `appsettings.Development.json` 或 user-secrets 設定 SigningKey（≥256 bits 建議）。
- **CI 預設**：維持 `false/false`，舊 15 測試不動。
- **prod（此 Stage）**：僅在準備好 secret 與 admin 種子後才開 `ProtectAdminEndpoints`；可先 `Enabled=true` 僅掛 middleware 但不保護（若需漸進）。

### 4. 對既有 15 個整合測試的影響與更新

| 測試 | flag 預設 OFF | 更新方式 |
|------|---------------|----------|
| 1–13（非 Members admin） | 通過 | 不變 |
| `Members_ReturnsNonEmpty` | 通過 | 不變 |
| `Members_PatchStatus_*` | 通過 | 不變 |
| `Members_PatchPlan_*` | 通過 | 不變 |

**新增測試檔**（建議 `AuthAdminEndpointTests.cs`，獨立 collection 或在 fixture 加 helper）：

| 新測試 | 作法 |
|--------|------|
| `Members_WhenProtectionOn_WithoutToken_Returns401` | `WebApplicationFactory` + `UseSetting("Auth:Enabled","true")` + `ProtectAdminEndpoints` + 設定 SigningKey |
| `Members_WhenProtectionOn_WithAdminToken_Returns200` | 測試 helper `CreateAdminClient()` 簽發 JWT |
| `Members_WhenProtectionOn_NonAdminToken_Returns403` | claim `role=user` |
| `Districts_WhenProtectionOn_StillPublic` | 確認公開端點未誤傷 |

**測試簽發 helper（新增於測試專案）**：

- `Tests/Auth/TestJwtFactory.cs`：讀取與 API 相同的 `Auth:Jwt` 設定簽發 token（**僅測試**）。

### 5. 回滾方式

- `Auth:ProtectAdminEndpoints=false` → admin 端點恢復匿名（若 `Enabled=true` 仍無強制）。
- `Auth:Enabled=false` → 完全恢復 Stage 0 行為。
- **無 migration**。

### 6. 風險與注意

- **最大風險**：誤對 `MapTransactionEndpoints` 等加授權 → demo／公開查詢壞掉；僅限 `MemberEndpoints`。
- `GET /api/members` 被保護後，未串接 auth 的前端 Admin 會失敗——預期在 Stage 3 前端改打 API 時一併處理；Stage 2 部署時應 **預設 flag OFF**。
- 401/403 回應應走既有 `ProblemDetails` 管線（勿拋未處理例外）。
- CORS：`AllowAnyHeader()` 已含 `Authorization`；若改 credentials 模式需另議（目前不需要）。
- 種子會員**尚無 admin 角色**（Stage 3 migration 才補）；Stage 2 測試靠測試專用 JWT，不靠 DB。

---

## Stage 3｜Token 簽發（模擬 SSO、對齊前端）+ Member Role Migration

### 1. 目標與完成定義

**目標**：實作 `/api/auth/*` mock 流程與 JWT 簽發；DB 新增 `Member.Role`；種子至少一筆 `admin`；前端模型可對齊登入。

**完成定義**：

- [ ] `Auth:MockSsoEnabled=true` 且設定 SigningKey 時：
  - `POST /api/auth/sso/google` 回傳與前端 `SSO_IDENTITIES.google` 相同欄位（見下）。
  - `POST /api/auth/register` 建立 `pending` 會員並回傳 JWT + `MemberDto`。
  - `GET /api/auth/me` 依 Bearer 回傳目前會員。
- [ ] 以 admin 帳號登入取得 JWT 後，可呼叫 Stage 2 受保護的三端點。
- [ ] `MemberDto` **可選擇** 不加 `role` 欄位（避免破壞前端型別）；role 僅在 JWT claim。
- [ ] Additive migration 套用後，舊資料 `Role` 預設 `user`；指定種子 email 為 `admin`。
- [ ] flag OFF 時：無 `/api/auth` 路由（或 404），15 原測試仍過。

**對齊前端的 API 契約（建議）**：

```csharp
// POST /api/auth/sso/{provider}  provider: google | apple
// Response 200（對齊 SsoProfile）
{ "email": "...", "name": "...", "avatar": "...", "provider": "google" }

// POST /api/auth/register
// Request
{ "email", "name", "avatar", "provider", "plan", "purpose" }
// Response 200
{ "token": "<jwt>", "member": <MemberDto> }

// GET /api/auth/me
// Response 200 <MemberDto> | 401
```

**Mock SSO 固定身分（與 `src/lib/auth.ts` 一致）**：

| provider | email | name | avatar |
|----------|-------|------|--------|
| google | `demo.user@gmail.com` | 示範使用者 | 示 |
| apple | `demo.user@icloud.com` | 示範使用者 | 示 |

### 2. 要改／新增的檔案與內容要點

| 動作 | 路徑 | 類型 | 要點 |
|------|------|------|------|
| 新增 | `Domain/Entities/MemberRole.cs` | **additive** | `enum MemberRole { User, Admin }` |
| 修改 | `Domain/Entities/Member.cs` | **additive** | `public MemberRole Role { get; set; } = MemberRole.User;` |
| 修改 | `Infrastructure/AppDbContext.cs` | **additive** | `Role` 字串轉換，max length 16 |
| 新增 | `Infrastructure/Migrations/*_AddMemberRole.cs` | **additive migration** | `role` 欄位 NOT NULL default `'user'`；**Down** 可移除欄位 |
| 修改 | `Infrastructure/DbSeeder.cs` | **additive** | 例如 `kuanyu.chen@gmail.com` → `Admin`（或新增專用 `admin@shijiatong.local` 種子）；與前端無衝突 |
| 新增 | `Features/Auth/AuthEndpoints.cs` | **additive** | `MapAuthEndpoints`；`MockSsoEnabled` 才 map |
| 新增 | `Infrastructure/Auth/IJwtTokenService.cs` + 實作 | **additive** | 簽發 access token（claims: sub, email, role） |
| 新增 | `Infrastructure/Auth/MockSsoService.cs` | **additive** | 回傳固定 profile；不呼叫外部 IdP |
| 修改 | `Contracts/Dtos.cs` | **盡量不變** | `MemberDto` 維持原 11 欄；role 不放 DTO |
| 修改 | `Program.cs` | **additive** | `app.MapAuthEndpoints()` |
| 修改 | `MemberEndpoints.cs` | 不變契約 | 仍僅授權，不改 body |

**登入流程（對齊 `AuthView.tsx`）**：

1. `POST /api/auth/sso/{provider}` → profile  
2. 以 email 查 `Members`：存在且 `Active` → 簽發 JWT 回傳（或 `POST /api/auth/login` 簡化）；存在但非 Active → `403` + 說明  
3. 不存在 → 前端走註冊表單 → `POST /api/auth/register` → `pending` + JWT  

**NuGet**：

| 套件 | 版本 |
|------|------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.0` |
| `Microsoft.IdentityModel.Tokens` | `8.x`（随 JwtBearer） |

### 3. 設定／Flag

```json
"Auth": {
  "Enabled": true,
  "ProtectAdminEndpoints": true,
  "MockSsoEnabled": true,
  "Jwt": {
    "SigningKey": "<from-env>",
    "Issuer": "shijiatong",
    "Audience": "shijiatong-web",
    "AccessTokenMinutes": 60
  }
}
```

- **dev**：`Auth__Jwt__SigningKey` 放 `.env`（不入庫）；`MockSsoEnabled=true`。
- **prod（Stage 4 前）**：`MockSsoEnabled=false`（僅關閉 mock 路由，JWT 保護仍可開）。
- **測試**：fixture 子類或 `UseSetting` 注入 key + 開 mock。

### 4. 對既有 15 個整合測試的影響與更新

| 測試 | 預設 | 更新 |
|------|------|------|
| 1–13 | 不變 | — |
| 14–16 Members | flag OFF 通過 | 維持；另建 **Auth 專用** 測試類 |

**本 Stage 建議新增測試**：

| 測試 | 說明 |
|------|------|
| `Auth_MockSso_Google_ReturnsExpectedProfile` | 對齊 `auth.ts` |
| `Auth_Register_CreatesPendingMember_AndReturnsToken` | |
| `Auth_Me_WithValidToken_ReturnsMember` | |
| `Auth_Me_WithoutToken_Returns401` | |
| `Auth_AdminSeeded_CanListMembers_WhenProtectionOn` | E2E admin |
| `Migration_AddMemberRole_AppliesOnStartup` | 可併入現有 fixture 啟動即 migrate |

**Migration 與測試**：`WebApplicationFactory` 已執行 `MigrateAndSeedAsync`；新 migration 會在測試 DB 自動套用——確認種子 idempotent。

### 5. 回滾方式

| 層級 | 作法 |
|------|------|
| 功能 | `MockSsoEnabled=false` 關閉 auth 路由 |
| 授權 | `ProtectAdminEndpoints=false` |
| 資料 | `dotnet ef database update <PreviousMigration>`（**Down** `AddMemberRole`）；或保留欄位僅停用邏輯（role 欄位可留） |
| 程式 | 移除 `Features/Auth`、還原 `Member.Role`（需另 migration 若已上 prod） |

### 6. 風險與注意

- **Guid vs 字串 id**：前端 `m-1001` 與後端 `Guid` 不一致；Stage 3 前端串接時改為使用 API 回傳之 `MemberDto.id`。
- 註冊重複 email：DB unique index → `409` ProblemDetails（勿 500）。
- `demo.user@gmail.com` 若被註冊為一般 user，需種子或 migration **明確標 admin** 以免鎖死後台。
- JWT 勿寫入 PII 至 log；SigningKey 不得進 git。
- `register` 不應允許客戶端指定 `role=admin`（僅 server 種子／未來 OAuth 映射）。
- 公開讀取端點仍不得要求 token。

---

## Stage 4｜真 Google OAuth（上線前；本計劃不細展）

### 1. 目標與完成定義（一句話）

**目標**：以 Google（及可選 Apple）IdP 取代 mock SSO，沿用 Stage 2–3 的 JWT 與 admin 授權模型。

**完成定義**：生產環境 `MockSsoEnabled=false`；使用者可經 Google 登入取得 JWT；admin 端點受保護；無 mock 端點暴露。

### 2. 待辦清單（不展開實作細節）

- [ ] Google Cloud OAuth client（Web application）與 redirect URI（含 GitHub Pages / 正式網域）。
- [ ] 新增 NuGet：`Microsoft.AspNetCore.Authentication.Google` `10.0.0`（Apple 另議）。
- [ ] 設定：`Authentication:Google:ClientId`、`ClientSecret`（**僅 env／secret store**）。
- [ ] `POST /api/auth/sso/{provider}` 改為 302 導向 IdP 或改為標準 `/signin-google` callback 流程。
- [ ] 帳號連結策略：email 對應既有 `Member`；首次登入建立 `pending` 或依產品規則。
- [ ] Admin 角色來源：Google group／allowlist email／DB `Role` 欄位（與 Stage 3 種子一致）。
- [ ] 撤銷 mock 路由與 `MockSsoEnabled` 預設。
- [ ] 安全審查：HTTPS only、refresh token（若需要）、CORS 正式 origin 列表。
- [ ] 更新 `ARCHITECTURE.md`、前端 `auth.ts` 改呼叫真實流程。

### 3–6. 設定／測試／回滾／風險

- **設定**：沿用 `Auth:Jwt`；新增 Google secret；**不依賴** mock flag。
- **測試**：Contract 測試 + 手動 OAuth；CI 通常不跑真 Google（用 test doubles）。
- **回滾**：關閉 Google handler、暫開 `MockSsoEnabled`（僅非 prod）或 `ProtectAdminEndpoints=false`。
- **風險**：client secret 外洩、redirect URI 錯誤、與 Apple Sign In 差異；**Stage 1–3 不得預先引入 Google secret**。

---

## 附錄 A：Middleware 目標順序（Stage 2+ flag ON）

```text
UseExceptionHandler()
UseSerilogRequestLogging()
UseCors()
UseAuthentication()      // Auth:Enabled
UseAuthorization()       // Auth:Enabled
MapHealthEndpoints()
MapDistrictEndpoints()
…
MapMemberEndpoints()     // 條件 RequireAuthorization("Admin")
MapAuthEndpoints()       // Stage 3, MockSsoEnabled
```

## 附錄 B：各 Stage NuGet 匯總

| Stage | 套件 | 版本 |
|-------|------|------|
| 1（可選）/ 2（必備） | `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.0` |
| 3 | 同上 + `Microsoft.IdentityModel.Tokens`（傳遞依賴） | `8.x` |
| 4 | `Microsoft.AspNetCore.Authentication.Google` | `10.0.0` |

## 附錄 C：與架構文件差異說明

| 項目 | `ARCHITECTURE.md` | 本計劃 |
|------|-------------------|--------|
| Phase 3 一次到位 | 真 OAuth + JWT | 拆 Stage 1–3 mock，Stage 4 真 OAuth |
| `Features/Auth/` | 已列出 | Stage 3 才建立 |
| Admin 保護 | 與認證同階段 | Stage 2 可獨立驗證（測試 JWT） |

## 附錄 D：待產品／技術確認（Opus review）

1. **401 vs 403** 語意是否採「未帶 token / 帶 token 無權限」區分？
2. **`GET /api/members` 是否與 PATCH 同級保護**（本計劃：是）——若有「公開會員目錄」需求需另議。
3. **Admin 種子帳號** 選哪一筆 email（或新增 `admin@…`）？
4. **`MemberDto` 是否暴露 `role`** 給前端 Admin 判斷（目前建議僅 JWT）？
5. **Refresh token** 是否在 Stage 3 纳入，或僅 access token（前端 localStorage）？
6. **Stage 2 部署預設** 是否堅持 `ProtectAdminEndpoints=false` 直到前端串接完成？

---

*文件版本：1.0 · 對應 commit 範圍：`implement-shijiatong` 分支後端現況。*
