// 地圖探索 — 可下鑽房價地圖（L0 縣市 → L1 區 → L2 成交散佈，純假資料）
import { useMemo, useState } from 'react';
import { TW_COUNTIES, TW_VIEWBOX } from '../data/taiwanGeo';
import type { CountyDatum } from '../data/taiwanGeo';
import type { District, Transaction } from '../data/mock';
import {
  countyHasDistrictData,
  dealsForDistrict,
  DRILL_VIEWBOX,
  getDistrictsForCounty,
  TAICHUNG_COUNTY_ID,
  TAICHUNG_TILE_ORDER,
  tileOrigin,
} from '../data/mapDrill';

interface MapDrillViewProps {
  barRounded: boolean;
  showGrid: boolean;
}

type Level = 0 | 1 | 2;

const MAX_BAR_COUNTY = 148;
const MAX_BAR_DISTRICT = 100;
const BW_COUNTY = 10;
const BW_DISTRICT = 8;
const DEPTH = 4.5;

export function MapDrillView({ barRounded, showGrid }: MapDrillViewProps) {
  const [level, setLevel] = useState<Level>(0);
  const [county, setCounty] = useState<CountyDatum | null>(null);
  const [district, setDistrict] = useState<District | null>(null);
  const [hover, setHover] = useState<string | null>(null);

  const counties = TW_COUNTIES;
  const maxCountyAvg = Math.max(...counties.map(c => c.avg));
  const minCountyAvg = Math.min(...counties.map(c => c.avg));
  const drawOrder = [...counties].sort((a, b) => a.cy - b.cy);

  const districts = county ? getDistrictsForCounty(county.id) : null;
  const maxDistAvg = districts ? Math.max(...districts.map(d => d.avg)) : 1;
  const minDistAvg = districts ? Math.min(...districts.map(d => d.avg)) : 1;

  const deals = useMemo(() => {
    if (!district) return [];
    return dealsForDistrict(district.name, district.avg);
  }, [district]);

  const tintCounty = (avg: number) => {
    const r = (avg - minCountyAvg) / (maxCountyAvg - minCountyAvg);
    return `color-mix(in srgb, var(--brand) ${Math.round(8 + r * 56)}%, transparent)`;
  };
  const tintDistrict = (avg: number) => {
    const r = (avg - minDistAvg) / (maxDistAvg - minDistAvg);
    return `color-mix(in srgb, var(--brand) ${Math.round(12 + r * 52)}%, transparent)`;
  };
  const barHCounty = (avg: number) => (avg / maxCountyAvg) * MAX_BAR_COUNTY;

  function drillCounty(c: CountyDatum) {
    setCounty(c);
    setDistrict(null);
    setLevel(1);
    setHover(null);
  }
  function drillDistrict(d: District) {
    setDistrict(d);
    setLevel(2);
    setHover(null);
  }
  function goLevel(target: Level) {
    if (target === 0) {
      setLevel(0);
      setCounty(null);
      setDistrict(null);
    } else if (target === 1) {
      setLevel(1);
      setDistrict(null);
    }
    setHover(null);
  }

  function Pillar({
    cx,
    cy,
    avg,
    maxBar,
    bw,
    id,
    onClick,
    active,
  }: {
    cx: number;
    cy: number;
    avg: number;
    maxBar: number;
    bw: number;
    id: string;
    onClick?: () => void;
    active: boolean;
  }) {
    const maxAvg = level === 0 ? maxCountyAvg : maxDistAvg;
    const h = (avg / maxAvg) * maxBar;
    const x = cx - bw / 2;
    const yTop = cy - h;
    const rad = barRounded ? Math.min(bw / 2, 4) : 0;
    const on = active;
    return (
      <g
        opacity={hover && hover !== id ? 0.5 : 1}
        style={{ transition: 'opacity 120ms' }}
        onMouseEnter={() => setHover(id)}
        onMouseLeave={() => setHover(null)}
        onClick={onClick}
        cursor={onClick ? 'pointer' : 'default'}
      >
        <ellipse cx={cx} cy={cy} rx={bw * 0.65} ry={bw * 0.3} fill="rgba(0,0,0,.16)" />
        <path
          d={`M${x + bw} ${cy} L${x + bw + DEPTH} ${cy - DEPTH} L${x + bw + DEPTH} ${yTop - DEPTH} L${x + bw} ${yTop} Z`}
          fill="var(--brand-600)"
        />
        <path
          d={`M${x} ${yTop} L${x + DEPTH} ${yTop - DEPTH} L${x + bw + DEPTH} ${yTop - DEPTH} L${x + bw} ${yTop} Z`}
          fill={`color-mix(in srgb, var(--brand) 60%, #fff)`}
        />
        <rect
          x={x}
          y={yTop}
          width={bw}
          height={h}
          rx={rad}
          fill="url(#drillPillarFill)"
          stroke={on ? '#fff' : 'none'}
          strokeWidth={on ? 1.2 : 0}
        />
        {on && (
          <text
            x={cx}
            y={yTop - DEPTH - 5}
            textAnchor="middle"
            fontSize="10.5"
            fontWeight="800"
            fill="var(--ore-fg)"
            className="mono"
          >
            {avg}
          </text>
        )}
      </g>
    );
  }

  const breadcrumb = (
    <nav className="drill-crumb" aria-label="地圖層級">
      <button type="button" className="drill-crumb-btn" onClick={() => goLevel(0)}>
        全台
      </button>
      {county && (
        <>
          <span className="drill-crumb-sep">›</span>
          <button
            type="button"
            className={`drill-crumb-btn ${level === 1 ? 'current' : ''}`}
            onClick={() => goLevel(1)}
            disabled={level === 1}
          >
            {county.name}
          </button>
        </>
      )}
      {district && (
        <>
          <span className="drill-crumb-sep">›</span>
          <span className="drill-crumb-current">{district.name}</span>
        </>
      )}
    </nav>
  );

  return (
    <div className="content-narrow fade-up">
      <div style={{ marginBottom: 12 }}>
        <h1 className="page-title">地圖探索</h1>
        <p className="page-sub">
          點擊下鑽：全台縣市 → 行政區均價 → 區內成交坪數與單價 · 示範資料
        </p>
      </div>

      {breadcrumb}

      {level === 0 && (
        <div className="map-grid">
          <div className="card card-pad">
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                marginBottom: 8,
              }}
            >
              <h3 className="section-title">全台縣市</h3>
              <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>
                點擊縣市下鑽
              </span>
            </div>
            <svg
              viewBox={`0 0 ${TW_VIEWBOX.w} ${TW_VIEWBOX.h}`}
              width="100%"
              style={{ display: 'block', overflow: 'visible', maxHeight: 580 }}
              onMouseLeave={() => setHover(null)}
            >
              <defs>
                <linearGradient id="drillPillarFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={`color-mix(in srgb, var(--brand) 78%, #fff)`} />
                  <stop offset="100%" stopColor="var(--brand)" />
                </linearGradient>
              </defs>
              {counties.map(c => (
                <path
                  key={c.id}
                  d={c.path}
                  fill={showGrid ? tintCounty(c.avg) : 'var(--ore-surface-100)'}
                  stroke={hover === c.id ? 'var(--brand-600)' : 'var(--ore-border)'}
                  strokeWidth={hover === c.id ? 1.6 : 0.8}
                  style={{ transition: 'fill 120ms, stroke 120ms' }}
                  onMouseEnter={() => setHover(c.id)}
                  onMouseLeave={() => setHover(null)}
                  onClick={() => drillCounty(c)}
                  cursor="pointer"
                />
              ))}
              {drawOrder.map(c => (
                <Pillar
                  key={c.id}
                  id={c.id}
                  cx={c.cx}
                  cy={c.cy}
                  avg={c.avg}
                  maxBar={MAX_BAR_COUNTY}
                  bw={BW_COUNTY}
                  active={hover === c.id}
                  onClick={() => drillCounty(c)}
                />
              ))}
              {hover && (() => {
                const c = counties.find(d => d.id === hover);
                if (!c) return null;
                const tw = 128;
                const th = 58;
                const tx = Math.min(Math.max(c.cx - tw / 2, 4), TW_VIEWBOX.w - tw - 4);
                const ty = Math.max(c.cy - barHCounty(c.avg) - th - 20, 4);
                const hint = countyHasDistrictData(c.id) ? '點擊下鑽' : '區級資料準備中';
                return (
                  <g transform={`translate(${tx}, ${ty})`} pointerEvents="none">
                    <rect width={tw} height={th} rx="8" fill="var(--ore-fg)" opacity="0.95" />
                    <text x="11" y="20" fontSize="12.5" fontWeight="700" fill="#fff">
                      {c.name}
                    </text>
                    <text x="11" y="38" fontSize="14" fontWeight="800" fill="#fff" className="mono">
                      {c.avg}
                      <tspan fontSize="10" fontWeight="500" fill="rgba(255,255,255,.7)">
                        {' '}
                        萬/坪
                      </tspan>
                    </text>
                    <text x="11" y="52" fontSize="10" fill="rgba(255,255,255,.65)">
                      {hint}
                    </text>
                  </g>
                );
              })()}
            </svg>
          </div>
          <DrillHintPanel
            title="操作說明"
            lines={[
              '從全台 22 縣市出發，點選進入下一層。',
              '台中市提供完整 12 行政區均價格網。',
              '進入最小行政區後，可檢視坪數 × 單價散佈。',
            ]}
            highlight={TAICHUNG_COUNTY_ID}
          />
        </div>
      )}

      {level === 1 && county && (
        <div className="map-grid">
          <div className="card card-pad">
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                marginBottom: 8,
                flexWrap: 'wrap',
                gap: 8,
              }}
            >
              <h3 className="section-title">{county.name} · 行政區均價</h3>
              <button type="button" className="btn btn-secondary btn-sm" onClick={() => goLevel(0)}>
                <i className="pi pi-arrow-left" /> 返回全台
              </button>
            </div>

            {!districts ? (
              <div className="drill-empty">
                <i className="pi pi-hourglass" style={{ fontSize: 28, color: 'var(--brand)' }} />
                <p>此縣市區級資料準備中</p>
                <p style={{ fontSize: 12.5, color: 'var(--ore-fg-muted)' }}>
                  可先返回全台，點選台中市體驗完整下鑽
                </p>
              </div>
            ) : (
              <svg
                viewBox={`0 0 ${DRILL_VIEWBOX.w} ${DRILL_VIEWBOX.h}`}
                width="100%"
                style={{ display: 'block', maxHeight: 520 }}
                onMouseLeave={() => setHover(null)}
              >
                <defs>
                  <linearGradient id="drillPillarFill" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor={`color-mix(in srgb, var(--brand) 78%, #fff)`} />
                    <stop offset="100%" stopColor="var(--brand)" />
                  </linearGradient>
                </defs>
                {TAICHUNG_TILE_ORDER.map(({ id, col, row }) => {
                  const d = districts.find(x => x.id === id)!;
                  const { x, y } = tileOrigin(col, row);
                  const { cellW, cellH } = { cellW: 136, cellH: 142 };
                  const cx = x + cellW / 2;
                  const cy = y + cellH - 18;
                  const on = hover === d.id;
                  return (
                    <g key={d.id}>
                      <rect
                        x={x}
                        y={y}
                        width={cellW}
                        height={cellH}
                        rx={10}
                        fill={showGrid ? tintDistrict(d.avg) : 'var(--ore-surface-100)'}
                        stroke={on ? 'var(--brand-600)' : 'var(--ore-border)'}
                        strokeWidth={on ? 1.8 : 1}
                        style={{ transition: 'fill 120ms, stroke 120ms' }}
                        onMouseEnter={() => setHover(d.id)}
                        onMouseLeave={() => setHover(null)}
                        onClick={() => drillDistrict(d)}
                        cursor="pointer"
                      />
                      <text
                        x={x + cellW / 2}
                        y={y + 18}
                        textAnchor="middle"
                        fontSize="12"
                        fontWeight="700"
                        fill="var(--ore-fg)"
                      >
                        {d.name.replace('區', '')}
                      </text>
                      <text
                        x={x + cellW / 2}
                        y={y + 32}
                        textAnchor="middle"
                        fontSize="9.5"
                        fill="var(--ore-fg-muted)"
                      >
                        {(() => { const z = (d.zone || '').split('/')[0].trim(); return z.length > 8 ? z.slice(0, 7) + '…' : z; })()}
                      </text>
                      <Pillar
                        id={d.id}
                        cx={cx}
                        cy={cy}
                        avg={d.avg}
                        maxBar={MAX_BAR_DISTRICT}
                        bw={BW_DISTRICT}
                        active={on}
                        onClick={() => drillDistrict(d)}
                      />
                    </g>
                  );
                })}
              </svg>
            )}
          </div>
          {districts && (
            <DistrictRankPanel
              districts={districts}
              maxAvg={maxDistAvg}
              hover={hover}
              setHover={setHover}
              onSelect={drillDistrict}
            />
          )}
        </div>
      )}

      {level === 2 && county && district && (
        <DealsLevel
          countyName={county.name}
          district={district}
          deals={deals}
          onBack={() => goLevel(1)}
        />
      )}
    </div>
  );
}

