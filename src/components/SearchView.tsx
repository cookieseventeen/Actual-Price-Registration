// 搜尋首頁 — 區域選擇 + 條件查詢
import { BUILDING_TYPES, TRADE_TYPES, TC_DISTRICTS } from '../data/mock';
import type { Filters, FilterGroup } from '../types';

function StatTile({ value, label, delta, suffix }:
  { value: string; label: string; delta?: number; suffix?: string }) {
  return (
    <div className="card card-pad" style={{ flex: 1, minWidth: 150 }}>
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 6 }}>
        <span className="stat-value mono">{value}</span>
        {suffix && <span style={{ fontSize: 13, color: 'var(--ore-fg-muted)', fontWeight: 600 }}>{suffix}</span>}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 6 }}>
        <span className="stat-label" style={{ margin: 0 }}>{label}</span>
        {delta != null && (
          <span className={delta >= 0 ? 'delta-up' : 'delta-down'} style={{ fontSize: 12 }}>
            <i className={`pi ${delta >= 0 ? 'pi-arrow-up-right' : 'pi-arrow-down-right'}`} style={{ fontSize: 10 }}></i> {Math.abs(delta)}%
          </span>
        )}
      </div>
    </div>
  );
}

interface SearchViewProps {
  onSearch: (district?: string) => void;
  setDistrict: (d: string) => void;
  query: string;
  setQuery: (q: string) => void;
  filters: Filters;
  toggleFilter: (group: FilterGroup, val: string) => void;
}

export function SearchView({ onSearch, setDistrict, query, setQuery, filters, toggleFilter }: SearchViewProps) {
  const buildingTypes = BUILDING_TYPES;
  const layouts = ['2房2廳', '3房2廳', '4房2廳'];
  const trades = TRADE_TYPES;

  return (
    <div className="content-narrow fade-up">
      {/* Hero */}
      <div style={{ textAlign: 'center', padding: '8px 0 var(--pad-6)' }}>
        <h1 style={{ fontSize: 32, fontWeight: 900, letterSpacing: '-.03em', color: 'var(--ore-fg)', lineHeight: 1.15 }}>
          台中市實價登錄，<span style={{ color: 'var(--brand)' }}>一次查清</span>
        </h1>
        <p style={{ fontSize: 14.5, color: 'var(--ore-fg-muted)', marginTop: 10 }}>
          每日自動爬取內政部成交資料 · 涵蓋 16 行政區 · 收錄 124,860 筆紀錄
        </p>
      </div>

      {/* 搜尋列 */}
      <div className="card" style={{ padding: 'var(--pad-3)', display: 'flex', gap: 10, alignItems: 'center', boxShadow: 'var(--ore-shadow-md)' }}>
        <i className="pi pi-search" style={{ color: 'var(--ore-fg-muted)', fontSize: 17, paddingLeft: 8 }}></i>
        <input className="input" style={{ border: 'none', boxShadow: 'none', fontSize: 15.5, flex: 1 }}
               placeholder="搜尋社區、路段或地址，例如「七期 聯聚」、「市政路」"
               value={query} onChange={e => setQuery(e.target.value)}
               onKeyDown={e => e.key === 'Enter' && onSearch()} />
        <button className="btn btn-primary" style={{ padding: '11px 22px', fontSize: 14.5 }} onClick={() => onSearch()}>
          <i className="pi pi-search"></i>查詢
        </button>
      </div>

      {/* 篩選 chips */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 14 }}>
        <span style={{ fontSize: 12, fontWeight: 700, color: 'var(--ore-fg-muted)', alignSelf: 'center', marginRight: 2 }}>建物</span>
        {buildingTypes.slice(0, 4).map(t => (
          <span key={t} className={`chip ${filters.type.includes(t) ? 'on' : ''}`} onClick={() => toggleFilter('type', t)}>{t}</span>
        ))}
        <span style={{ width: 1, background: 'var(--ore-border)', margin: '2px 6px' }}></span>
        <span style={{ fontSize: 12, fontWeight: 700, color: 'var(--ore-fg-muted)', alignSelf: 'center', marginRight: 2 }}>格局</span>
        {layouts.map(t => (
          <span key={t} className={`chip ${filters.layout.includes(t) ? 'on' : ''}`} onClick={() => toggleFilter('layout', t)}>{t}</span>
        ))}
        <span style={{ width: 1, background: 'var(--ore-border)', margin: '2px 6px' }}></span>
        {trades.map(t => (
          <span key={t} className={`chip ${filters.trade.includes(t) ? 'on' : ''}`} onClick={() => toggleFilter('trade', t)}>{t}</span>
        ))}
      </div>

      {/* 市場概況 */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 14, marginTop: 'var(--pad-6)' }}>
        <StatTile value="34.8" suffix="萬/坪" label="全市成交均價" delta={3.4} />
        <StatTile value="9,234" suffix="筆" label="本月成交件數" delta={4.2} />
        <StatTile value="6.8" suffix="%" label="近一年漲幅" delta={6.8} />
        <StatTile value="38.5" suffix="天" label="平均成交週期" delta={-2.1} />
      </div>

      {/* 行政區選擇 */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', margin: 'var(--pad-6) 0 12px' }}>
        <h3 className="section-title">選擇行政區</h3>
        <span style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>點擊任一區查看成交紀錄</span>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: 12 }}>
        {TC_DISTRICTS.map(d => (
          <div key={d.id} className="card card-hover" style={{ padding: 'var(--pad-4)' }}
               onClick={() => { setDistrict(d.name); onSearch(d.name); }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span style={{ fontSize: 15, fontWeight: 700, color: 'var(--ore-fg)' }}>{d.name}</span>
              <span className={d.change >= 0 ? 'delta-up' : 'delta-down'} style={{ fontSize: 12 }}>
                <i className={`pi ${d.change >= 0 ? 'pi-caret-up' : 'pi-caret-down'}`} style={{ fontSize: 9 }}></i>{Math.abs(d.change)}%
              </span>
            </div>
            <div style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', margin: '3px 0 10px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{d.zone}</div>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 5 }}>
              <span className="mono" style={{ fontSize: 21, fontWeight: 800, color: 'var(--brand)' }}>{d.avg}</span>
              <span style={{ fontSize: 11, color: 'var(--ore-fg-muted)' }}>萬/坪</span>
            </div>
            <div style={{ fontSize: 10.5, color: 'var(--ore-neutral-400)', marginTop: 6 }}>近一年 {d.vol.toLocaleString()} 筆</div>
          </div>
        ))}
      </div>
    </div>
  );
}
