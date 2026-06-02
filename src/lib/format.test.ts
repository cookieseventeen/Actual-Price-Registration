import { describe, it, expect } from 'vitest';
import { fmtTotal, fmtTotalShort } from './format';

describe('fmtTotal', () => {
  it('formats values under 1 億 as 萬 with thousands separators', () => {
    expect(fmtTotal(8980)).toBe('8,980 萬');
    expect(fmtTotal(528)).toBe('528 萬');
  });

  it('formats values >= 10000 萬 as 億 with two decimals', () => {
    expect(fmtTotal(12800)).toBe('1.28 億');
    expect(fmtTotal(10000)).toBe('1.00 億');
  });
});

describe('fmtTotalShort', () => {
  it('drops the unit suffix but keeps the number', () => {
    expect(fmtTotalShort(8980)).toBe('8,980');
    expect(fmtTotalShort(12800)).toBe('1.28');
  });
});
