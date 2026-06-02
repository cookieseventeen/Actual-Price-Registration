import { describe, it, expect } from 'vitest';
import { filterTransactions, sortTransactions } from './query';
import { TRANSACTIONS } from '../data/mock';
import type { Filters } from '../types';

const EMPTY: Filters = { type: [], layout: [], trade: [] };

describe('filterTransactions', () => {
  it('returns everything with no criteria', () => {
    expect(filterTransactions(TRANSACTIONS, { filters: EMPTY })).toHaveLength(TRANSACTIONS.length);
  });

  it('filters by district', () => {
    const rows = filterTransactions(TRANSACTIONS, { district: '西屯區', filters: EMPTY });
    expect(rows.length).toBeGreaterThan(0);
    expect(rows.every(r => r.district === '西屯區')).toBe(true);
  });

  it('matches the query against community / road / section / district', () => {
    expect(filterTransactions(TRANSACTIONS, { query: '聯聚', filters: EMPTY }).every(r => r.community.includes('聯聚'))).toBe(true);
    // road match
    expect(filterTransactions(TRANSACTIONS, { query: '市政路', filters: EMPTY }).some(r => r.road === '市政路')).toBe(true);
    // trims whitespace
    expect(filterTransactions(TRANSACTIONS, { query: '  七期  ', filters: EMPTY }).length).toBeGreaterThan(0);
  });

  it('applies type / layout / trade filters as AND across groups, OR within a group', () => {
    const rows = filterTransactions(TRANSACTIONS, {
      filters: { type: ['住宅大樓'], layout: ['3房2廳', '4房2廳'], trade: ['成屋'] },
    });
    expect(rows.every(r => r.type === '住宅大樓' && ['3房2廳', '4房2廳'].includes(r.layout) && r.trade === '成屋')).toBe(true);
  });

  it('returns no rows when filters exclude everything', () => {
    expect(filterTransactions(TRANSACTIONS, { district: '西屯區', filters: { ...EMPTY, trade: ['預售屋'] } })).toHaveLength(0);
  });
});

describe('sortTransactions', () => {
  it('sorts numeric columns ascending and descending', () => {
    const asc = sortTransactions(TRANSACTIONS, { key: 'total', dir: 'asc' });
    const desc = sortTransactions(TRANSACTIONS, { key: 'total', dir: 'desc' });
    expect(asc[0].total).toBeLessThanOrEqual(asc[asc.length - 1].total);
    expect(desc[0].total).toBeGreaterThanOrEqual(desc[desc.length - 1].total);
    expect(desc[0].total).toBe(asc[asc.length - 1].total);
  });

  it('does not mutate the input array', () => {
    const input = [...TRANSACTIONS];
    const snapshot = input.map(t => t.id);
    sortTransactions(input, { key: 'unit', dir: 'desc' });
    expect(input.map(t => t.id)).toEqual(snapshot);
  });

  it('sorts string columns with a stable locale comparison', () => {
    const byDate = sortTransactions(TRANSACTIONS, { key: 'date', dir: 'asc' });
    for (let i = 1; i < byDate.length; i++) {
      expect(byDate[i - 1].date <= byDate[i].date).toBe(true);
    }
  });
});
