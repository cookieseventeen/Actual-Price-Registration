// 地圖探索 — 下鑽層級輔助（純前端假資料）
import type { District, Transaction } from './mock';
import { TC_DISTRICTS, TRANSACTIONS } from './mock';

export const TAICHUNG_COUNTY_ID = '台中市';

export function countyHasDistrictData(countyId: string): boolean {
  return countyId === TAICHUNG_COUNTY_ID;
}

export function getDistrictsForCounty(countyId: string): District[] | null {
  if (countyId === TAICHUNG_COUNTY_ID) return TC_DISTRICTS;
  return null;
}

/** 台中區級 tile cartogram 格線（4×3，避免擁擠） */
export const DRILL_GRID = { cols: 4, rows: 3, cellW: 136, cellH: 142, pad: 14, gap: 10 };

export const DRILL_VIEWBOX = {
  w:
    DRILL_GRID.pad * 2 +
    DRILL_GRID.cols * DRILL_GRID.cellW +
    (DRILL_GRID.cols - 1) * DRILL_GRID.gap,
  h:
    DRILL_GRID.pad * 2 +
    DRILL_GRID.rows * DRILL_GRID.cellH +
    (DRILL_GRID.rows - 1) * DRILL_GRID.gap,
};

export function tileOrigin(col: number, row: number): { x: number; y: number } {
  const { pad, cellW, cellH, gap } = DRILL_GRID;
  return { x: pad + col * (cellW + gap), y: pad + row * (cellH + gap) };
}

export const TAICHUNG_TILE_ORDER: { id: string; col: number; row: number }[] = [
  { id: 'xitun', col: 0, row: 0 },
  { id: 'beitun', col: 1, row: 0 },
  { id: 'nantun', col: 2, row: 0 },
  { id: 'west', col: 3, row: 0 },
  { id: 'north', col: 0, row: 1 },
  { id: 'east', col: 1, row: 1 },
  { id: 'south', col: 2, row: 1 },
  { id: 'central', col: 3, row: 1 },
  { id: 'dali', col: 0, row: 2 },
  { id: 'taiping', col: 1, row: 2 },
  { id: 'fengyuan', col: 2, row: 2 },
  { id: 'wuri', col: 3, row: 2 },
];

/** 依行政區篩選成交；不足時以均價為錨補假資料 */
export function dealsForDistrict(districtName: string, anchorAvg: number): Transaction[] {
  const real = TRANSACTIONS.filter(t => t.district === districtName);
  if (real.length >= 5) return real;

  const samples: { ping: number; unitMul: number; community: string }[] = [
    { ping: 22, unitMul: 0.82, community: '鄰近成屋 A' },
    { ping: 38, unitMul: 0.92, community: '鄰近成屋 B' },
    { ping: 52, unitMul: 1.0, community: '鄰近成屋 C' },
    { ping: 68, unitMul: 1.08, community: '鄰近成屋 D' },
    { ping: 85, unitMul: 1.15, community: '鄰近成屋 E' },
    { ping: 105, unitMul: 1.22, community: '鄰近成屋 F' },
    { ping: 42, unitMul: 0.88, community: '鄰近成屋 G' },
    { ping: 58, unitMul: 1.05, community: '鄰近成屋 H' },
  ];

  const extras = samples.slice(0, Math.max(0, 6 - real.length)).map((s, i) => ({
    id: `M-${districtName}-${i}`,
    community: s.community,
    district: districtName,
    road: '示範路',
    section: '鄰近樣本',
    type: '住宅大樓',
    total: Math.round(s.ping * s.unitMul * anchorAvg),
    unit: Math.round(s.unitMul * anchorAvg * 10) / 10,
    ping: s.ping,
    floor: '8/15',
    age: 5,
    layout: '3房2廳',
    rooms: 3,
    trade: '成屋',
    date: '2026-05-01',
    source: '模擬',
    crawled: '2026-05-28',
    lat: 24.15,
    lng: 120.65,
    parking: '坡道平面',
  }));

  return [...real, ...extras];
}
