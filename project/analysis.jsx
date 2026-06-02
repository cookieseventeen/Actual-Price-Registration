// 行情分析頁

function AnalysisView({ chartMode, showGrid, barRounded }) {
  const trend = window.CITY_TREND;
  const dist = window.PRICE_DISTRIBUTION;
  const districts = [...window.TC_DISTRICTS].sort((a, b) => b.avg - a.avg);
  const rankData = districts.map(d => ({ label: d.name, value: d.avg, accent: d.name === '西屯區' }));
  const distData = dist.map(d => ({ label: d.range, value: d.count }));
  const volData = trend.months.map((m, i) => ({ label: m, value: trend.volume[i] }));
  const accentDistIdx = dist.reduce((mi, d, i, arr) => d.count > arr[mi].count ? i : mi, 0);

  return (
    <div className="content-narrow fade-up">
      <div style={{ marginBottom: 16 }}>
        <h1 className="page-title">台中市行情分析</h1>
        <p className="page-sub">近 12 個月成交資料彙整 · 更新於 2026-05-28</p>
      </div>

      {/* 概況列 */}
      <div style={{ display: 'flex', gap: 14, marginBottom: 16, flexWrap: 'wrap' }}>
        {[
          ['全市均價', '34.8', '萬/坪', 3.4],
          ['最高 西屯', '42.8', '萬/坪', 3.2],
          ['年成交量', '46,800', '筆', 8.1],
          ['預售占比', '31.5', '%', 5.2],
        ].map(([l, v, u, d], i) => (
          <div key={i} className="card card-pad" style={{ flex: 1, minWidth: 150 }}>
            <div className="stat-label" style={{ marginTop: 0, marginBottom: 6 }}>{l}</div>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 5 }}>
              <span className="mono stat-value">{v}</span>
              <span style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>{u}</span>
              <span className="delta-up" style={{ fontSize: 12, marginLeft: 'auto' }}><i className="pi pi-arrow-up-right" style={{ fontSize: 9 }}></i>{d}%</span>
            </div>
          </div>
        ))}
      </div>

      {/* 全市單價走勢 */}
      <div className="card card-pad" style={{ marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 4 }}>
          <h3 className="section-title">全市成交單價走勢</h3>
          <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>單位：萬元 / 坪</span>
        </div>
        <TrendChart data={trend.unit} labels={trend.months} mode={chartMode} showGrid={showGrid} unit=" 萬/坪" valueFmt={v => v.toFixed(0)} height={250} />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
        {/* 行政區排行 */}
        <div className="card card-pad">
          <h3 className="section-title" style={{ marginBottom: 4 }}>行政區單價排行</h3>
          <p style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 16 }}>成交均價 萬元 / 坪</p>
          <RankBars data={rankData} />
        </div>

        {/* 價格分布 */}
        <div className="card card-pad">
          <h3 className="section-title" style={{ marginBottom: 4 }}>單價分布</h3>
          <p style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 8 }}>橫軸 萬元/坪 · 縱軸 成交件數</p>
          <BarChart data={distData} rounded={barRounded} unit=" 件" accentIndex={accentDistIdx} height={250} />
        </div>
      </div>

      {/* 成交量 */}
      <div className="card card-pad">
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 4 }}>
          <h3 className="section-title">月成交量</h3>
          <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>單位：件</span>
        </div>
        <BarChart data={volData} rounded={barRounded} unit=" 件" height={230} />
      </div>
    </div>
  );
}

Object.assign(window, { AnalysisView });
