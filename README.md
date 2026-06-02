# 實價通 — 台中市實價登錄查詢服務

A responsive, professional real-estate price-lookup interface for Taichung City
(台中市) built on the **OreNote design system** (Noto Sans TC, ore blue-green
`#38b2a0`, flat Slate cards, PrimeIcons).

This is a production React implementation of a high-fidelity prototype that was
designed in [Claude Design](https://claude.ai/design). The original prototype
(React + Babel-standalone) and the full design conversation are preserved under
[`project/`](project/) and [`chats/`](chats/) for provenance.

## Features

- **搜尋 (Search)** — district picker (12 行政區 cards with avg price / YoY change),
  building-type / layout / trade-type filter chips, market overview stats.
- **成交紀錄 (Transactions)** — card / table dual view, sortable columns, active
  filter chips, click-through to detail.
- **物件詳情 (Detail)** — headline price, key stats, community price-trend chart,
  data-source provenance, same-community + nearby comparables.
- **行情分析 (Analysis)** — city price trend, district ranking, price distribution
  histogram, monthly volume.
- **資料來源 (Data sources)** — per-district crawl-task status (done / running /
  queued / failed), schedule and durations.
- **匯出 (Export)** — format picker (CSV / Excel / JSON) + column selection +
  generate animation.
- **Tweaks panel** (teal launcher button, bottom-right) — theme color (6),
  light/dark mode, font size, density, nav layout (sidebar / topnav / rail),
  card-vs-table data presentation, chart style, bar rounding, gridlines. Choices
  persist to `localStorage`.

## Tech stack

- [Vite](https://vitejs.dev/) + [React 18](https://react.dev/) + TypeScript
- Pure-SVG charts (no chart library) driven by the `--brand` CSS variable
- PrimeIcons (self-hosted via npm), Noto Sans TC (self-hosted under `public/fonts/`)
- OreNote design tokens in `src/styles/tokens.css`

## Getting started

```bash
npm install
npm run dev        # start the dev server
npm run build      # type-check + production build to dist/
npm run preview    # serve the production build
```

## Project structure

```
src/
  main.tsx               entry point (loads tokens + app CSS + primeicons)
  App.tsx                routing + tweak state, applies tokens to :root
  types.ts               shared Filters / SortState / ViewMode types
  data/mock.ts           Taichung mock dataset (typed)
  lib/format.ts          price formatting helpers (萬 / 億)
  styles/
    tokens.css           OreNote design-system custom properties
    app.css              app component styles (built on the tokens)
  components/
    shell/Shell.tsx      TopBar, Sidebar, TopNav
    charts/Charts.tsx    TrendChart, BarChart, RankBars, Sparkline
    tweaks/Tweaks.tsx    useTweaks + floating Tweaks panel & controls
    SearchView.tsx
    ResultsView.tsx
    DetailView.tsx
    AnalysisView.tsx
    DataView.tsx         data-source page + ExportModal
```

> Data is mock/sample data modelled on the 內政部不動產交易實價查詢服務網
> (Ministry of the Interior open data). No live crawling is performed.
