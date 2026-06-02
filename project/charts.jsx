// 圖表元件 — 純 SVG，吃 var(--brand) 主色，支援 chartStyle tweak
// chartStyle: 'area' | 'line' | 'bar-rounded' | 'bar-flat'（趨勢圖用 area/line；長條圖用圓角/平頭）

function niceMax(v) {
  const pow = Math.pow(10, Math.floor(Math.log10(v)));
  const n = v / pow;
  const step = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
  return step * pow;
}

// 折線 / 面積趨勢圖
function TrendChart({ data, labels, height = 240, mode = 'area', showGrid = true, unit = '', valueFmt }) {
  const [hover, setHover] = _useState(null);
  const W = 760, H = height, padL = 46, padR = 16, padT = 16, padB = 30;
  const innerW = W - padL - padR, innerH = H - padT - padB;
  const max = niceMax(Math.max(...data) * 1.05);
  const min = Math.min(...data);
  const lo = Math.max(0, niceMax(min * 0.85) - (max - min > 30 ? 0 : 0));
  const yMin = min > max * 0.4 ? Math.floor((min * 0.9) / 5) * 5 : 0;
  const range = max - yMin || 1;
  const x = i => padL + (innerW * i) / (data.length - 1);
  const y = v => padT + innerH - ((v - yMin) / range) * innerH;
  const pts = data.map((v, i) => [x(i), y(v)]);
  const linePath = pts.map((p, i) => (i === 0 ? 'M' : 'L') + p[0].toFixed(1) + ' ' + p[1].toFixed(1)).join(' ');
  const areaPath = linePath + ` L${x(data.length - 1).toFixed(1)} ${(padT + innerH).toFixed(1)} L${padL} ${(padT + innerH).toFixed(1)} Z`;
  const ticks = 4;
  const fmt = valueFmt || (v => v.toLocaleString());

  return (
    <svg viewBox={`0 0 ${W} ${H}`} width="100%" style={{ display: 'block', overflow: 'visible' }}
         onMouseLeave={() => setHover(null)}>
      <defs>
        <linearGradient id="trendFill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="var(--brand)" stopOpacity="0.26" />
          <stop offset="100%" stopColor="var(--brand)" stopOpacity="0.02" />
        </linearGradient>
      </defs>
      {/* y 格線 */}
      {showGrid && Array.from({ length: ticks + 1 }).map((_, i) => {
        const v = yMin + (range * i) / ticks;
        const yy = y(v);
        return (
          <g key={i}>
            <line x1={padL} y1={yy} x2={W - padR} y2={yy} stroke="var(--ore-border)" strokeWidth="1" strokeDasharray={i === 0 ? '' : '3 4'} opacity={i === 0 ? 0.9 : 0.5} />
            <text x={padL - 8} y={yy + 4} textAnchor="end" fontSize="10.5" fill="var(--ore-fg-muted)" className="mono">{fmt(Math.round(v))}</text>
          </g>
        );
      })}
      {mode === 'area' && <path d={areaPath} fill="url(#trendFill)" />}
      <path d={linePath} fill="none" stroke="var(--brand)" strokeWidth="2.5" strokeLinejoin="round" strokeLinecap="round" />
      {/* x 標籤 */}
      {labels.map((l, i) => (
        (i % Math.ceil(labels.length / 8) === 0 || i === labels.length - 1) &&
        <text key={i} x={x(i)} y={H - 10} textAnchor="middle" fontSize="10.5" fill="var(--ore-fg-muted)">{l}</text>
      ))}
      {/* 互動點 */}
      {pts.map((p, i) => (
        <g key={i}>
          <rect x={x(i) - innerW / data.length / 2} y={padT} width={innerW / data.length} height={innerH}
                fill="transparent" onMouseEnter={() => setHover(i)} />
          {hover === i && <>
            <line x1={p[0]} y1={padT} x2={p[0]} y2={padT + innerH} stroke="var(--brand)" strokeWidth="1" strokeDasharray="3 3" opacity="0.6" />
            <circle cx={p[0]} cy={p[1]} r="5" fill="var(--brand)" stroke="#fff" strokeWidth="2" />
          </>}
        </g>
      ))}
      {hover !== null && (() => {
        const p = pts[hover]; const tw = 96;
        const tx = Math.min(Math.max(p[0] - tw / 2, padL), W - padR - tw);
        return (
          <g transform={`translate(${tx}, ${Math.max(p[1] - 48, 4)})`}>
            <rect width={tw} height="38" rx="7" fill="var(--ore-fg)" opacity="0.94" />
            <text x={tw/2} y="15" textAnchor="middle" fontSize="10" fill="rgba(255,255,255,.65)">{labels[hover]}</text>
            <text x={tw/2} y="30" textAnchor="middle" fontSize="13" fontWeight="700" fill="#fff" className="mono">{fmt(data[hover])}{unit}</text>
          </g>
        );
      })()}
    </svg>
  );
}

