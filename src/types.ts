import type { Transaction } from './data/mock';

export type FilterGroup = 'type' | 'layout' | 'trade';
export type Filters = Record<FilterGroup, string[]>;

export interface SortState {
  key: keyof Transaction;
  dir: 'asc' | 'desc';
}

export type ViewMode = 'card' | 'table';
