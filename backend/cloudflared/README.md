# Cloudflare Tunnel 設定

讓 API 在「無公網 IP、不開任何入站 Port」的情況下對外。流量路徑：

```
Cloudflare Edge → cloudflared(容器) → http://nginx:80 → api:8080
```

## 設定步驟

1. 登入 Cloudflare **Zero Trust** → **Networks → Tunnels** → **Create a tunnel**（類型選 *Cloudflared*）。
2. 建立後複製 **Tunnel Token**（`eyJ...` 開頭那串）。
3. 在 `backend/.env` 填入：
   ```
   CLOUDFLARE_TUNNEL_TOKEN=eyJ...
   ```
4. 設定 **Public Hostname**：
   - Subdomain/Domain：`api.<your-domain>`
   - Service：`HTTP` → `nginx:80`
5. 啟動（在 `backend/` 目錄）：
   ```bash
   docker compose -f docker-compose.yml -f docker-compose.prod.yml \
     --profile prod --profile tunnel up -d --build
   ```
   DNS 由 Cloudflare 自動建立，數秒後即可用 `https://api.<your-domain>` 連到 API。

## 備註
- 未填 token 前，**不要**加 `--profile tunnel`，僅用 `--profile prod` 即可在本機 `http://localhost:8080` 測試正式組態。
- Tunnel 為 outbound-only，主機防火牆可全關入站；正式環境的 DB / Redis / Nginx 皆不對公網開 Port。
