// 實價通 — App 主程式（路由 + Tweaks）

const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "brand": "#38b2a0",
  "dark": false,
  "fontSize": 14,
  "density": "regular",
  "layout": "sidebar",
  "dataView": "card",
  "chartMode": "area",
  "barRounded": true,
  "showGrid": true
}/*EDITMODE-END*/;

const DENSITY_SCALE = { compact: 0.84, regular: 1, comfy: 1.16 };

function App() {
  const [t, setTweak] = useTweaks(TWEAK_DEFAULTS);

  const [view, setView] = _useState('search');     // search | results | analysis | data
  const [detail, setDetail] = _useState(null);      // 選中的成交紀錄
  const [district, setDistrict] = _useState('');
  const [query, setQuery] = _useState('');
  const [filters, setFilters] = _useState({ type: [], layout: [], trade: [] });
  const [sort, setSort] = _useState({ key: 'date', dir: 'desc' });
  const [viewMode, setViewMode] = _useState(t.dataView);
  const [showExport, setShowExport] = _useState(false);

  // tweak 改變預設呈現方式時同步
  _useEffect(() => { setViewMode(t.dataView); }, [t.dataView]);
  // 讓頁內切換鈕可改 viewMode
  _useEffect(() => { window.__setViewMode = setViewMode; }, []);

  // 套用 tweak 至 :root
  _useEffect(() => {
    const r = document.documentElement;
    r.style.setProperty('--brand', t.brand);
    r.style.setProperty('--density', String(DENSITY_SCALE[t.density] || 1));
    r.style.setProperty('--app-font-size', t.fontSize + 'px');
    r.setAttribute('data-theme', t.dark ? 'dark' : 'light');
  }, [t.brand, t.density, t.fontSize, t.dark]);

  function toggleFilter(group, val) {
    setFilters(prev => {
      const arr = prev[group];
      return { ...prev, [group]: arr.includes(val) ? arr.filter(x => x !== val) : [...arr, val] };
    });
  }

  function runSearch(dist) {
    if (typeof dist === 'string') setDistrict(dist);
    setDetail(null);
    setView('results');
  }
  function openDetail(tx) { setDetail(tx); }

  function nav(v) { setDetail(null); setView(v); }

  // 計算目前匯出筆數
  const exportCount = window.TRANSACTIONS.filter(x => {
    if (district && x.district !== district) return false;
    if (filters.type.length && !filters.type.includes(x.type)) return false;
    if (filters.layout.length && !filters.layout.includes(x.layout)) return false;
    if (filters.trade.length && !filters.trade.includes(x.trade)) return false;
    return true;
  }).length;

  const rail = t.layout === 'rail';

  return (
    <div className="app-shell">
      <TopBar
        dark={t.dark} onToggleDark={() => setTweak('dark', !t.dark)}
        onToggleLayout={() => setTweak('layout', t.layout === 'sidebar' ? 'topnav' : t.layout === 'topnav' ? 'rail' : 'sidebar')}
        lastSync="2026-05-28 03:40" onExport={() => setShowExport(true)} view={view}
      />

      {t.layout === 'topnav' && <TopNav view={detail ? 'results' : view} onNav={nav} />}

      <div className="app-body">
        {t.layout !== 'topnav' && <Sidebar view={detail ? 'results' : view} onNav={nav} rail={rail} />}

        <div className="main">
          <div className="content">
            {detail ? (
              <DetailView t={detail} onBack={() => setDetail(null)} chartMode={t.chartMode} showGrid={t.showGrid} />
            ) : view === 'search' ? (
              <SearchView onSearch={runSearch} district={district} setDistrict={setDistrict}
                          query={query} setQuery={setQuery} filters={filters} toggleFilter={toggleFilter} />
            ) : view === 'results' ? (
              <ResultsView district={district} setDistrict={setDistrict} query={query} filters={filters}
                           toggleFilter={toggleFilter} viewMode={viewMode} sort={sort} setSort={setSort} onOpen={openDetail} />
            ) : view === 'analysis' ? (
              <AnalysisView chartMode={t.chartMode} showGrid={t.showGrid} barRounded={t.barRounded} />
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
                    onChange={v => setTweak('density', v)} />

        <TweakSection label="版面佈局" />
        <TweakRadio label="導覽樣式" value={t.layout} options={['sidebar', 'topnav', 'rail']}
                    onChange={v => setTweak('layout', v)} />
        <TweakRadio label="成交資料呈現" value={t.dataView} options={['card', 'table']}
                    onChange={v => setTweak('dataView', v)} />

        <TweakSection label="圖表樣式" />
        <TweakRadio label="趨勢圖" value={t.chartMode} options={['area', 'line']}
                    onChange={v => setTweak('chartMode', v)} />
        <TweakToggle label="長條圓角" value={t.barRounded} onChange={v => setTweak('barRounded', v)} />
        <TweakToggle label="顯示格線" value={t.showGrid} onChange={v => setTweak('showGrid', v)} />
      </TweaksPanel>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
