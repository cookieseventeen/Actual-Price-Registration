// 後台會員管理 — 確認會員使用資格（核准/拒絕/停權/恢復、調整方案）
import { useState } from 'react';
import {
  listMembers, setMemberStatus, setMemberPlan,
  PLAN_META, STATUS_META,
} from '../../lib/auth';
import type { Member, MemberStatus, Plan } from '../../lib/auth';

type FilterKey = 'all' | MemberStatus;

const FILTERS: { key: FilterKey; label: string }[] = [
  { key: 'all',       label: '全部' },
  { key: 'pending',   label: '待審核' },
  { key: 'active',    label: '已啟用' },
  { key: 'suspended', label: '已停權' },
  { key: 'rejected',  label: '已拒絕' },
];

function StatusTag({ status }: { status: MemberStatus }) {
  const m = STATUS_META[status];
  return (
    <span className="tag" style={{ background: m.bg, color: m.color }}>
      <i className={`pi ${m.icon}`} style={{ fontSize: 11 }}></i>{m.label}
    </span>
  );
}

function ActionBtn({ label, icon, tone, onClick }:
  { label: string; icon: string; tone: 'primary' | 'danger' | 'neutral'; onClick: () => void }) {
  const color = tone === 'primary' ? 'var(--brand-600)' : tone === 'danger' ? 'var(--ore-danger)' : 'var(--ore-fg-muted)';
  return (
    <button onClick={onClick}
      style={{ display: 'inline-flex', alignItems: 'center', gap: 5, padding: '5px 10px', borderRadius: 7, cursor: 'pointer',
               border: '1px solid var(--ore-border)', background: 'var(--ore-card-bg)', color, fontFamily: 'inherit', fontSize: 12, fontWeight: 600 }}>
      <i className={`pi ${icon}`} style={{ fontSize: 11 }}></i>{label}
    </button>
  );
}