function DrillHintPanel({
  title,
  lines,
  highlight,
}: {
  title: string;
  lines: string[];
  highlight: string;
}) {
  return (
    <div className="card card-pad">
      <h3 className="section-title" style={{ marginBottom: 10 }}>{title}</h3>
      <ul className="drill-hint-list">
        {lines.map((t, i) => (
          <li key={i}>{t}</li>
        ))}
      </ul>
      <div
        style={{
          marginTop: 14,
          padding: '10px 12px',
          borderRadius: 8,
          background: 'var(--brand-50)',
          border: '1px solid var(--ore-border)',
          fontSize: 12.5,
        }}
      >
        <strong style={{ color: 'var(--brand-600)' }}>建議路徑：</strong>
        {highlight} → 西屯區 → 檢視成交散佈
      </div>
    </div>
  );
}

function DistrictRankPanel({
  districts,
  maxAvg,
  hover,
  setHover,
  onSelect,
}: {
  districts: District[];
  maxAvg: number;
  hover: string | null;
  setHover: (id: string | null) => void;
  onSelect: (d: District) => void;
}) {
  const sorted = [...districts].sort((a, b) => b.avg - a.avg);
  return (
    <div className="card card-pad">
      <h3 className="section-title" style={{ marginBottom: 4 }}>行政區均價排行</h3>
      <p style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 12 }}>
        點選進入成交散佈
      </p>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 'calc(7px * var(--density))' }}>
        {sorted.map((d, i) => {
          const on = hover === d.id;
          return (
            <div
              key={d.id}
              onMouseEnter={() => setHover(d.id)}
              onMouseLeave={() => setHover(null)}
              onClick={() => onSelect(d)}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                cursor: 'pointer',
                padding: '3px 6px',
                borderRadius: 7,
                background: on ? 'var(--brand-50)' : 'transparent',
              }}
            >
              <span
                className="mono"
                style={{ width: 16, fontSize: 11, color: 'var(--ore-fg-muted)', textAlign: 'right' }}
              >
                {i + 1}
              </span>
              <span style={{ width: 48, fontSize: 12, fontWeight: 600 }}>{d.name}</span>
              <div
                style={{
                  flex: 1,
                  height: 14,
                  background: 'var(--ore-surface-100)',
                  borderRadius: 4,
                  overflow: 'hidden',
                }}
              >
                <div
                  style={{
                    width: `${(d.avg / maxAvg) * 100}%`,
                    height: '100%',
                    background: on ? 'var(--brand-600)' : 'var(--brand)',
                    borderRadius: 4,
                  }}
                />
              </div>
              <span className="mono" style={{ width: 38, textAlign: 'right', fontSize: 12, fontWeight: 700 }}>
                {d.avg}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function DealsLevel({
  countyName,
  district,
  deals,
  onBack,
}: {
  countyName: string;
  district: District;
  deals: Transaction[];
  onBack: () => void;
}) {
  const [hoverId, setHoverId] = useState<string | null>(null);
  const pad = { l: 52, r: 20, t: 24, b: 44 };
  const plotW = 520;
  const plotH = 300;
  const w = pad.l + plotW + pad.r;
  const h = pad.t + plotH + pad.b;

  const maxPing = Math.max(...deals.map(d => d.ping), 80);
  const maxUnit = Math.max(...deals.map(d => d.unit), district.avg * 1.3);
  const minUnit = Math.min(...deals.map(d => d.unit), district.avg * 0.7);

  const sx = (ping: number) => pad.l + (ping / maxPing) * plotW;
  const sy = (unit: number) => pad.t + plotH - ((unit - minUnit * 0.9) / (maxUnit - minUnit * 0.9 + 0.01)) * plotH;

  const active = hoverId ? deals.find(d => d.id === hoverId) : null;

  return (
    <div className="drill-deals-layout">
      <div className="card card-pad">
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            marginBottom: 8,
            flexWrap: 'wrap',
            gap: 8,
          }}
        >
          <h3 className="section-title">
            {countyName} · {district.name} · 鄰近成交
          </h3>
          <button type="button" className="btn btn-secondary btn-sm" onClick={onBack}>
            <i className="pi pi-arrow-left" /> 返回行政區
          </button>
        </div>
        <p style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 10 }}>
          橫軸坪數 · 縱軸成交單價（萬/坪）· 共 {deals.length} 筆
        </p>
        <svg viewBox={`0 0 ${w} ${h}`} width="100%" style={{ display: 'block', maxHeight: 340 }}>
          {/* 格線 */}
          {[0.25, 0.5, 0.75, 1].map(f => (
            <line
              key={f}
              x1={pad.l}
              x2={pad.l + plotW}
              y1={pad.t + plotH * (1 - f)}
              y2={pad.t + plotH * (1 - f)}
              stroke="var(--ore-border)"
              strokeDasharray="4 4"
              opacity={0.6}
            />
          ))}
          <line
            x1={pad.l}
            x2={pad.l + plotW}
            y1={pad.t + plotH}
            y2={pad.t + plotH}
            stroke="var(--ore-border)"
          />
          <line x1={pad.l} x2={pad.l} y1={pad.t} y2={pad.t + plotH} stroke="var(--ore-border)" />
          <text x={pad.l + plotW / 2} y={h - 8} textAnchor="middle" fontSize="11" fill="var(--ore-fg-muted)">
            坪數（坪）
          </text>
          <text
            x={14}
            y={pad.t + plotH / 2}
            textAnchor="middle"
            fontSize="11"
            fill="var(--ore-fg-muted)"
            transform={`rotate(-90, 14, ${pad.t + plotH / 2})`}
          >
            單價（萬/坪）
          </text>
          <line
            x1={pad.l}
            x2={pad.l + plotW}
            y1={sy(district.avg)}
            y2={sy(district.avg)}
            stroke="var(--brand)"
            strokeDasharray="6 4"
            opacity={0.55}
          />
          <text x={pad.l + 4} y={sy(district.avg) - 4} fontSize="9.5" fill="var(--brand-600)">
            區均 {district.avg}
          </text>
          {deals.map(tx => {
            const on = hoverId === tx.id;
            return (
              <g
                key={tx.id}
                onMouseEnter={() => setHoverId(tx.id)}
                onMouseLeave={() => setHoverId(null)}
                cursor="pointer"
              >
                <circle
                  cx={sx(tx.ping)}
                  cy={sy(tx.unit)}
                  r={on ? 7 : 5}
                  fill={on ? 'var(--brand-600)' : 'var(--brand)'}
                  stroke="#fff"
                  strokeWidth={on ? 2 : 1}
                  opacity={hoverId && !on ? 0.45 : 0.92}
                />
              </g>
            );
          })}
          {active && (() => {
            const tx = active;
            const bx = Math.min(sx(tx.ping) + 10, w - 150);
            const by = Math.max(sy(tx.unit) - 58, 8);
            return (
              <g transform={`translate(${bx}, ${by})`} pointerEvents="none">
                <rect width={142} height={52} rx="7" fill="var(--ore-fg)" opacity="0.94" />
                <text x="9" y="18" fontSize="11" fontWeight="700" fill="#fff">
                  {tx.community.length > 12 ? tx.community.slice(0, 11) + '…' : tx.community}
                </text>
                <text x="9" y="34" fontSize="11" fill="rgba(255,255,255,.85)" className="mono">
                  {tx.ping} 坪 · {tx.unit} 萬/坪
                </text>
                <text x="9" y="46" fontSize="9.5" fill="rgba(255,255,255,.6)">
                  {tx.date}
                </text>
              </g>
            );
          })()}
        </svg>
      </div>

      <div className="card card-pad">
        <h3 className="section-title" style={{ marginBottom: 10 }}>成交明細</h3>
        <div className="tbl-wrap" style={{ maxHeight: 380, overflow: 'auto' }}>
          <table className="tbl">
            <thead>
              <tr>
                <th>社區</th>
                <th className="num">坪數</th>
                <th className="num">單價</th>
                <th className="num">總價</th>
              </tr>
            </thead>
            <tbody>
              {deals.map(tx => (
                <tr
                  key={tx.id}
                  onMouseEnter={() => setHoverId(tx.id)}
                  onMouseLeave={() => setHoverId(null)}
                  style={{ background: hoverId === tx.id ? 'var(--brand-50)' : undefined }}
                >
                  <td>{tx.community}</td>
                  <td className="num mono">{tx.ping}</td>
                  <td className="num mono">{tx.unit}</td>
                  <td className="num mono">{tx.total.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
