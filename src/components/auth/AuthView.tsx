// 登入 / 註冊頁 — 全螢幕（不含 shell）
// 流程：SSO 同意 → 已是會員直接登入 / 新用戶單步註冊（status=待審核）
import { useState } from 'react';
import { mockSSO, findMemberByEmail, registerMember, PLAN_META, PURPOSE_OPTIONS } from '../../lib/auth';
import type { Member, Provider, Plan, SsoProfile } from '../../lib/auth';

// Google 四色「G」標誌
function GoogleLogo() {
  return (
    <svg width="18" height="18" viewBox="0 0 48 48" aria-hidden>
      <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
      <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
      <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
      <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
    </svg>
  );
}

// Apple 標誌
function AppleLogo() {
  return (
    <svg width="17" height="17" viewBox="0 0 384 512" aria-hidden fill="currentColor">
      <path d="M318.7 268.7c-.2-36.7 16.4-64.4 50-84.8-18.8-26.9-47.2-41.7-84.7-44.6-35.5-2.8-74.3 20.7-88.5 20.7-15 0-49.4-19.7-76.4-19.7C63.3 141.2 4 184.8 4 273.5q0 39.3 14.4 81.2c12.8 36.7 59 126.7 107.2 125.2 25.2-.6 43-17.9 75.8-17.9 31.8 0 48.3 17.9 76.4 17.9 48.6-.7 90.4-82.5 102.6-119.3-65.2-30.7-61.7-90-61.7-91.9zm-56.6-164.2c27.3-32.4 24.8-61.9 24-72.5-24.1 1.4-52 16.4-67.9 34.9-17.5 19.8-27.8 44.3-25.6 71.9 26.1 2 49.9-11.4 69.5-34.3z"/>
    </svg>
  );
}

function SsoButton({ provider, onClick }: { provider: Provider; onClick: () => void }) {
  const isGoogle = provider === 'google';
  return (
    <button onClick={onClick}
      style={{ width: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 10,
               padding: '12px 16px', borderRadius: 'var(--ore-radius-md)', cursor: 'pointer',
               border: '1px solid var(--ore-border)', background: 'var(--ore-card-bg)', color: 'var(--ore-fg)',
               fontFamily: 'inherit', fontSize: 14.5, fontWeight: 600, transition: 'border-color 150ms, background 150ms' }}
      onMouseEnter={e => { e.currentTarget.style.borderColor = 'var(--brand)'; }}
      onMouseLeave={e => { e.currentTarget.style.borderColor = 'var(--ore-border)'; }}>
      {isGoogle ? <GoogleLogo /> : <AppleLogo />}
      使用 {isGoogle ? 'Google' : 'Apple'} 繼續
    </button>
  );
}

