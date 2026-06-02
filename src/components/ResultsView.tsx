// 成交紀錄頁 — 卡片 / 表格切換、篩選、排序
import { TRANSACTIONS } from '../data/mock';
import type { Transaction } from '../data/mock';
import type { Filters, FilterGroup, SortState, ViewMode } from '../types';
import { filterTransactions, sortTransactions } from '../lib/query';
import { fmtTotalShort } from '../lib/format';

function ResultRowCard({ t, onOpen }: { t: Transaction; onOpen: (t: Transaction) => void }) {
  return (
    <div className="card card-hover" style={{ padding: 'var(--pad-4)' }} onClick={() => onOpen(t)}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8 }}>
        <div style={{ minWidth: 0 }}>
          <div style={{ fontSize: 15, fontWeight: 700, color: 'var(--ore-fg)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{t.community}</div>
          <div style={{ fontSize: 12, color: 'var(--ore-fg-muted)', marginTop: 2 }}>{t.district} · {t.section}</div>
        </div>
        {t.trade === '預售屋'
          ? <span className="tag tag-presale">預售</span>
          : <span className="tag tag-done">成屋</span>}
      </div>
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 6, margin: '12px 0 2px' }}>
        <span className="mono" style={{ fontSize: 24, fontWeight: 800, color: 'var(--ore-fg)', letterSpacing: '-.02em' }}>{fmtTotalShort(t.total)}</span>
        <span style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>{t.total >= 10000 ? '億' : '萬'}</span>
        <span style={{ marginLeft: 'auto', fontSize: 12.5, color: 'var(--brand-600)', fontWeight: 700 }} className="mono">{t.unit} 萬/坪</span>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 4, marginTop: 12, paddingTop: 12, borderTop: '1px solid var(--ore-surface-100)' }}>
        {([['坪數', t.ping], ['格局', t.layout], ['樓層', t.floor], ['屋齡', t.age + '年']] as const).map(([k, v]) => (
          <div key={k}>
            <div style={{ fontSize: 10, color: 'var(--ore-neutral-400)' }}>{k}</div>
            <div className="tnum" style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--ore-fg)', marginTop: 1 }}>{v}</div>
          </div>
        ))}
      </div>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 11, fontSize: 11, color: 'var(--ore-neutral-400)' }}>
        <span><i className="pi pi-calendar" style={{ fontSize: 10, marginRight: 4 }}></i>{t.date} 成交</span>
        <span className="tag tag-source" style={{ fontSize: 10 }}><i className="pi pi-database" style={{ fontSize: 9 }}></i>{t.source}</span>
      </div>
    </div>
  );
}

const COLUMNS: { key: keyof Transaction; label: string; align: 'left' | 'right' }[] = [
  { key: 'community', label: '社區 / 地址', align: 'left' },
  { key: 'district',  label: '行政區', align: 'left' },
  { key: 'total',     label: '總價(萬)', align: 'right' },
  { key: 'unit',      label: '單價(萬/坪)', align: 'right' },
  { key: 'ping',      label: '坪數', align: 'right' },
  { key: 'layout',    label: '格局', align: 'left' },
  { key: 'floor',     label: '樓層', align: 'left' },
  { key: 'age',       label: '屋齡', align: 'right' },
  { key: 'trade',     label: '類型', align: 'left' },
  { key: 'date',      label: '成交日', align: 'left' },
];

