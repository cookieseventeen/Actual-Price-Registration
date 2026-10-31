# 實價登錄資料匯入 — 第 1 段 PoC（B1）

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

## 第 2、3 段要先拍板的事

| # | 問題 | 建議 |
|---|---|---|
| 1 | 預售屋交屋（51.8%）怎麼處理 | 保留，`Trade` 標成「預售屋」（其餘「成屋」），趨勢圖預設依成交日、可篩掉；不另抓預售屋 b 檔以免重複計算 |
| 2 | 單價異常值 | 匯入時保留原值但標 `IsOutlier`（< 5 或 > 200 萬/坪），統計/趨勢排除 |
| 3 | 社區名 | `Community` 改可為空；前端改顯示「路名 + 樓層」 |
| 4 | 經緯度 | 依決定先用行政區中心點；`Lat/Lng` 保留欄位，之後再 geocoding |
| 5 | 重劃區 `Section` / 區 `Zone` | 改可為空（或之後用路名對照表補） |
| 6 | 29 區 id | 沿用現有拼音 id（xitun…），新增 17 區照同規則 |
| 7 | API Docker build context | 目前是 `src/Shijiatong.Api`，API 參照 `Shijiatong.Lvr` 後要改成 `backend/` |