export function AdminView({ currentUserId, onChange }: { currentUserId?: string; onChange?: () => void }) {
  const [members, setMembers] = useState<Member[]>(() => listMembers());
  const [filter, setFilter] = useState<FilterKey>('all');

  function refresh() {
    setMembers(listMembers());
    onChange?.();
  }

  function changeStatus(id: string, status: MemberStatus) { setMemberStatus(id, status); refresh(); }
  function changePlan(id: string, plan: Plan) { setMemberPlan(id, plan); refresh(); }

  const counts = {
    total: members.length,
    pending: members.filter(m => m.status === 'pending').length,
    active: members.filter(m => m.status === 'active').length,
  };

  const rows = filter === 'all' ? members : members.filter(m => m.status === filter);

  return (
    <div className="content-narrow fade-up">
      <div style={{ marginBottom: 16 }}>
        <h1 className="page-title">會員管理</h1>
        <p className="page-sub">確認會員使用資格 · 審核註冊申請、調整方案與權限</p>
      </div>

      {/* 統計 */}
      <div style={{ display: 'flex', gap: 14, marginBottom: 16, flexWrap: 'wrap' }}>
        {([
          ['總會員', counts.total, 'pi-users', 'var(--brand)'],
          ['待審核', counts.pending, 'pi-clock', 'var(--ore-warning)'],
          ['已啟用', counts.active, 'pi-check-circle', 'var(--ore-success)'],
        ] as [string, number, string, string][]).map(([l, v, ic, c]) => (
          <div key={l} className="card card-pad" style={{ flex: 1, minWidth: 150, display: 'flex', alignItems: 'center', gap: 12 }}>
            <i className={`pi ${ic}`} style={{ fontSize: 18, color: c }}></i>
            <div>
              <div className="mono" style={{ fontSize: 19, fontWeight: 800, color: 'var(--ore-fg)' }}>{v}</div>
              <div style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)', marginTop: 2 }}>{l}</div>
            </div>
          </div>
        ))}
      </div>

      {/* 篩選 */}
      <div style={{ display: 'flex', gap: 7, marginBottom: 14, flexWrap: 'wrap' }}>
        {FILTERS.map(f => {
          const n = f.key === 'all' ? members.length : members.filter(m => m.status === f.key).length;
          return (
            <span key={f.key} className={`chip ${filter === f.key ? 'on' : ''}`} onClick={() => setFilter(f.key)}>
              {f.label}<span className="mono" style={{ opacity: 0.7 }}>{n}</span>
            </span>
          );
        })}
      </div>

      {/* 會員表 */}
      <div className="card" style={{ overflow: 'hidden' }}>
        <div className="tbl-wrap" style={{ overflowX: 'auto' }}>
          <table className="tbl">
            <thead>
              <tr>
                <th>會員</th><th>來源</th><th>方案</th><th>狀態</th><th>用途</th><th>註冊時間</th>
                <th style={{ textAlign: 'right' }}>操作</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(m => (
                <tr key={m.id} style={{ cursor: 'default' }}>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <div className="avatar" style={{ flexShrink: 0 }}>{m.avatar}</div>
                      <div style={{ minWidth: 0 }}>
                        <div style={{ fontWeight: 600, color: 'var(--ore-fg)', display: 'flex', alignItems: 'center', gap: 6 }}>
                          {m.name}
                          {m.id === currentUserId && <span className="tag tag-source" style={{ fontSize: 10 }}>本人</span>}
                        </div>
                        <div className="mono" style={{ fontSize: 11, color: 'var(--ore-fg-muted)' }}>{m.email}</div>
                      </div>
                    </div>
                  </td>
                  <td>
                    <span style={{ display: 'inline-flex', alignItems: 'center', gap: 5, fontSize: 12.5, color: 'var(--ore-fg-muted)' }}>
                      <i className={`pi ${m.provider === 'google' ? 'pi-google' : 'pi-apple'}`} style={{ fontSize: 12 }}></i>
                      {m.provider === 'google' ? 'Google' : 'Apple'}
                    </span>
                  </td>
                  <td>
                    <select value={m.plan} onChange={e => changePlan(m.id, e.target.value as Plan)}
                      style={{ padding: '4px 8px', borderRadius: 7, border: '1px solid var(--ore-border)', background: 'var(--ore-card-bg)',
                               color: 'var(--ore-fg)', fontFamily: 'inherit', fontSize: 12.5, cursor: 'pointer' }}>
                      {(Object.keys(PLAN_META) as Plan[]).map(p => (
                        <option key={p} value={p}>{PLAN_META[p].label}</option>
                      ))}
                    </select>
                  </td>
                  <td><StatusTag status={m.status} /></td>
                  <td style={{ fontSize: 12.5, color: 'var(--ore-fg-muted)' }}>{m.purpose}</td>
                  <td className="mono" style={{ fontSize: 11.5, color: 'var(--ore-fg-muted)' }}>{m.createdAt}</td>
                  <td>
                    <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end', flexWrap: 'wrap' }}>
                      {m.status === 'pending' && <>
                        <ActionBtn label="核准" icon="pi-check" tone="primary" onClick={() => changeStatus(m.id, 'active')} />
                        <ActionBtn label="拒絕" icon="pi-times" tone="danger" onClick={() => changeStatus(m.id, 'rejected')} />
                      </>}
                      {m.status === 'active' && (
                        <ActionBtn label="停權" icon="pi-ban" tone="danger" onClick={() => changeStatus(m.id, 'suspended')} />
                      )}
                      {(m.status === 'suspended' || m.status === 'rejected') && (
                        <ActionBtn label="恢復啟用" icon="pi-refresh" tone="primary" onClick={() => changeStatus(m.id, 'active')} />
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {rows.length === 0 && (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '32px', color: 'var(--ore-fg-muted)', fontSize: 13 }}>此狀態目前沒有會員</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
