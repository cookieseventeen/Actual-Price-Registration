# 實價登錄資料匯入（roadmap B）— 第 1 段 PoC、第 2 段 staging

> 2026-10-31。範圍：台中市「不動產買賣」（`b_lvr_land_a.csv`）。**只下載＋解析＋統計，不寫 DB、不動既有資料。**
> 已決定：先只做買賣；座標先用行政區中心點；真實資料**直接取代** demo 種子資料。
> 完整數據見 [`lvr-poc-report.md`](lvr-poc-report.md)（由工具產生）。

## 資料來源（已實測）

| 項目 | 內容 |
|---|---|
| 本期 | `https://plvr.land.moi.gov.tw/Download?fileName=b_lvr_land_a.csv`（官方約每月 1、11、21 日更新） |
| 歷史季別 | `https://plvr.land.moi.gov.tw/DownloadSeason?season=115S2&fileName=b_lvr_land_a.csv` |
| 格式 | UTF-8（含 BOM）、第 1 列中文欄名、第 2 列英文欄名、33 欄 |
| 無資料季別 | 回 `text/html` 錯誤頁（如 101S1）或只有表頭 → 工具判為「無資料」、不寫快取 |
| 量級 | 台中一季約 1 萬列，保留約 8 千筆住宅買賣 |
| 授權 | 政府開放資料，**授權條款與出處標示方式待正式匯入前確認** |

**禮貌下載**：季別檔抓過就用本機快取、不再抓；本期檔一天最多抓一次；請求間隔預設 3 秒；
User-Agent 明確標示 `ShijiatongLvrImport/0.1`；不碰有驗證碼的互動查詢頁。

## 怎麼跑

```bash
cd backend
# 本機無 dotnet 時用容器（下載快取在 backend/.lvr-cache/，已 gitignore）
docker run --rm -v "$PWD":/src -v shijiatong-nuget:/root/.nuget/packages -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc "dotnet run --project tools/Shijiatong.LvrPoc -- --season 115S1 --season 115S2 --season 115S3 --current --out docs/lvr-poc-report.md"

# 單元測試（純函式，不需 DB / Testcontainers）
docker run --rm -v "$PWD":/src -v shijiatong-nuget:/root/.nuget/packages -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/Shijiatong.Lvr.Tests
```

程式結構：
- `src/Shijiatong.Lvr/` — 純函式庫（無 EF/ASP.NET 相依），第 2、3 段直接沿用
  - `LvrCsv` 切欄/表頭、`LvrParsing` 民國日期・中文樓層・單位換算・路名、`LvrNormalizer` 排除規則＋正規化、`LvrDownloader` 快取＋節流下載
- `tools/Shijiatong.LvrPoc/` — PoC 指令程式（下載 → 統計報告 Markdown）
- `tests/Shijiatong.Lvr.Tests/` — 59 個單元測試（用真實資料列當測試資料）

## 主要發現（115S1–S3 + 本期，31,827 列）

1. **格式穩定**：0 筆切欄錯誤；必要欄位（交易日、總價、面積）0 筆無效；樓層/總樓層/屋齡解析 ≈ 100%、路名抽取 99.1%。
2. **保留率約 81%**（25,745 筆）：排除的主要是純土地/車位（4,679）與親友等特殊關係交易（1,398）。
3. **「編號」可當不重複寫入的鍵**：單檔內 0 重複、跨 4 檔只有 1 個重疊（應為更正）。本期檔與 115S3 無重疊 → 本期是增量。
4. **⚠️ 一半是「預售屋交屋」**：51.8% 帶「預售屋、或土地及建物分件登記案件」備註，**交易日 = 當年簽約日**（中位數 2023-08，屋齡 0），
   交屋登記時才出現在買賣檔。一般成屋的成交日中位數則是 2026-03。→ 直接影響趨勢圖與「最新成交」排序。
5. **單價一定用官方欄位**：車位單獨計價的 13,088 筆，用 mock 的「總價 ÷ 坪數」會比官方單價低 11.5%（中位數）。
   另有 5,175 筆車位價併入總價、官方也無法拆，前端宜標示「含車位」。
6. **異常值要處理**：單價 < 5 萬/坪 15 筆、> 200 萬/坪 24 筆（最大 1,566），P1–P99 為 13.8–77.6。
7. **行政區**：真實資料有 28 區，現有種子 12 區只涵蓋 75.7% 筆數 → 直接取代時 `districts` 要擴成全台中 29 區（含和平區）。
8. **確定缺的欄位**：社區名（買賣檔沒有）、經緯度（沒有，門牌去識別化）、重劃區/生活圈（沒有）。

## 第 2、3 段決策（2026-10-31 使用者確認：全部照建議）

| # | 問題 | 決定 |
|---|---|---|
| 1 | 預售屋交屋（51.8%）怎麼處理 | 保留，`Trade` 標成「預售屋」（其餘「成屋」），趨勢圖預設依成交日、可篩掉；不另抓預售屋 b 檔以免重複計算 |
| 2 | 單價異常值 | 匯入時保留原值但標 `IsOutlier`（< 5 或 > 200 萬/坪），統計/趨勢排除 |
| 3 | 社區名 | `Community` 改可為空；前端改顯示「路名 + 樓層」 |
| 4 | 經緯度 | 依決定先用行政區中心點；`Lat/Lng` 保留欄位，之後再 geocoding |
| 5 | 重劃區 `Section` / 區 `Zone` | 改可為空（或之後用路名對照表補） |
| 6 | 29 區 id | 沿用現有拼音 id（xitun…），新增 17 區照同規則 |
| 7 | API Docker build context | ✅ 第 2 段已改為 `backend/`（Dockerfile 仍在 `src/Shijiatong.Api/`，`.dockerignore` 移到 `backend/`） |

