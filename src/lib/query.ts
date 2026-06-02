// 成交紀錄查詢邏輯 — 純函式，供 ResultsView 與匯出計數共用、便於測試
import type { Transaction } from '../data/mock';
import type { Filters, SortState } from '../types';

export interface QueryCriteria {
  district?: string;
  query?: string;
  filters: Filters;
}

// 依行政區、關鍵字與篩選條件過濾成交紀錄
export function filterTransactions(txs: Transaction[], { district, query, filters }: QueryCriteria): Transaction[] {
  return txs.filter(t => {
    if (district && t.district !== district) return false;
    if (query) {
      const q = query.trim();
      if (q && !(t.community.includes(q) || t.road.includes(q) || t.section.includes(q) || t.district.includes(q))) return false;
    }
    if (filters.type.length && !filters.type.includes(t.type)) return false;
    if (filters.layout.length && !filters.layout.includes(t.layout)) return false;
    if (filters.trade.length && !filters.trade.includes(t.trade)) return false;
    return true;
  });
}

// 依欄位排序（數值用差值，字串用 zh-Hant locale 比較），不變更輸入陣列
export function sortTransactions(rows: Transaction[], sort: SortState): Transaction[] {
  return [...rows].sort((a, b) => {
    const dir = sort.dir === 'asc' ? 1 : -1;
    const va = a[sort.key], vb = b[sort.key];
    if (typeof va === 'number' && typeof vb === 'number') return (va - vb) * dir;
    return String(va).localeCompare(String(vb), 'zh-Hant') * dir;
  });
}
