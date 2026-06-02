// ── 格式化 ──
export function fmtTotal(wan: number): string {
  if (wan >= 10000) return (wan / 10000).toFixed(2) + ' 億';
  return wan.toLocaleString() + ' 萬';
}

// 僅回傳數字（單位由呼叫端依 >= 10000 與否自行附加「億」/「萬」）
export function fmtTotalShort(wan: number): string {
  if (wan >= 10000) return (wan / 10000).toFixed(2);
  return wan.toLocaleString();
}
