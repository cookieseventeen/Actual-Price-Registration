// 會員資料 — 後台管理用的種子資料（模擬）
// 真實情境會由後端提供；此處首次載入若 localStorage 為空則寫入。

export type Provider = 'google' | 'apple';
export type Plan = 'free' | 'pro' | 'enterprise';
export type MemberStatus = 'pending' | 'active' | 'rejected' | 'suspended';

export interface Member {
  id: string;
  name: string;
  email: string;
  avatar: string;       // 顯示用的單字（取名字首字）
  provider: Provider;
  plan: Plan;
  status: MemberStatus;
  purpose: string;      // 使用用途
  createdAt: string;    // 註冊時間 YYYY-MM-DD HH:mm
  reviewedAt?: string;  // 最後審核時間
  note?: string;
}

// 用途選項（註冊與後台共用）
export const PURPOSE_OPTIONS = ['自住購屋', '投資理財', '不動產仲介', '估價/金融', '學術研究'] as const;

export const SEED_MEMBERS: Member[] = [
  { id: 'm-1001', name: '陳冠宇', email: 'kuanyu.chen@gmail.com',     avatar: '陳', provider: 'google', plan: 'pro',        status: 'active',    purpose: '不動產仲介', createdAt: '2026-04-12 09:21', reviewedAt: '2026-04-12 14:02' },
  { id: 'm-1002', name: '林佳穎', email: 'jiaying.lin@icloud.com',    avatar: '林', provider: 'apple',  plan: 'free',       status: 'active',    purpose: '自住購屋',   createdAt: '2026-04-20 18:44', reviewedAt: '2026-04-21 10:11' },
  { id: 'm-1003', name: '王志明', email: 'zhiming.wang@gmail.com',    avatar: '王', provider: 'google', plan: 'enterprise', status: 'active',    purpose: '估價/金融', createdAt: '2026-03-02 11:05', reviewedAt: '2026-03-02 16:30' },
  { id: 'm-1004', name: '張雅婷', email: 'yating.chang@icloud.com',   avatar: '張', provider: 'apple',  plan: 'free',       status: 'pending',   purpose: '投資理財',   createdAt: '2026-05-28 21:17' },
  { id: 'm-1005', name: '黃建德', email: 'jiande.huang@gmail.com',    avatar: '黃', provider: 'google', plan: 'pro',        status: 'pending',   purpose: '不動產仲介', createdAt: '2026-05-30 08:52' },
  { id: 'm-1006', name: '吳秉澄', email: 'bingcheng.wu@gmail.com',    avatar: '吳', provider: 'google', plan: 'free',       status: 'suspended', purpose: '投資理財',   createdAt: '2026-02-14 13:38', reviewedAt: '2026-05-10 09:00', note: '異常大量匯出，暫停權限' },
  { id: 'm-1007', name: '蔡宜真', email: 'yizhen.tsai@icloud.com',    avatar: '蔡', provider: 'apple',  plan: 'free',       status: 'rejected',  purpose: '學術研究',   createdAt: '2026-05-22 16:09', reviewedAt: '2026-05-23 09:45', note: '無法驗證使用單位' },
  { id: 'm-1008', name: '劉冠廷', email: 'kuanting.liu@gmail.com',    avatar: '劉', provider: 'google', plan: 'pro',        status: 'active',    purpose: '估價/金融', createdAt: '2026-01-08 10:00', reviewedAt: '2026-01-08 11:20' },
];