export function AuthView({ onComplete, onCancel }: { onComplete: (m: Member) => void; onCancel: () => void }) {
  const [step, setStep] = useState<'signin' | 'register'>('signin');
  const [profile, setProfile] = useState<SsoProfile | null>(null);
  const [name, setName] = useState('');
  const [plan, setPlan] = useState<Plan>('free');
  const [purpose, setPurpose] = useState<string>(PURPOSE_OPTIONS[0]);

  function handleSSO(provider: Provider) {
    const p = mockSSO(provider);
    const existing = findMemberByEmail(p.email);
    if (existing) { onComplete(existing); return; }   // 已是會員 → 直接登入
    setProfile(p);
    setName(p.name);
    setStep('register');                               // 新用戶 → 單步註冊
  }

  function handleRegister() {
    if (!profile) return;
    const member = registerMember({ profile, name, plan, purpose });
    onComplete(member);
  }

  return (
    <div style={{ height: '100%', display: 'flex', background: 'var(--ore-bg)', overflow: 'auto' }}>
      {/* 左側 hero（窄螢幕隱藏） */}
      <div className="auth-hero" style={{
        flex: '1 1 0', minWidth: 0, padding: '56px 48px', display: 'flex', flexDirection: 'column', justifyContent: 'center',
        background: 'linear-gradient(160deg, var(--brand-600), var(--brand))', color: '#fff' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 28 }}>
          <div style={{ width: 34, height: 34, borderRadius: 9, background: 'rgba(255,255,255,0.18)', display: 'grid', placeItems: 'center' }}>
            <i className="pi pi-map-marker" style={{ fontSize: 17 }}></i>
          </div>
          <span style={{ fontSize: 19, fontWeight: 800, letterSpacing: '-.02em' }}>實價通</span>
          <span style={{ fontSize: 11, fontWeight: 700, padding: '2px 8px', borderRadius: 999, background: 'rgba(255,255,255,0.18)' }}>台中市</span>
        </div>
        <h1 style={{ fontSize: 30, fontWeight: 800, lineHeight: 1.25, letterSpacing: '-.02em', maxWidth: 420 }}>
          掌握第一手<br />不動產實價登錄行情
        </h1>
        <p style={{ fontSize: 14.5, opacity: 0.92, marginTop: 16, maxWidth: 400, lineHeight: 1.7 }}>
          每日同步內政部實價登錄開放資料，提供成交查詢、行情分析與房價地圖。登入後即可申請完整查詢與匯出權限。
        </p>
        <div style={{ display: 'flex', gap: 28, marginTop: 36 }}>
          {[['9,234', '本月收錄'], ['29', '行政區'], ['每日', '自動更新']].map(([v, l]) => (
            <div key={l}>
              <div style={{ fontSize: 22, fontWeight: 800 }}>{v}</div>
              <div style={{ fontSize: 12, opacity: 0.85, marginTop: 2 }}>{l}</div>
            </div>
          ))}
        </div>
      </div>

      {/* 右側登入 / 註冊卡 */}
      <div style={{ flex: '1 1 0', minWidth: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '40px 24px' }}>
        <div style={{ width: '100%', maxWidth: 380 }}>
          {step === 'signin' ? (
            <div className="fade-up">
              <h2 style={{ fontSize: 22, fontWeight: 800, color: 'var(--ore-fg)', letterSpacing: '-.02em' }}>登入 / 註冊</h2>
              <p className="page-sub" style={{ marginBottom: 26 }}>使用第三方帳號快速登入，首次登入將自動引導註冊。</p>

              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                <SsoButton provider="google" onClick={() => handleSSO('google')} />
                <SsoButton provider="apple" onClick={() => handleSSO('apple')} />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: 10, margin: '22px 0' }}>
                <div style={{ flex: 1, height: 1, background: 'var(--ore-border)' }}></div>
                <span style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>安全登入</span>
                <div style={{ flex: 1, height: 1, background: 'var(--ore-border)' }}></div>
              </div>
              <p style={{ fontSize: 12, color: 'var(--ore-fg-muted)', lineHeight: 1.7, display: 'flex', gap: 7 }}>
                <i className="pi pi-shield" style={{ color: 'var(--brand)', marginTop: 2 }}></i>
                <span>我們僅取得您的基本帳號資訊（Email、名稱）。註冊後需經管理員審核啟用，方可使用完整查詢與匯出功能。</span>
              </p>

              <button className="btn btn-ghost" style={{ marginTop: 24, width: '100%', justifyContent: 'center' }} onClick={onCancel}>
                <i className="pi pi-arrow-left"></i>先看看，返回主站
              </button>
            </div>
          ) : (
            <div className="fade-up">
              <button className="btn btn-ghost btn-sm" style={{ marginBottom: 14, paddingLeft: 0 }} onClick={() => setStep('signin')}>
                <i className="pi pi-arrow-left"></i>返回
              </button>
              <h2 style={{ fontSize: 22, fontWeight: 800, color: 'var(--ore-fg)', letterSpacing: '-.02em' }}>完成註冊</h2>
              <p className="page-sub" style={{ marginBottom: 22 }}>只差一步。確認資料後即送出申請。</p>

              {/* 第三方帶回的帳號 */}
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 12px', borderRadius: 'var(--ore-radius-md)',
                            background: 'var(--brand-50)', border: '1px solid var(--ore-border)', marginBottom: 20 }}>
                <div className="avatar" style={{ width: 34, height: 34 }}>{(name || profile?.name || '?').charAt(0)}</div>
                <div style={{ minWidth: 0 }}>
                  <div style={{ fontSize: 13, fontWeight: 700, color: 'var(--ore-fg)', display: 'flex', alignItems: 'center', gap: 6 }}>
                    {profile?.provider === 'google' ? <GoogleLogo /> : <AppleLogo />}
                    {profile?.provider === 'google' ? 'Google' : 'Apple'} 帳號
                  </div>
                  <div className="mono" style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', overflow: 'hidden', textOverflow: 'ellipsis' }}>{profile?.email}</div>
                </div>
              </div>

              <label className="field-label">稱呼</label>
              <input className="input" value={name} onChange={e => setName(e.target.value)} placeholder="您的稱呼" style={{ marginBottom: 18 }} />

              <div className="field-label">使用方案</div>
              <div style={{ display: 'flex', gap: 8, marginBottom: 18 }}>
                {(Object.keys(PLAN_META) as Plan[]).map(p => (
                  <button key={p} type="button" onClick={() => setPlan(p)}
                    style={{ flex: 1, padding: '10px 4px', borderRadius: 'var(--ore-radius-md)', cursor: 'pointer', fontFamily: 'inherit', fontWeight: 700, fontSize: 13,
                             border: `1.5px solid ${plan === p ? 'var(--brand)' : 'var(--ore-border)'}`,
                             background: plan === p ? 'var(--brand-50)' : 'transparent',
                             color: plan === p ? 'var(--brand-600)' : 'var(--ore-fg-muted)' }}>
                    {PLAN_META[p].label}
                  </button>
                ))}
              </div>

              <div className="field-label">使用用途</div>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 7, marginBottom: 24 }}>
                {PURPOSE_OPTIONS.map(p => (
                  <span key={p} className={`chip ${purpose === p ? 'on' : ''}`} onClick={() => setPurpose(p)}>{p}</span>
                ))}
              </div>

              <button className="btn btn-primary" style={{ width: '100%', justifyContent: 'center' }} onClick={handleRegister}>
                <i className="pi pi-check"></i>送出申請並登入
              </button>
              <p style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', textAlign: 'center', marginTop: 12, lineHeight: 1.6 }}>
                送出後帳號狀態為「待審核」，由管理員確認使用資格後啟用。
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
