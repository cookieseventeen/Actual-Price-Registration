// 實價通 — App 主程式（路由 + Tweaks）
import { useState, useEffect } from 'react';
import { TRANSACTIONS } from './data/mock';
import type { Transaction } from './data/mock';
import type { Filters, FilterGroup, SortState, ViewMode } from './types';
import { filterTransactions } from './lib/query';
import { TopBar, Sidebar, TopNav } from './components/shell/Shell';
import type { ViewId } from './components/shell/Shell';
import { SearchView } from './components/SearchView';
import { ResultsView } from './components/ResultsView';
import { DetailView } from './components/DetailView';
import { AnalysisView } from './components/AnalysisView';
import { MapView } from './components/MapView';
import { MapDrillView } from './components/MapDrillView';
import { DataView, ExportModal } from './components/DataView';
import { AuthView } from './components/auth/AuthView';
import { AdminView } from './components/admin/AdminView';
import {
  seedMembers, getCurrentUser, setCurrentUser, signOut, STATUS_META,
} from './lib/auth';
import type { Member } from './lib/auth';
import {
  useTweaks, TweaksPanel, TweakSection,
  TweakSlider, TweakToggle, TweakRadio, TweakColor,
} from './components/tweaks/Tweaks';

interface TweakValues {
  brand: string;
  dark: boolean;
  fontSize: number;
  density: 'compact' | 'regular' | 'comfy';
  layout: 'sidebar' | 'topnav' | 'rail';
  dataView: ViewMode;
  chartMode: 'area' | 'line';
  barRounded: boolean;
  showGrid: boolean;
}

const TWEAK_DEFAULTS: TweakValues = {
  brand: '#38b2a0',
  dark: false,
  fontSize: 14,
  density: 'regular',
  layout: 'sidebar',
  dataView: 'card',
  chartMode: 'area',
  barRounded: true,
  showGrid: true,
};

const DENSITY_SCALE: Record<string, number> = { compact: 0.84, regular: 1, comfy: 1.16 };

