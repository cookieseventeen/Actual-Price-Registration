// 框架元件：TopBar、導覽
import { useState } from 'react';
import type { Member } from '../../lib/auth';
import { STATUS_META, PLAN_META } from '../../lib/auth';

export type ViewId = 'search' | 'results' | 'analysis' | 'map' | 'mapDrill' | 'data' | 'admin';

export const NAV_ITEMS: { id: ViewId; label: string; icon: string }[] = [
  { id: 'search',   label: '搜尋',     icon: 'pi-search' },
  { id: 'results',  label: '成交紀錄', icon: 'pi-list' },
  { id: 'analysis', label: '行情分析', icon: 'pi-chart-bar' },
  { id: 'map',      label: '房價地圖', icon: 'pi-map' },
  { id: 'mapDrill', label: '地圖探索', icon: 'pi-compass' },
  { id: 'data',     label: '資料來源', icon: 'pi-database' },
  { id: 'admin',    label: '會員管理', icon: 'pi-users' },
];

// ── TopBar ──
export function TopBar({ onToggleLayout, dark, onToggleDark, lastSync, onExport, user, onLogin, onLogout, onNav }:
  { onToggleLayout: () => void; dark: boolean; onToggleDark: () => void; lastSync: string; onExport: () => void;
    user: Member | null; onLogin: () => void; onLogout: () => void; onNav: (v: ViewId) => void }) {
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
        <div className="topbar-divider" style={{ width: 1, height: 22, background: 'var(--ore-border)', margin: '0 4px' }}></div>
        <button className="btn btn-secondary btn-sm" onClick={onExport}><i className="pi pi-download"></i>匯出</button>
        <button className="icon-btn" title="切換導覽版面" onClick={onToggleLayout}><i className="pi pi-th-large"></i></button>
        <button className="icon-btn" title="深淺色" onClick={onToggleDark}><i className={`pi ${dark ? 'pi-sun' : 'pi-moon'}`}></i></button>
        <UserMenu user={user} onLogin={onLogin} onLogout={onLogout} onNav={onNav} />
      </div>
    </div>
  );
}

// ── 使用者選單（頭像）──
function UserMenu({ user, onLogin, onLogout, onNav }:
  { user: Member | null; onLogin: () => void; onLogout: () => void; onNav: (v: ViewId) => void }) {
  const [open, setOpen] = useState(false);
  const close = () => setOpen(false);

  if (!user) {
    return <button className="btn btn-primary btn-sm" onClick={onLogin}><i className="pi pi-sign-in"></i>登入 / 註冊</button>;
  }

  const st = STATUS_META[user.status];
  return (
    <div style={{ position: 'relative' }}>
      <button className="avatar" title={user.name} onClick={() => setOpen(o => !o)}
        style={{ border: 'none', cursor: 'pointer', position: 'relative' }}>
        {user.avatar}
        <span style={{ position: 'absolute', right: -1, bottom: -1, width: 9, height: 9, borderRadius: '50%',
                       background: st.color, border: '2px solid var(--ore-card-bg)' }}></span>
      </button>

      {open && <>
        <div style={{ position: 'fixed', inset: 0, zIndex: 90 }} onClick={close}></div>
        <div className="card" style={{ position: 'absolute', right: 0, top: 'calc(100% + 8px)', width: 240, zIndex: 100,
                                       boxShadow: 'var(--ore-shadow-xl)', overflow: 'hidden' }}>
          <div style={{ padding: '14px', borderBottom: '1px solid var(--ore-border)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <div className="avatar" style={{ width: 36, height: 36, flexShrink: 0 }}>{user.avatar}</div>
              <div style={{ minWidth: 0 }}>
                <div style={{ fontSize: 13.5, fontWeight: 700, color: 'var(--ore-fg)' }}>{user.name}</div>
                <div className="mono" style={{ fontSize: 11, color: 'var(--ore-fg-muted)', overflow: 'hidden', textOverflow: 'ellipsis' }}>{user.email}</div>
              </div>
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 10 }}>
              <span className="tag" style={{ background: st.bg, color: st.color }}><i className={`pi ${st.icon}`} style={{ fontSize: 10 }}></i>{st.label}</span>
              <span className="tag tag-source">{PLAN_META[user.plan].label}方案</span>
            </div>
          </div>
          <div style={{ padding: 6 }}>
            <MenuRow icon="pi-users" label="後台管理" onClick={() => { onNav('admin'); close(); }} />
            <MenuRow icon="pi-sign-out" label="登出" danger onClick={() => { onLogout(); close(); }} />
          </div>
        </div>
      </>}
    </div>
  );
}

function MenuRow({ icon, label, onClick, danger }: { icon: string; label: string; onClick: () => void; danger?: boolean }) {
  return (
    <button onClick={onClick}
      style={{ display: 'flex', alignItems: 'center', gap: 10, width: '100%', padding: '9px 10px', borderRadius: 8,
               border: 'none', background: 'transparent', cursor: 'pointer', fontFamily: 'inherit', fontSize: 13, fontWeight: 600,
               color: danger ? 'var(--ore-danger)' : 'var(--ore-fg)' }}
      onMouseEnter={e => { e.currentTarget.style.background = 'var(--ore-surface-100)'; }}
      onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; }}>
      <i className={`pi ${icon}`} style={{ fontSize: 14, width: 16 }}></i>{label}
    </button>
  );
}

// ── 側欄導覽 ──
export function Sidebar({ view, onNav, rail }:
  { view: ViewId; onNav: (v: ViewId) => void; rail: boolean }) {
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
      <div className="nav-item" title="設定">
        <i className="pi pi-cog"></i><span className="nav-label">設定</span>
      </div>
      {!rail && (
        <div className="sidebar-promo" style={{ padding: '12px', marginTop: 8, borderRadius: 10, background: 'var(--brand-50)', border: '1px solid var(--ore-border)' }}>
          <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--brand-600)', marginBottom: 4 }}>本月已收錄</div>
          <div className="mono" style={{ fontSize: 20, fontWeight: 800, color: 'var(--ore-fg)' }}>9,234</div>
          <div style={{ fontSize: 10.5, color: 'var(--ore-fg-muted)' }}>筆成交紀錄</div>
        </div>
      )}
    </nav>
  );
}

// ── 頂部分頁導覽 ──
export function TopNav({ view, onNav }: { view: ViewId; onNav: (v: ViewId) => void }) {
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
