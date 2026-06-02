// 資料來源 / 爬蟲任務狀態頁

const STATUS_META = {
  done:    { label: '完成',   color: 'var(--ore-success)', bg: 'color-mix(in srgb, var(--ore-success) 14%, transparent)', icon: 'pi-check-circle' },
  running: { label: '爬取中', color: 'var(--brand)',       bg: 'var(--brand-50)',                                        icon: 'pi-spin pi-spinner' },
  queued:  { label: '排隊中', color: 'var(--ore-warning)', bg: 'color-mix(in srgb, var(--ore-warning) 16%, transparent)', icon: 'pi-clock' },
  error:   { label: '失敗',   color: 'var(--ore-danger)',  bg: 'color-mix(in srgb, var(--ore-danger) 14%, transparent)',  icon: 'pi-exclamation-triangle' },
};

function StatusTag({ status }) {
  const m = STATUS_META[status];
  return (
    <span className="tag" style={{ background: m.bg, color: m.color }}>
      <i className={`pi ${m.icon}`} style={{ fontSize: 11 }}></i>{m.label}
    </span>
  );
}

function DataView() {
  const tasks = window.CRAWL_TASKS;
  const total = tasks.reduce((s, t) => s + t.records, 0);
  const done = tasks.filter(t => t.status === 'done').length;

  return (
    <div className="content-narrow fade-up">
      <div style={{ marginBottom: 16 }}>
        <h1 className="page-title">資料來源與爬取狀態</h1>
        <p className="page-sub">每日 03:00 自動排程 · 來源：內政部不動產交易實價查詢服務網開放資料</p>
      </div>

      {/* 來源卡 */}
      <div className="card card-pad" style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
        <div style={{ width: 46, height: 46, borderRadius: 11, background: 'var(--brand-50)', display: 'grid', placeItems: 'center', flexShrink: 0 }}>
          <i className="pi pi-database" style={{ fontSize: 20, color: 'var(--brand)' }}></i>
        </div>
        <div style={{ flex: 1, minWidth: 200 }}>
          <div style={{ fontSize: 15, fontWeight: 700, color: 'var(--ore-fg)' }}>內政部不動產交易實價查詢服務網</div>
          <div className="mono" style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginTop: 2 }}>lvr.land.moi.gov.tw · 開放資料每 10 日更新</div>
        </div>
        <span className="status-pill"><span className="status-dot"></span>連線正常</span>
        <button className="btn btn-secondary btn-sm"><i className="pi pi-refresh"></i>立即重爬</button>
      </div>

      {/* 統計 */}
      <div style={{ display: 'flex', gap: 14, marginBottom: 16, flexWrap: 'wrap' }}>
        {[
          ['累計收錄', total.toLocaleString(), '筆', 'pi-file'],
          ['行政區任務', `${done}/${tasks.length}`, '已完成', 'pi-sitemap'],
          ['本次耗時', '24', '分鐘', 'pi-clock'],
          ['下次排程', '明日 03:00', '', 'pi-calendar'],
        ].map(([l, v, u, ic], i) => (
          <div key={i} className="card card-pad" style={{ flex: 1, minWidth: 150, display: 'flex', alignItems: 'center', gap: 12 }}>
            <i className={`pi ${ic}`} style={{ fontSize: 18, color: 'var(--brand)' }}></i>
            <div>
              <div style={{ display: 'flex', alignItems: 'baseline', gap: 4 }}>
                <span className="mono" style={{ fontSize: 19, fontWeight: 800, color: 'var(--ore-fg)' }}>{v}</span>
                <span style={{ fontSize: 11, color: 'var(--ore-fg-muted)' }}>{u}</span>
              </div>
              <div style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginTop: 2 }}>{l}</div>
            </div>
          </div>
        ))}
      </div>

      {/* 任務表 */}
      <div className="card" style={{ overflow: 'hidden' }}>
        <div style={{ padding: 'var(--pad-4) var(--pad-5)', borderBottom: '1px solid var(--ore-border)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <h3 className="section-title">各行政區爬取任務</h3>
          <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>2026-05-28 排程</span>
        </div>
        <div className="tbl-wrap" style={{ overflowX: 'auto' }}>
          <table className="tbl">
            <thead>
              <tr>
                <th>任務</th><th>行政區</th><th>狀態</th>
                <th style={{ textAlign: 'right' }}>收錄筆數</th>
                <th>最後執行</th><th style={{ textAlign: 'right' }}>耗時</th><th>下次排程</th>
              </tr>
            </thead>
            <tbody>
              {tasks.map(t => (
                <tr key={t.id} style={{ cursor: 'default' }}>
                  <td className="mono" style={{ color: 'var(--ore-fg-muted)' }}>{t.id}</td>
                  <td style={{ fontWeight: 600, color: 'var(--ore-fg)' }}>{t.district}</td>
                  <td><StatusTag status={t.status} /></td>
                  <td className="num" style={{ fontWeight: 700, color: t.status === 'error' ? 'var(--ore-danger)' : 'var(--ore-fg)' }}>{t.records.toLocaleString()}</td>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>{t.lastRun}</td>
                  <td className="num mono" style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>{t.duration}</td>
                  <td className="mono" style={{ fontSize: 12, color: 'var(--ore-fg-muted)' }}>{t.next}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

// ── 匯出 Modal ──
function ExportModal({ onClose, count }) {
  const ALL_FIELDS = ['社區/地址', '行政區', '總價', '單價', '坪數', '格局', '樓層', '屋齡', '建物型態', '車位', '成交日期', '交易類型', '資料來源'];
  const [fields, setFields] = _useState(() => new Set(['社區/地址', '行政區', '總價', '單價', '坪數', '格局', '成交日期']));
  const [fmt, setFmt] = _useState('csv');
  const [stage, setStage] = _useState('config'); // config | running | done

  function toggle(f) {
    setFields(prev => { const n = new Set(prev); n.has(f) ? n.delete(f) : n.add(f); return n; });
  }
  function run() {
    setStage('running');
    setTimeout(() => setStage('done'), 1400);
  }

  const FMT = [['csv', 'CSV', 'pi-file'], ['xlsx', 'Excel', 'pi-file-excel'], ['json', 'JSON', 'pi-code']];

  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(15,23,42,0.5)', display: 'grid', placeItems: 'center', zIndex: 1000, animation: 'fadeUp 160ms ease' }} onClick={onClose}>
      <div className="card" style={{ width: 520, maxWidth: '92vw', boxShadow: 'var(--ore-shadow-xl)' }} onClick={e => e.stopPropagation()}>
        <div style={{ padding: 'var(--pad-5)', borderBottom: '1px solid var(--ore-border)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <h3 style={{ fontSize: 17, fontWeight: 700, color: 'var(--ore-fg)' }}>匯出成交資料</h3>
          <button className="icon-btn" onClick={onClose}><i className="pi pi-times"></i></button>
        </div>

        {stage === 'done' ? (
          <div style={{ padding: '36px var(--pad-5)', textAlign: 'center' }}>
            <div style={{ width: 56, height: 56, borderRadius: '50%', background: 'color-mix(in srgb, var(--ore-success) 16%, transparent)', display: 'grid', placeItems: 'center', margin: '0 auto 14px' }}>
              <i className="pi pi-check" style={{ fontSize: 24, color: 'var(--ore-success)' }}></i>
            </div>
            <div style={{ fontSize: 16, fontWeight: 700, color: 'var(--ore-fg)' }}>匯出完成</div>
            <p style={{ fontSize: 13, color: 'var(--ore-fg-muted)', marginTop: 6 }}>
              台中市成交紀錄_{count}筆.{fmt} 已開始下載
            </p>
            <button className="btn btn-primary" style={{ marginTop: 18 }} onClick={onClose}><i className="pi pi-check"></i>完成</button>
          </div>
        ) : (
          <>
            <div style={{ padding: 'var(--pad-5)' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '10px 12px', borderRadius: 9, background: 'var(--brand-50)', marginBottom: 18 }}>
                <i className="pi pi-info-circle" style={{ color: 'var(--brand)' }}></i>
                <span style={{ fontSize: 13, color: 'var(--ore-fg)' }}>將匯出符合目前篩選的 <strong className="mono" style={{ color: 'var(--brand-600)' }}>{count}</strong> 筆成交紀錄</span>
              </div>

              <div className="field-label">檔案格式</div>
              <div style={{ display: 'flex', gap: 8, marginBottom: 18 }}>
                {FMT.map(([id, lb, ic]) => (
                  <button key={id} onClick={() => setFmt(id)}
                    style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6, padding: '12px', borderRadius: 9, cursor: 'pointer',
                             border: `1.5px solid ${fmt === id ? 'var(--brand)' : 'var(--ore-border)'}`, background: fmt === id ? 'var(--brand-50)' : 'transparent',
                             color: fmt === id ? 'var(--brand-600)' : 'var(--ore-fg-muted)', fontFamily: 'inherit', fontWeight: 600, fontSize: 13 }}>
                    <i className={`pi ${ic}`} style={{ fontSize: 18 }}></i>{lb}
                  </button>
                ))}
              </div>

              <div className="field-label">匯出欄位（{fields.size}）</div>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 7 }}>
                {ALL_FIELDS.map(f => (
                  <span key={f} className={`chip ${fields.has(f) ? 'on' : ''}`} onClick={() => toggle(f)}>
                    {fields.has(f) && <i className="pi pi-check" style={{ fontSize: 10 }}></i>}{f}
                  </span>
                ))}
              </div>
            </div>
            <div style={{ padding: 'var(--pad-4) var(--pad-5)', borderTop: '1px solid var(--ore-border)', display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
              <button className="btn btn-secondary" onClick={onClose}>取消</button>
              <button className="btn btn-primary" onClick={run} disabled={stage === 'running' || fields.size === 0} style={{ opacity: fields.size === 0 ? 0.5 : 1 }}>
                {stage === 'running' ? <><i className="pi pi-spin pi-spinner"></i>產生中…</> : <><i className="pi pi-download"></i>匯出 {fields.size} 欄位</>}
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

Object.assign(window, { DataView, ExportModal });