export default function App() {
  const [t, setTweak] = useTweaks<TweakValues>(TWEAK_DEFAULTS);

  // 認證 / 會員（純前端，localStorage 持久化）
  const [currentUser, setCurrentUserState] = useState<Member | null>(() => { seedMembers(); return getCurrentUser(); });
  const [showAuth, setShowAuth] = useState(false);

  function handleAuthComplete(m: Member) { setCurrentUser(m.id); setCurrentUserState(m); setShowAuth(false); }
  function handleLogout() { signOut(); setCurrentUserState(null); if (view === 'admin') setView('search'); }
  function refreshCurrentUser() { setCurrentUserState(getCurrentUser()); }

  const [view, setView] = useState<ViewId>('search');
  const [detail, setDetail] = useState<Transaction | null>(null);
  const [district, setDistrict] = useState('');
  const [query, setQuery] = useState('');
  const [filters, setFilters] = useState<Filters>({ type: [], layout: [], trade: [] });
  const [sort, setSort] = useState<SortState>({ key: 'date', dir: 'desc' });
  const [viewMode, setViewMode] = useState<ViewMode>(t.dataView);
  const [showExport, setShowExport] = useState(false);

  // tweak 改變預設呈現方式時同步
  useEffect(() => { setViewMode(t.dataView); }, [t.dataView]);

  // 套用 tweak 至 :root
  useEffect(() => {
    const r = document.documentElement;
    r.style.setProperty('--brand', t.brand);
    r.style.setProperty('--density', String(DENSITY_SCALE[t.density] || 1));
    r.style.setProperty('--app-font-size', t.fontSize + 'px');
    r.setAttribute('data-theme', t.dark ? 'dark' : 'light');
  }, [t.brand, t.density, t.fontSize, t.dark]);

  function toggleFilter(group: FilterGroup, val: string) {
    setFilters(prev => {
      const arr = prev[group];
      return { ...prev, [group]: arr.includes(val) ? arr.filter(x => x !== val) : [...arr, val] };
    });
  }

  function runSearch(dist?: string) {
    if (typeof dist === 'string') setDistrict(dist);
    setDetail(null);
    setView('results');
  }
  function openDetail(tx: Transaction) { setDetail(tx); }
  function nav(v: ViewId) { setDetail(null); setView(v); }

  // 計算目前匯出筆數（與成交紀錄頁共用同一查詢邏輯）
  const exportCount = filterTransactions(TRANSACTIONS, { district, filters }).length;

  const rail = t.layout === 'rail';
  const navView: ViewId = detail ? 'results' : view;

  // 登入 / 註冊頁：全螢幕，不含 shell
  if (showAuth) {
    return <AuthView onComplete={handleAuthComplete} onCancel={() => setShowAuth(false)} />;
  }

  // 已登入但帳號待審核 → 顯示提示橫幅
  const pendingBanner = currentUser && currentUser.status === 'pending';

  return (
    <div className="app-shell">
      <TopBar
        dark={t.dark} onToggleDark={() => setTweak('dark', !t.dark)}
        onToggleLayout={() => setTweak('layout', t.layout === 'sidebar' ? 'topnav' : t.layout === 'topnav' ? 'rail' : 'sidebar')}
        lastSync="2026-05-28 03:40" onExport={() => setShowExport(true)}
        user={currentUser} onLogin={() => setShowAuth(true)} onLogout={handleLogout} onNav={nav}
      />

      {t.layout === 'topnav' && <TopNav view={navView} onNav={nav} />}

      <div className="app-body">
        {t.layout !== 'topnav' && <Sidebar view={navView} onNav={nav} rail={rail} />}

        <div className="main">
          <div className="content">
            {pendingBanner && (
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '11px 14px', marginBottom: 16, borderRadius: 'var(--ore-radius-md)',
                            background: STATUS_META.pending.bg, border: '1px solid color-mix(in srgb, var(--ore-warning) 40%, transparent)' }}>
                <i className="pi pi-clock" style={{ color: STATUS_META.pending.color }}></i>
                <span style={{ fontSize: 13, color: 'var(--ore-fg)' }}>
                  您的帳號 <strong>審核中</strong>，管理員確認使用資格後即可使用完整查詢與匯出功能。
                </span>
              </div>
            )}
            {detail ? (
              <DetailView t={detail} onBack={() => setDetail(null)} chartMode={t.chartMode} showGrid={t.showGrid} />
            ) : view === 'search' ? (
              <SearchView onSearch={runSearch} setDistrict={setDistrict}
                          query={query} setQuery={setQuery} filters={filters} toggleFilter={toggleFilter} />
            ) : view === 'results' ? (
              <ResultsView district={district} setDistrict={setDistrict} query={query} filters={filters}
                           toggleFilter={toggleFilter} viewMode={viewMode} setViewMode={setViewMode} sort={sort} setSort={setSort} onOpen={openDetail} />
            ) : view === 'analysis' ? (
              <AnalysisView chartMode={t.chartMode} showGrid={t.showGrid} barRounded={t.barRounded} />
            ) : view === 'map' ? (
              <MapView barRounded={t.barRounded} showGrid={t.showGrid} />
            ) : view === 'mapDrill' ? (
              <MapDrillView barRounded={t.barRounded} showGrid={t.showGrid} />
            ) : view === 'admin' ? (
              <AdminView currentUserId={currentUser?.id} onChange={refreshCurrentUser} />
            ) : (
              <DataView />
            )}
          </div>
        </div>
      </div>

      {showExport && <ExportModal onClose={() => setShowExport(false)} count={exportCount} />}

      <TweaksPanel>
        <TweakSection label="主題" />
        <TweakColor label="主題色" value={t.brand}
                    options={['#38b2a0', '#1e7a6e', '#2563eb', '#6d5ae0', '#0ea5a3', '#475569']}
                    onChange={v => setTweak('brand', v)} />
        <TweakToggle label="深色模式" value={t.dark} onChange={v => setTweak('dark', v)} />

        <TweakSection label="排版密度" />
        <TweakSlider label="字體大小" value={t.fontSize} min={12} max={17} step={1} unit="px"
                     onChange={v => setTweak('fontSize', v)} />
        <TweakRadio label="密度" value={t.density} options={['compact', 'regular', 'comfy']}
                    onChange={v => setTweak('density', v as TweakValues['density'])} />

        <TweakSection label="版面佈局" />
        <TweakRadio label="導覽樣式" value={t.layout} options={['sidebar', 'topnav', 'rail']}
                    onChange={v => setTweak('layout', v as TweakValues['layout'])} />
        <TweakRadio label="成交資料呈現" value={t.dataView} options={['card', 'table']}
                    onChange={v => setTweak('dataView', v as ViewMode)} />

        <TweakSection label="圖表樣式" />
        <TweakRadio label="趨勢圖" value={t.chartMode} options={['area', 'line']}
                    onChange={v => setTweak('chartMode', v as TweakValues['chartMode'])} />
        <TweakToggle label="長條圓角" value={t.barRounded} onChange={v => setTweak('barRounded', v)} />
        <TweakToggle label="顯示格線" value={t.showGrid} onChange={v => setTweak('showGrid', v)} />
      </TweaksPanel>
    </div>
  );
}
