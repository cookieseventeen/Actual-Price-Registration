// 框架元件：格式化工具、TopBar、導覽

// ── 格式化 ──
function fmtTotal(wan) {
  if (wan >= 10000) return (wan / 10000).toFixed(2) + ' 億';
  return wan.toLocaleString() + ' 萬';
}
function fmtTotalShort(wan) {
  if (wan >= 10000) return (wan / 10000).toFixed(2) + '億';
  return wan.toLocaleString();
}

const NAV_ITEMS = [
  { id: 'search',   label: '搜尋',     icon: 'pi-search' },
  { id: 'results',  label: '成交紀錄', icon: 'pi-list' },
  { id: 'analysis', label: '行情分析', icon: 'pi-chart-bar' },
  { id: 'data',     label: '資料來源', icon: 'pi-database' },
];

// ── TopBar ──
function TopBar({ layout, onToggleLayout, dark, onToggleDark, lastSync, onExport, view }) {
  return (
    <div className="topbar">
      <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
        <div className="brand">
          <div className="brand-mark"><i className="pi pi-map-marker"></i></div>
          <span className="brand-name">實價通</span>
          <span className="brand-tag">台中市</span>
        </div>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
        <span className="status-pill"><span className="status-dot"></span>資料同步正常</span>
        <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }} className="sync-time">
          最後更新 <span className="mono">{lastSync}</span>
        </span>
        <div style={{ width: 1, height: 22, background: 'var(--ore-border)', margin: '0 4px' }}></div>
        <button className="btn btn-secondary btn-sm" onClick={onExport}><i className="pi pi-download"></i>匯出</button>
        <button className="icon-btn" title="切換導覽版面" onClick={onToggleLayout}><i className="pi pi-th-large"></i></button>
        <button className="icon-btn" title="深淺色" onClick={onToggleDark}><i className={`pi ${dark ? 'pi-sun' : 'pi-moon'}`}></i></button>
        <div className="avatar">陳</div>
      </div>
    </div>
  );
}

// ── 側欄導覽 ──
function Sidebar({ view, onNav, rail }) {
  return (
    <nav className={`sidebar ${rail ? 'rail' : ''}`}>
      {!rail && <div className="nav-section-label">查詢</div>}
      {NAV_ITEMS.map(it => (
        <div key={it.id} className={`nav-item ${view === it.id ? 'active' : ''}`}
             onClick={() => onNav(it.id)} title={it.label}>
          <i className={`pi ${it.icon}`}></i>
          <span className="nav-label">{it.label}</span>
        </div>
      ))}
      <div style={{ flex: 1 }}></div>
      <div className={`nav-item`} title="設定">
        <i className="pi pi-cog"></i><span className="nav-label">設定</span>
      </div>
      {!rail && (
        <div style={{ padding: '12px', marginTop: 8, borderRadius: 10, background: 'var(--brand-50)', border: '1px solid var(--ore-border)' }}>
          <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--brand-600)', marginBottom: 4 }}>本月已收錄</div>
          <div className="mono" style={{ fontSize: 20, fontWeight: 800, color: 'var(--ore-fg)' }}>9,234</div>
          <div style={{ fontSize: 10.5, color: 'var(--ore-fg-muted)' }}>筆成交紀錄</div>
        </div>
      )}
    </nav>
  );
}

// ── 頂部分頁導覽 ──
function TopNav({ view, onNav }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 var(--ore-space-6)', height: 46, background: 'var(--ore-card-bg)', borderBottom: '1px solid var(--ore-border)', flexShrink: 0 }}>
      <div className="topnav">
        {NAV_ITEMS.map(it => (
          <div key={it.id} className={`nav-item ${view === it.id ? 'active' : ''}`} onClick={() => onNav(it.id)}>
            <i className={`pi ${it.icon}`}></i><span>{it.label}</span>
          </div>
        ))}
      </div>
      <div className="nav-item"><i className="pi pi-cog"></i><span>設定</span></div>
    </div>
  );
}

Object.assign(window, { fmtTotal, fmtTotalShort, NAV_ITEMS, TopBar, Sidebar, TopNav });
