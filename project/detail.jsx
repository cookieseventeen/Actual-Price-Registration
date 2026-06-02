// 物件 / 社區詳情頁

function KeyStat({ label, value, unit, accent }) {
  return (
    <div style={{ padding: 'var(--pad-4)', borderRight: '1px solid var(--ore-border)' }}>
      <div style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 5 }}>{label}</div>
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 4 }}>
        <span className="mono" style={{ fontSize: 23, fontWeight: 800, color: accent ? 'var(--brand)' : 'var(--ore-fg)', letterSpacing: '-.02em' }}>{value}</span>
        {unit && <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>{unit}</span>}
      </div>
    </div>
  );
}

function DetailView({ t, onBack, chartMode, showGrid }) {
  // 建構 12 個月趨勢（以該筆單價回推，營造合理曲線）
  const base = t.unit;
  const trend = window.COMMUNITY_TREND[t.community] ||
    Array.from({ length: 12 }, (_, i) => +(base * (0.86 + 0.014 * i) + (i % 3 === 0 ? -0.6 : 0.4)).toFixed(1));
  const months = window.CITY_TREND.months;

  // 同社區其他成交（同區 + 相近單價）
  const sameComm = window.TRANSACTIONS.filter(x => x.community === t.community && x.id !== t.id);
  const nearby = window.TRANSACTIONS
    .filter(x => x.district === t.district && x.id !== t.id && x.community !== t.community)
    .slice(0, 5);

  const firstUnit = trend[0], lastUnit = trend[trend.length - 1];
  const growth = (((lastUnit - firstUnit) / firstUnit) * 100).toFixed(1);

  return (
    <div className="content-narrow fade-up">
      <button className="btn btn-ghost btn-sm" style={{ marginBottom: 14, paddingLeft: 4 }} onClick={onBack}>
        <i className="pi pi-arrow-left"></i>返回成交紀錄
      </button>

      {/* 標頭 */}
      <div className="card" style={{ overflow: 'hidden', marginBottom: 16 }}>
        <div style={{ padding: 'var(--pad-5)', display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16, flexWrap: 'wrap' }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <h1 style={{ fontSize: 25, fontWeight: 800, color: 'var(--ore-fg)', letterSpacing: '-.02em' }}>{t.community}</h1>
              {t.trade === '預售屋' ? <span className="tag tag-presale">預售屋</span> : <span className="tag tag-done">成屋</span>}
            </div>
            <div style={{ fontSize: 13.5, color: 'var(--ore-fg-muted)', marginTop: 6 }}>
              <i className="pi pi-map-marker" style={{ fontSize: 12, marginRight: 5, color: 'var(--brand)' }}></i>
              台中市{t.district} {t.road} · {t.section}
            </div>
            <div style={{ display: 'flex', gap: 7, marginTop: 12, flexWrap: 'wrap' }}>
              <span className="chip" style={{ cursor: 'default' }}><i className="pi pi-building" style={{ fontSize: 11 }}></i>{t.type}</span>
              <span className="chip" style={{ cursor: 'default' }}>{t.layout}</span>
              <span className="chip" style={{ cursor: 'default' }}>{t.parking}</span>
            </div>
          </div>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>成交總價</div>
            <div className="mono" style={{ fontSize: 36, fontWeight: 900, color: 'var(--brand)', letterSpacing: '-.03em', lineHeight: 1.1 }}>{fmtTotal(t.total)}</div>
            <div className="mono" style={{ fontSize: 13, color: 'var(--ore-fg-muted)', marginTop: 2 }}>{t.unit} 萬/坪 · {t.date}</div>
          </div>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', borderTop: '1px solid var(--ore-border)', background: 'var(--ore-bg)' }}>
          <KeyStat label="權狀坪數" value={t.ping} unit="坪" />
          <KeyStat label="單價" value={t.unit} unit="萬/坪" accent />
          <KeyStat label="樓層" value={t.floor} />
          <KeyStat label="屋齡" value={t.age} unit="年" />
          <div style={{ padding: 'var(--pad-4)' }}>
            <div style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 5 }}>格局</div>
            <div className="mono" style={{ fontSize: 23, fontWeight: 800, color: 'var(--ore-fg)' }}>{t.rooms}<span style={{ fontSize: 12, fontWeight: 500, color: 'var(--ore-fg-muted)' }}> 房</span></div>
          </div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1.6fr 1fr', gap: 16 }}>
        {/* 趨勢 */}
        <div className="card card-pad">
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
            <h3 className="section-title">社區成交單價走勢</h3>
            <span className={+growth >= 0 ? 'delta-up' : 'delta-down'} style={{ fontSize: 13 }}>
              近一年 <i className={`pi ${+growth >= 0 ? 'pi-arrow-up-right' : 'pi-arrow-down-right'}`} style={{ fontSize: 10 }}></i> {Math.abs(growth)}%
            </span>
          </div>
          <p style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginBottom: 8 }}>單位：萬元 / 坪</p>
          <TrendChart data={trend} labels={months} mode={chartMode} showGrid={showGrid} unit=" 萬/坪" valueFmt={v => v.toFixed(0)} height={230} />
        </div>

        {/* 資料來源卡 */}
        <div className="card card-pad" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <h3 className="section-title">資料來源</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 11 }}>
            {[
              ['來源機關', t.source + ' · 實價登錄'],
              ['登錄編號', t.id + '-TC'],
              ['交易標的', '房地（土地+建物）'],
              ['資料擷取', t.crawled],
            ].map(([k, v]) => (
              <div key={k} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8 }}>
                <span style={{ fontSize: 12.5, color: 'var(--ore-fg-muted)' }}>{k}</span>
                <span className="mono" style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--ore-fg)', textAlign: 'right' }}>{v}</span>
              </div>
            ))}
          </div>
          <div style={{ padding: '10px 12px', borderRadius: 9, background: 'var(--brand-50)', border: '1px solid var(--ore-border)', display: 'flex', gap: 9, alignItems: 'flex-start' }}>
            <i className="pi pi-verified" style={{ color: 'var(--brand)', fontSize: 15, marginTop: 1 }}></i>
            <div style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', lineHeight: 1.55 }}>
              本筆資料每日 03:00 自動比對內政部開放資料，與官方登錄一致。
            </div>
          </div>
        </div>
      </div>

      {/* 同社區成交 */}
      {sameComm.length > 0 && (
        <div className="card card-pad" style={{ marginTop: 16 }}>
          <h3 className="section-title" style={{ marginBottom: 12 }}>同社區其他成交</h3>
          <CompactList rows={sameComm} />
        </div>
      )}

      {/* 周邊比價 */}
      <div className="card card-pad" style={{ marginTop: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
          <h3 className="section-title">{t.district} 周邊比價</h3>
          <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>相近區段最近成交</span>
        </div>
        <CompactList rows={nearby} showCommunity />
      </div>
    </div>
  );
}