---

# 第 2 段：原始資料暫存表 + 手動匯入（已完成）

原始列**完整保留**到 staging，尚未轉成 `transactions`（第 3 段才做），所以既有 demo 資料與所有公開端點都不受影響。

| 表 | 用途 |
|---|---|
| `lvr_staging_records` | 一列 = 一筆原始資料。PK = 官方「編號」；`Fields` 為 jsonb（中文欄名 → 原字串）；`ContentHash` 偵測官方更正 |
| `lvr_import_runs` | 每次匯入一筆：來源、狀態（running / succeeded / noData / failed）、新增 / 更新 / 未變 / 格式錯誤筆數、耗時 |

匯入規則：同編號且內容雜湊相同 → 略過；雜湊不同 → 覆寫（官方更正）；新編號 → 新增。
單一檔案的寫入在同一交易內，失敗時整批不落地，只記一筆 `failed` run。同時間只允許一個匯入（第二個回 409）。

## 端點（`Lvr:ImportEnabled=true` 才註冊；Auth 保護開啟時需 admin JWT）

| Method | Path | 說明 |
|---|---|---|
| POST | `/api/admin/lvr/import` | body `{"season":"115S3"}` 或 `{"current":true}`，可加 `"refresh":true` 忽略快取重抓 |
| GET | `/api/admin/lvr/runs?limit=20` | 最近匯入紀錄 |
| GET | `/api/admin/lvr/staging/summary` | staging 總數、依首次來源、依行政區 |

開關：`appsettings.json` 預設 **false**；`appsettings.Development.json` 為 true；compose 以 `LVR_IMPORT_ENABLED`（預設 false）注入。
**prod 在 admin 保護真正生效前不要開**，否則任何人都能觸發下載與寫入。下載快取在 compose volume `lvrcache`。

```bash
# 本機（dotnet run，Development）
curl -X POST localhost:8080/api/admin/lvr/import -H 'Content-Type: application/json' -d '{"season":"115S3"}'
curl localhost:8080/api/admin/lvr/staging/summary
```

## 驗證（2026-10-31，拋棄式 PG/Redis/API 容器 + 新 Dockerfile）

- flag 關 → 404；flag 開 → 115S1 / S2 / S3 / 本期 共 **31,826 筆**入 staging（每檔 0.2–2.6 秒，表約 45 MB）
- 115S2 有 1 筆「更新」= PoC 發現的跨檔重複編號（官方更正），行為正確
- 重跑 115S3 → 10,477 筆全部 unchanged（0.3 秒）
- 既有 `/api/transactions` 仍回 demo 資料；整合測試 36/36、單元測試 59/59

---

# 第 3 段：staging → transactions，取代 demo（已完成）

`POST /api/admin/lvr/rebuild`（與匯入同一把鎖、同一個開關）：讀全部台中買賣 staging → `LvrNormalizer` →
**全量取代** `transactions` → 重算 29 區統計 → 清 `districts:all` 快取。刪除與寫入在同一交易，失敗整批回滾。
匯入後要手動呼叫一次 rebuild 才會反映到公開端點（第 4 段排程會自動串起來）。

| 決策 | 實作 |
|---|---|
| 預售屋交屋 | `Trade = "預售屋"`（其餘「成屋」），`Date` 保留簽約日；`/api/analysis/*` 可帶 `trade=成屋` 篩掉 |
| 單價異常值 | `IsOutlier`（< 5 或 > 200 萬/坪，門檻在 `LvrNormalizer`）；列表/詳情照常顯示，所有 `/api/analysis/*` 與 districts 統計排除 |
| 缺欄位 | `Community`、`Section`、`Zone`、`Age` 改可為空；新增 `Address`（去識別化門牌）供前端顯示 |
| 座標 | `Lat/Lng` = 行政區中心點（`Domain/TaichungDistricts.cs`，約略區公所位置）；districts 也新增 `Lat/Lng` |
| 29 區 | 原 12 區 id 不變，新增 17 區（shalu、wuqi…）；`DbSeeder` 啟動時補齊，不動統計 |
| 區統計 | 以資料最新交易日為基準、**只用一般成屋**（預售屋交屋的交易日是簽約日）、排除異常值：近 12 個月平均單價 / 筆數；年變化只在前 12 個月 ≥ 10 筆且 ≥ 近 12 個月筆數一半時計算，否則 `null`（資料不足） |
| demo | migration `ReplaceDemoWithLvrData` 刪除 T0001–T0022、統計歸零；`DbSeeder` 不再灌 demo 成交 |
| city-trend | 原本寫死的假數字改為真實資料（最新交易月往回 12 個月） |

API 契約變更（前端尚未串接，影響僅在 roadmap E）：`TransactionDto` 新增 `address`、`isOutlier`，
`community` / `section` / `age` 可為 null；`DistrictDto` 新增 `lat` / `lng`，`zone` 可為 null。

**為什麼年變化目前多為 null**：只匯入 115 年季別時，前 12 個月（2024-10 ~ 2025-09）幾乎只有遲延登記與預售屋交屋，
例如西屯近 12 月 1,489 筆成屋、前 12 月只有 50 筆（均價 48.1 vs 36.9）——硬算會得到約 -23% 的假跌幅。
回補 114 年季別（第 4 段）後前期涵蓋度足夠，年變化就會自動算出來。

上線順序（prod）：部署新版 → 啟動自動套 migration（demo 清除）→ 開 admin 保護（A1）→ 開 `LVR_IMPORT_ENABLED`
→ 依序 import 季別 / 本期 → rebuild。**在 rebuild 之前公開端點會是空的**。
