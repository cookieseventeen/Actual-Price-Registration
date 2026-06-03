// 認證與會員資料 store — localStorage 持久化（純前端模擬）
// 無後端，故 SSO 為擬真模擬：點擊後回傳一組假身分，日後可替換為真 OAuth。
import { SEED_MEMBERS, PURPOSE_OPTIONS } from '../data/members';
import type { Member, Provider, Plan, MemberStatus } from '../data/members';

export type { Member, Provider, Plan, MemberStatus };

const MEMBERS_KEY = 'shijiatong.members';
const CURRENT_KEY = 'shijiatong.currentUserId';

// ── 持久化讀寫 ──
function read(): Member[] {
  try {
    const raw = localStorage.getItem(MEMBERS_KEY);
    if (raw) return JSON.parse(raw) as Member[];
  } catch { /* ignore */ }
  return [];
}

function write(members: Member[]) {
  try { localStorage.setItem(MEMBERS_KEY, JSON.stringify(members)); } catch { /* ignore */ }
}

/** 首次載入：若 localStorage 為空則寫入種子資料 */
export function seedMembers() {
  if (read().length === 0) write(SEED_MEMBERS);
}

export function listMembers(): Member[] {
  return read().slice().sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

export function findMemberByEmail(email: string): Member | undefined {
  return read().find(m => m.email.toLowerCase() === email.toLowerCase());
}

export function getMember(id: string): Member | undefined {
  return read().find(m => m.id === id);
}

// ── 目前登入者 ──
export function getCurrentUser(): Member | null {
  const id = localStorage.getItem(CURRENT_KEY);
  if (!id) return null;
  return getMember(id) ?? null;
}

export function setCurrentUser(id: string | null) {
  if (id) localStorage.setItem(CURRENT_KEY, id);
  else localStorage.removeItem(CURRENT_KEY);
}

export function signOut() {
  setCurrentUser(null);
}

// ── 模擬 SSO ──
export interface SsoProfile {
  email: string;
  name: string;
  avatar: string;
  provider: Provider;
}

// 每個 provider 一組固定的「新用戶」假身分，方便展示註冊流程
const SSO_IDENTITIES: Record<Provider, SsoProfile> = {
  google: { email: 'demo.user@gmail.com',   name: '示範使用者', avatar: '示', provider: 'google' },
  apple:  { email: 'demo.user@icloud.com',  name: '示範使用者', avatar: '示', provider: 'apple' },
};

/** 模擬 SSO 同意流程，回傳第三方帶回的基本資料 */
export function mockSSO(provider: Provider): SsoProfile {
  return { ...SSO_IDENTITIES[provider] };
}

// ── 註冊 / 更新 ──
function now(): string {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`;
}

export interface RegisterInput {
  profile: SsoProfile;
  name: string;
  plan: Plan;
  purpose: string;
}

/** 新會員註冊 — 狀態為待審核 (pending) */
export function registerMember(input: RegisterInput): Member {
  const members = read();
  const member: Member = {
    id: `m-${Date.now()}`,
    name: input.name.trim() || input.profile.name,
    email: input.profile.email,
    avatar: (input.name.trim() || input.profile.name).charAt(0),
    provider: input.profile.provider,
    plan: input.plan,
    status: 'pending',
    purpose: input.purpose,
    createdAt: now(),
  };
  members.push(member);
  write(members);
  return member;
}

export function updateMember(id: string, patch: Partial<Member>): Member | undefined {
  const members = read();
  const idx = members.findIndex(m => m.id === id);
  if (idx === -1) return undefined;
  const updated = { ...members[idx], ...patch };
  members[idx] = updated;
  write(members);
  return updated;
}

/** 後台變更會員狀態（核准/拒絕/停權/恢復），記錄審核時間 */
export function setMemberStatus(id: string, status: MemberStatus): Member | undefined {
  return updateMember(id, { status, reviewedAt: now() });
}

export function setMemberPlan(id: string, plan: Plan): Member | undefined {
  return updateMember(id, { plan });
}

// ── 顯示用 meta ──
export const PLAN_META: Record<Plan, { label: string }> = {
  free:       { label: '免費' },
  pro:        { label: '專業' },
  enterprise: { label: '企業' },
};

export const STATUS_META: Record<MemberStatus, { label: string; color: string; bg: string; icon: string }> = {
  pending:   { label: '待審核', color: 'var(--ore-warning)', bg: 'color-mix(in srgb, var(--ore-warning) 16%, transparent)', icon: 'pi-clock' },
  active:    { label: '已啟用', color: 'var(--ore-success)', bg: 'color-mix(in srgb, var(--ore-success) 14%, transparent)', icon: 'pi-check-circle' },
  rejected:  { label: '已拒絕', color: 'var(--ore-danger)',  bg: 'color-mix(in srgb, var(--ore-danger) 14%, transparent)',  icon: 'pi-times-circle' },
  suspended: { label: '已停權', color: 'var(--ore-fg-muted)', bg: 'var(--ore-surface-100)',                                 icon: 'pi-ban' },
};

export { PURPOSE_OPTIONS };