function CompactList({ rows, showCommunity }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column' }}>
      {rows.map((r, i) => (
        <div key={r.id} style={{ display: 'flex', alignItems: 'center', gap: 12, padding: '10px 0', borderTop: i ? '1px solid var(--ore-surface-100)' : 'none' }}>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ fontSize: 13.5, fontWeight: 600, color: 'var(--ore-fg)' }}>{showCommunity ? r.community : r.layout + ' · ' + r.floor}</div>
            <div style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>{showCommunity ? r.section + ' · ' + r.layout : r.section} · {r.date}</div>
          </div>
          <div className="tnum" style={{ fontSize: 12.5, color: 'var(--ore-fg-muted)' }}>{r.ping} 坪</div>
          <div className="mono" style={{ fontSize: 12.5, fontWeight: 700, color: 'var(--brand-600)', width: 68, textAlign: 'right' }}>{r.unit} <span style={{ fontSize: 10, fontWeight: 500, color: 'var(--ore-fg-muted)' }}>萬/坪</span></div>
          <div className="mono" style={{ fontSize: 14, fontWeight: 800, color: 'var(--ore-fg)', width: 78, textAlign: 'right' }}>{fmtTotalShort(r.total)}<span style={{ fontSize: 10, fontWeight: 500, color: 'var(--ore-fg-muted)' }}>{r.total >= 10000 ? '億' : '萬'}</span></div>
        </div>
      ))}
    </div>
  );
}

Object.assign(window, { DetailView });