function ResultsTable({ rows, onOpen, sort, setSort }:
  { rows: Transaction[]; onOpen: (t: Transaction) => void; sort: SortState; setSort: (fn: (s: SortState) => SortState) => void }) {
  function clickSort(key: keyof Transaction) {
    setSort(s => s.key === key ? { key, dir: s.dir === 'asc' ? 'desc' : 'asc' } : { key, dir: 'desc' });
  }
  return (
    <div className="card tbl-wrap" style={{ overflow: 'auto', maxHeight: '100%' }}>
      <table className="tbl">
        <thead>
          <tr>
            {COLUMNS.map(c => (
              <th key={c.key} style={{ textAlign: c.align }} onClick={() => clickSort(c.key)}>
                {c.label}
                {sort.key === c.key && <i className={`pi ${sort.dir === 'asc' ? 'pi-sort-up' : 'pi-sort-down'}`} style={{ fontSize: 10, marginLeft: 4, color: 'var(--brand)' }}></i>}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map(t => (
            <tr key={t.id} onClick={() => onOpen(t)}>
              <td>
                <div style={{ fontWeight: 600, color: 'var(--ore-fg)' }}>{t.community}</div>
                <div style={{ fontSize: 11, color: 'var(--ore-fg-muted)' }}>{t.road} · {t.section}</div>
              </td>
              <td style={{ color: 'var(--ore-fg-muted)' }}>{t.district}</td>
              <td className="num" style={{ fontWeight: 700, color: 'var(--ore-fg)' }}>{t.total.toLocaleString()}</td>
              <td className="num" style={{ fontWeight: 700, color: 'var(--brand-600)' }}>{t.unit}</td>
              <td className="num">{t.ping}</td>
              <td>{t.layout}</td>
              <td style={{ color: 'var(--ore-fg-muted)' }}>{t.floor}</td>
              <td className="num">{t.age} 年</td>
              <td>{t.trade === '預售屋' ? <span className="tag tag-presale">預售</span> : <span className="tag tag-done">成屋</span>}</td>
              <td className="mono" style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>{t.date}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// viewMode 切換鈕（由 tweak 控制預設，使用者亦可在頁面切換）
function ViewToggle({ viewMode, setViewMode }: { viewMode: ViewMode; setViewMode: (m: ViewMode) => void }) {
  const opts: [ViewMode, string, string][] = [['card', 'pi-th-large', '卡片'], ['table', 'pi-table', '表格']];
  return (
    <div style={{ display: 'flex', border: '1px solid var(--ore-border)', borderRadius: 8, overflow: 'hidden', background: 'var(--ore-card-bg)' }}>
      {opts.map(([m, ic, lb]) => (
        <button key={m} onClick={() => setViewMode(m)}
          style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '7px 13px', border: 'none', cursor: 'pointer',
                   background: viewMode === m ? 'var(--brand)' : 'transparent', color: viewMode === m ? '#fff' : 'var(--ore-fg-muted)',
                   fontFamily: 'inherit', fontSize: 12.5, fontWeight: 600 }}>
          <i className={`pi ${ic}`} style={{ fontSize: 13 }}></i>{lb}
        </button>
      ))}
    </div>
  );
}

interface ResultsViewProps {
  district: string;
  setDistrict: (d: string) => void;
  query: string;
  filters: Filters;
  toggleFilter: (group: FilterGroup, val: string) => void;
  viewMode: ViewMode;
  setViewMode: (m: ViewMode) => void;
  sort: SortState;
  setSort: (fn: (s: SortState) => SortState) => void;
  onOpen: (t: Transaction) => void;
}

export function ResultsView({ district, setDistrict, query, filters, toggleFilter, viewMode, setViewMode, sort, setSort, onOpen }: ResultsViewProps) {
  const rows = sortTransactions(filterTransactions(TRANSACTIONS, { district, query, filters }), sort);

  const activeChips: { group: FilterGroup | '_district'; label: string }[] = [
    ...(district ? [{ group: '_district' as const, label: district }] : []),
    ...filters.type.map(v => ({ group: 'type' as const, label: v })),
    ...filters.layout.map(v => ({ group: 'layout' as const, label: v })),
    ...filters.trade.map(v => ({ group: 'trade' as const, label: v })),
  ];

  return (
    <div className="fade-up" style={{ display: 'flex', flexDirection: 'column', height: '100%', minHeight: 0 }}>
      <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', marginBottom: 14, flexWrap: 'wrap', gap: 12 }}>
        <div>
          <h1 className="page-title">{district || '全台中'} 成交紀錄</h1>
          <p className="page-sub">共 <strong style={{ color: 'var(--brand-600)' }}>{rows.length}</strong> 筆符合條件 · 資料來源 內政部不動產交易實價查詢服務網</p>
        </div>
        <ViewToggle viewMode={viewMode} setViewMode={setViewMode} />
      </div>

      {/* 已套用篩選 */}
      {activeChips.length > 0 && (
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 7, marginBottom: 14, alignItems: 'center' }}>
          {activeChips.map((c, i) => (
            <span key={i} className="chip on" style={{ cursor: 'default' }}>
              {c.label}
              <i className="pi pi-times" style={{ fontSize: 10, cursor: 'pointer' }}
                 onClick={() => c.group === '_district' ? setDistrict('') : toggleFilter(c.group, c.label)}></i>
            </span>
          ))}
        </div>
      )}

      <div style={{ flex: 1, minHeight: 0 }}>
        {viewMode === 'table'
          ? <ResultsTable rows={rows} onOpen={onOpen} sort={sort} setSort={setSort} />
          : (
            <div style={{ overflowY: 'auto', height: '100%' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: 14, paddingBottom: 8 }}>
                {rows.map(t => <ResultRowCard key={t.id} t={t} onOpen={onOpen} />)}
              </div>
            </div>
          )}
      </div>
    </div>
  );
}