// 長條圖（直方 / 分類）
function BarChart({ data, height = 240, rounded = true, unit = '', accentIndex }) {
  const [hover, setHover] = _useState(null);
  const W = 760, H = height, padL = 46, padR = 16, padT = 16, padB = 34;
  const innerW = W - padL - padR, innerH = H - padT - padB;
  const max = niceMax(Math.max(...data.map(d => d.value)));
  const n = data.length;
  const gap = 14;
  const bw = (innerW - gap * (n - 1)) / n;
  const ticks = 4;
  const rad = rounded ? Math.min(bw / 2, 7) : 0;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} width="100%" style={{ display: 'block', overflow: 'visible' }}>
      {Array.from({ length: ticks + 1 }).map((_, i) => {
        const v = (max * i) / ticks; const yy = padT + innerH - (v / max) * innerH;
        return (<g key={i}>
          <line x1={padL} y1={yy} x2={W - padR} y2={yy} stroke="var(--ore-border)" strokeDasharray={i === 0 ? '' : '3 4'} opacity={i === 0 ? 0.9 : 0.5} />
          <text x={padL - 8} y={yy + 4} textAnchor="end" fontSize="10.5" fill="var(--ore-fg-muted)" className="mono">{v.toLocaleString()}</text>
        </g>);
      })}
      {data.map((d, i) => {
        const h = (d.value / max) * innerH;
        const bx = padL + i * (bw + gap);
        const by = padT + innerH - h;
        const isAccent = accentIndex === i;
        const isHover = hover === i;
        return (
          <g key={i} onMouseEnter={() => setHover(i)} onMouseLeave={() => setHover(null)}>
            <rect x={bx} y={by} width={bw} height={h}
                  rx={rad} ry={rad}
                  fill={isAccent ? 'var(--brand-600)' : 'var(--brand)'}
                  opacity={isHover ? 1 : (isAccent ? 1 : 0.82)} style={{ transition: 'opacity 120ms' }} />
            <text x={bx + bw / 2} y={H - 18} textAnchor="middle" fontSize="10.5" fill="var(--ore-fg-muted)">{d.label}</text>
            {isHover && <text x={bx + bw / 2} y={by - 7} textAnchor="middle" fontSize="12" fontWeight="700" fill="var(--ore-fg)" className="mono">{d.value.toLocaleString()}{unit}</text>}
          </g>
        );
      })}
    </svg>
  );
}

// 水平長條（行政區排行）
function RankBars({ data, unit = '萬/坪', max: maxOverride }) {
  const max = maxOverride || niceMax(Math.max(...data.map(d => d.value)));
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'calc(11px * var(--density))' }}>
      {data.map((d, i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{ width: 64, fontSize: 12.5, fontWeight: 600, color: 'var(--ore-fg)', flexShrink: 0 }}>{d.label}</div>
          <div style={{ flex: 1, height: 22, background: 'var(--ore-surface-100)', borderRadius: 6, overflow: 'hidden', position: 'relative' }}>
            <div style={{ width: `${(d.value / max) * 100}%`, height: '100%', background: d.accent ? 'var(--brand-600)' : 'var(--brand)', borderRadius: 6, transition: 'width 500ms ease' }}></div>
          </div>
          <div className="mono" style={{ width: 70, textAlign: 'right', fontSize: 12.5, fontWeight: 700, color: 'var(--ore-fg)' }}>{d.value} <span style={{ fontSize: 10, fontWeight: 500, color: 'var(--ore-fg-muted)' }}>{unit}</span></div>
        </div>
      ))}
    </div>
  );
}

// 迷你 sparkline
function Sparkline({ data, width = 88, height = 28 }) {
  const max = Math.max(...data), min = Math.min(...data), range = max - min || 1;
  const x = i => (width * i) / (data.length - 1);
  const y = v => height - ((v - min) / range) * (height - 4) - 2;
  const path = data.map((v, i) => (i === 0 ? 'M' : 'L') + x(i).toFixed(1) + ' ' + y(v).toFixed(1)).join(' ');
  const up = data[data.length - 1] >= data[0];
  return (
    <svg width={width} height={height} style={{ display: 'block' }}>
      <path d={path} fill="none" stroke={up ? 'var(--ore-success)' : 'var(--ore-danger)'} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

Object.assign(window, { TrendChart, BarChart, RankBars, Sparkline });
