// 價格地圖頁 — 台灣縣市 3D 柱狀房價地圖（純 SVG，吃 var(--brand)）
// 柱越高 = 成交均價越高；縣市底圖依均價 choropleth 著色
import { useState } from 'react';
import { TW_COUNTIES, TW_OUTLYING, TW_VIEWBOX } from '../data/taiwanGeo';
import type { CountyDatum } from '../data/taiwanGeo';

interface MapViewProps {
  barRounded: boolean;   // 柱頂圓角
  showGrid: boolean;     // 縣市底圖 choropleth 著色開關
}

const MAX_BAR = 168;   // 最高柱（px，對應全國最高均價）
const BW = 10;         // 柱寬
const DEPTH = 4.5;     // 3D 立體深度

export function MapView({ barRounded, showGrid }: MapViewProps) {
  const [hover, setHover] = useState<string | null>(null);
  const [pinned, setPinned] = useState<string | null>(null);
  const active = hover ?? pinned;

  const counties = TW_COUNTIES;
  const maxAvg = Math.max(...counties.map(c => c.avg));
  const minAvg = Math.min(...[...counties, ...TW_OUTLYING].map(c => c.avg));
  const top = counties.find(c => c.avg === maxAvg)!;
  const all = [...counties, ...TW_OUTLYING];
  const nationalAvg = all.reduce((s, c) => s + c.avg, 0) / all.length;
  const totalVol = all.reduce((s, c) => s + c.vol, 0);

  // 北→南（cy 由小到大）作畫家排序：南部柱體疊在前
  const drawOrder = [...counties].sort((a, b) => a.cy - b.cy);

  const tint = (avg: number) => {
    const r = (avg - minAvg) / (maxAvg - minAvg);
    return `color-mix(in srgb, var(--brand) ${Math.round(8 + r * 56)}%, transparent)`;
  };
  const barH = (avg: number) => (avg / maxAvg) * MAX_BAR;

  // 立體柱 path 群
  function Pillar({ c, on }: { c: CountyDatum; on: boolean }) {
    const h = barH(c.avg);
    const x = c.cx - BW / 2, yTop = c.cy - h;
    const rad = barRounded ? Math.min(BW / 2, 4) : 0;
    return (
      <g style={{ transition: 'opacity 120ms' }} opacity={active && !on ? 0.45 : 1}
         onMouseEnter={() => setHover(c.id)} onMouseLeave={() => setHover(null)}
         onClick={() => setPinned(p => (p === c.id ? null : c.id))} cursor="pointer">
        {/* 落地陰影 */}
        <ellipse cx={c.cx} cy={c.cy} rx={BW * 0.7} ry={BW * 0.32} fill="rgba(0,0,0,.18)" />
        {/* 右側面 */}
        <path d={`M${x + BW} ${c.cy} L${x + BW + DEPTH} ${c.cy - DEPTH} L${x + BW + DEPTH} ${yTop - DEPTH} L${x + BW} ${yTop} Z`}
              fill="var(--brand-600)" />
        {/* 頂面 */}
        <path d={`M${x} ${yTop} L${x + DEPTH} ${yTop - DEPTH} L${x + BW + DEPTH} ${yTop - DEPTH} L${x + BW} ${yTop} Z`}
              fill={`color-mix(in srgb, var(--brand) 60%, #fff)`} />
        {/* 正面 */}
        <rect x={x} y={yTop} width={BW} height={h} rx={rad} fill="url(#pillarFill)"
              stroke={on ? '#fff' : 'none'} strokeWidth={on ? 1.2 : 0} />
        {on && (
          <text x={c.cx} y={yTop - DEPTH - 6} textAnchor="middle" fontSize="11.5" fontWeight="800"
                fill="var(--ore-fg)" className="mono">{c.avg}</text>
        )}
      </g>
    );
  }

  const activeData = active ? all.find(c => c.id === active) : null;

  return (
    <div className="content-narrow fade-up">
      <div style={{ marginBottom: 16 }}>
        <h1 className="page-title">全台房價地圖</h1>
        <p className="page-sub">各縣市成交均價立體柱狀圖 · 柱高與單價成正比 · 更新於 2026-05-28</p>
      </div>

      {/* 概況列 */}
      <div style={{ display: 'flex', gap: 14, marginBottom: 16, flexWrap: 'wrap' }}>
        {([
          ['全國均價', nationalAvg.toFixed(1), '萬/坪'],
          ['最高 ' + top.name, top.avg.toFixed(1), '萬/坪'],
          ['縣市涵蓋', String(all.length), '個'],
          ['年成交量', totalVol.toLocaleString(), '筆'],
        ] as [string, string, string][]).map(([l, v, u], i) => (
          <div key={i} className="card card-pad" style={{ flex: 1, minWidth: 150 }}>
            <div className="stat-label" style={{ marginTop: 0, marginBottom: 6 }}>{l}</div>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 5 }}>
              <span className="mono stat-value">{v}</span>
              <span style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>{u}</span>
            </div>
          </div>
        ))}
      </div>

      <div className="map-grid">
        {/* 地圖 */}
        <div className="card card-pad">
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
            <h3 className="section-title">縣市成交均價</h3>
            <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>柱高 = 萬元 / 坪</span>
          </div>

          <svg viewBox={`0 0 ${TW_VIEWBOX.w} ${TW_VIEWBOX.h}`} width="100%"
               style={{ display: 'block', overflow: 'visible', maxHeight: 620 }}
               onMouseLeave={() => setHover(null)}>
            <defs>
              <linearGradient id="pillarFill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor={`color-mix(in srgb, var(--brand) 78%, #fff)`} />
                <stop offset="100%" stopColor="var(--brand)" />
              </linearGradient>
            </defs>

            {/* 縣市底圖 */}
            {counties.map(c => {
              const on = active === c.id;
              return (
                <path key={c.id} d={c.path}
                      fill={showGrid ? tint(c.avg) : 'var(--ore-surface-100)'}
                      stroke={on ? 'var(--brand-600)' : 'var(--ore-border)'}
                      strokeWidth={on ? 1.6 : 0.8}
                      style={{ transition: 'fill 120ms, stroke 120ms' }}
                      onMouseEnter={() => setHover(c.id)} onMouseLeave={() => setHover(null)}
                      onClick={() => setPinned(p => (p === c.id ? null : c.id))} cursor="pointer" />
              );
            })}

            {/* 立體柱（北→南排序） */}
            {drawOrder.map(c => <Pillar key={c.id} c={c} on={active === c.id} />)}

            {/* tooltip */}
            {activeData && counties.some(c => c.id === active) && (() => {
              const c = counties.find(d => d.id === active)!;
              const tw = 124, th = 56;
              const tx = Math.min(Math.max(c.cx - tw / 2, 4), TW_VIEWBOX.w - tw - 4);
              const ty = Math.max(c.cy - barH(c.avg) - th - 22, 4);
              const up = c.change >= 0;
              return (
                <g transform={`translate(${tx}, ${ty})`} pointerEvents="none">
                  <rect width={tw} height={th} rx="8" fill="var(--ore-fg)" opacity="0.95" />
                  <text x="11" y="20" fontSize="12.5" fontWeight="700" fill="#fff">{c.name}</text>
                  <text x="11" y="38" fontSize="15" fontWeight="800" fill="#fff" className="mono">{c.avg}
                    <tspan fontSize="10" fontWeight="500" fill="rgba(255,255,255,.7)"> 萬/坪</tspan></text>
                  <text x="11" y="51" fontSize="10" fill="rgba(255,255,255,.7)">
                    年增 <tspan fill={up ? '#4ade80' : '#f87171'} fontWeight="700">{up ? '+' : ''}{c.change}%</tspan>
                    <tspan dx="6">{c.vol.toLocaleString()} 筆</tspan></text>
                </g>
              );
            })()}
          </svg>

          {/* 色階圖例 */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginTop: 6 }}>
            <span style={{ fontSize: 11, color: 'var(--ore-fg-muted)' }} className="mono">{minAvg}</span>
            <div style={{ flex: 1, height: 8, borderRadius: 4,
              background: 'linear-gradient(90deg, color-mix(in srgb, var(--brand) 12%, transparent), var(--brand))' }} />
            <span style={{ fontSize: 11, color: 'var(--ore-fg-muted)' }} className="mono">{maxAvg}</span>
            <span style={{ fontSize: 11, color: 'var(--ore-fg-muted)' }}>萬/坪</span>
          </div>
        </div>

        {/* 右欄：排行 + 外島 */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div className="card card-pad">
            <h3 className="section-title" style={{ marginBottom: 4 }}>縣市均價排行</h3>
            <p style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 14 }}>成交均價 萬元 / 坪 · 點選與地圖連動</p>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 'calc(8px * var(--density))' }}>
              {[...all].sort((a, b) => b.avg - a.avg).map((c, i) => {
                const on = active === c.id;
                return (
                  <div key={c.id} onMouseEnter={() => setHover(c.id)} onMouseLeave={() => setHover(null)}
                       onClick={() => setPinned(p => (p === c.id ? null : c.id))}
                       style={{ display: 'flex', alignItems: 'center', gap: 9, cursor: 'pointer',
                         padding: '3px 6px', borderRadius: 7, background: on ? 'var(--brand-50)' : 'transparent',
                         transition: 'background 120ms' }}>
                    <span className="mono" style={{ width: 18, fontSize: 11, color: 'var(--ore-fg-muted)', textAlign: 'right' }}>{i + 1}</span>
                    <span style={{ width: 52, fontSize: 12.5, fontWeight: 600, color: 'var(--ore-fg)', flexShrink: 0 }}>{c.name}</span>
                    <div style={{ flex: 1, height: 16, background: 'var(--ore-surface-100)', borderRadius: 5, overflow: 'hidden' }}>
                      <div style={{ width: `${(c.avg / maxAvg) * 100}%`, height: '100%',
                        background: on ? 'var(--brand-600)' : 'var(--brand)', borderRadius: 5, transition: 'width 400ms ease' }} />
                    </div>
                    <span className="mono" style={{ width: 42, textAlign: 'right', fontSize: 12.5, fontWeight: 700, color: 'var(--ore-fg)' }}>{c.avg}</span>
                  </div>
                );
              })}
            </div>
          </div>

          {/* 外島（不在主地圖） */}
          <div className="card card-pad">
            <h3 className="section-title" style={{ marginBottom: 4 }}>外島縣市</h3>
            <p style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 12 }}>金門 · 連江（馬祖）</p>
            <div style={{ display: 'flex', gap: 12 }}>
              {TW_OUTLYING.map(c => {
                const on = active === c.id;
                const up = c.change >= 0;
                return (
                  <div key={c.id} onMouseEnter={() => setHover(c.id)} onMouseLeave={() => setHover(null)}
                       className="card card-pad" style={{ flex: 1, textAlign: 'center', cursor: 'pointer',
                         borderColor: on ? 'var(--brand)' : undefined, background: on ? 'var(--brand-50)' : undefined }}>
                    <div style={{ fontSize: 12.5, fontWeight: 700, color: 'var(--ore-fg)', marginBottom: 4 }}>{c.name}</div>
                    <div className="mono" style={{ fontSize: 20, fontWeight: 800, color: 'var(--brand-600)' }}>{c.avg}</div>
                    <div style={{ fontSize: 10.5, color: 'var(--ore-fg-muted)' }}>萬/坪 · <span style={{ color: up ? 'var(--ore-success)' : 'var(--ore-danger)' }}>{up ? '+' : ''}{c.change}%</span></div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
