// 台中市實價登錄 — mock 資料集
// 資料來源：內政部不動產交易實價查詢服務網（模擬）

export interface District {
  id: string;
  name: string;
  zone: string;
  avg: number;
  change: number;
  vol: number;
}

export interface Transaction {
  id: string;
  community: string;
  district: string;
  road: string;
  section: string;
  type: string;
  total: number;
  unit: number;
  ping: number;
  floor: string;
  age: number;
  layout: string;
  rooms: number;
  trade: string;
  date: string;
  source: string;
  crawled: string;
  lat: number;
  lng: number;
  parking: string;
}

export type CrawlStatus = 'done' | 'running' | 'queued' | 'error';

export interface CrawlTask {
  id: string;
  district: string;
  status: CrawlStatus;
  records: number;
  lastRun: string;
  next: string;
  duration: string;
}

// 行政區（含重劃區 / 熱門生活圈）
export const TC_DISTRICTS: District[] = [
  { id: 'xitun',  name: '西屯區', zone: '七期 / 單元二',     avg: 42.8, change: +3.2, vol: 1284 },
  { id: 'beitun', name: '北屯區', zone: '十四期 / 水湳',      avg: 34.1, change: +5.1, vol: 1607 },
  { id: 'nantun', name: '南屯區', zone: '單元三 / 黎明',      avg: 38.6, change: +2.4, vol: 1043 },
  { id: 'west',   name: '西區',   zone: '草悟道 / 美術館',    avg: 40.2, change: +1.8, vol: 612 },
  { id: 'north',  name: '北區',   zone: '一中 / 中國醫',      avg: 31.5, change: +0.9, vol: 538 },
  { id: 'east',   name: '東區',   zone: '帝國糖廠 / 火車站',  avg: 28.7, change: +4.3, vol: 421 },
  { id: 'south',  name: '南區',   zone: '中興大學 / 文心南',  avg: 29.9, change: +3.6, vol: 489 },
  { id: 'central',name: '中區',   zone: '舊城 / 綠空鐵道',    avg: 24.3, change: -0.4, vol: 156 },
  { id: 'dali',   name: '大里區', zone: '國光 / 軟體園區',    avg: 26.4, change: +2.1, vol: 734 },
  { id: 'taiping',name: '太平區', zone: '坪林 / 新光重劃',    avg: 24.8, change: +1.5, vol: 658 },
  { id: 'fengyuan',name: '豐原區',zone: '葫蘆墩 / 車站',      avg: 22.1, change: +0.7, vol: 402 },
  { id: 'wuri',   name: '烏日區', zone: '高鐵特區',          avg: 27.6, change: +6.2, vol: 388 },
];

export const BUILDING_TYPES = ['住宅大樓', '華廈', '電梯華廈', '公寓', '透天厝', '套房'];
export const LAYOUTS = ['1房1廳', '2房2廳', '3房2廳', '4房2廳', '套房'];
export const TRADE_TYPES = ['成屋', '預售屋'];

// 社區 / 物件成交紀錄
export const TRANSACTIONS: Transaction[] = [
  { id: 'T0001', community: '聯聚理仁',   district: '西屯區', road: '市政路', section: '七期重劃區', type: '住宅大樓', total: 8980, unit: 86.4, ping: 103.9, floor: '18/26', age: 6,  layout: '4房2廳', rooms: 4, trade: '成屋',   date: '2026-05-12', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.161, lng: 120.642, parking: '坡道平面' },
  { id: 'T0002', community: '惠宇仁愛',   district: '西屯區', road: '惠中路', section: '七期重劃區', type: '住宅大樓', total: 4280, unit: 58.2, ping: 73.5,  floor: '11/22', age: 9,  layout: '3房2廳', rooms: 3, trade: '成屋',   date: '2026-05-08', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.155, lng: 120.645, parking: '坡道平面' },
  { id: 'T0003', community: '由鉅大恆',   district: '西屯區', road: '河南路', section: '單元二',     type: '住宅大樓', total: 3650, unit: 49.8, ping: 73.3,  floor: '8/15',  age: 4,  layout: '3房2廳', rooms: 3, trade: '成屋',   date: '2026-05-15', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.179, lng: 120.625, parking: '坡道機械' },
  { id: 'T0004', community: '富宇九大',   district: '北屯區', road: '崇德路', section: '十四期重劃', type: '住宅大樓', total: 2180, unit: 36.5, ping: 59.7,  floor: '6/14',  age: 2,  layout: '2房2廳', rooms: 2, trade: '成屋',   date: '2026-05-19', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.183, lng: 120.689, parking: '坡道平面' },
  { id: 'T0005', community: '惠宇澄品',   district: '北屯區', road: '環中路', section: '水湳經貿園區', type: '住宅大樓', total: 2980, unit: 41.2, ping: 72.3, floor: '14/24', age: 1,  layout: '3房2廳', rooms: 3, trade: '預售屋', date: '2026-05-21', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.193, lng: 120.651, parking: '坡道平面' },
  { id: 'T0006', community: '太子昀',     district: '北屯區', road: '后庄路', section: '北屯機捷',   type: '電梯華廈', total: 1480, unit: 28.9, ping: 51.2,  floor: '5/11',  age: 3,  layout: '2房2廳', rooms: 2, trade: '成屋',   date: '2026-05-05', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.201, lng: 120.704, parking: '坡道機械' },
  { id: 'T0007', community: '寶輝秋紅谷', district: '西屯區', road: '朝富路', section: '七期重劃區', type: '住宅大樓', total: 12800, unit: 98.6, ping: 129.8, floor: '29/32', age: 12, layout: '4房2廳', rooms: 4, trade: '成屋',   date: '2026-04-28', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.165, lng: 120.638, parking: '坡道平面' },
  { id: 'T0008', community: '勤美璞真',   district: '西區',   road: '英才路', section: '草悟道',     type: '住宅大樓', total: 5640, unit: 64.3, ping: 87.7,  floor: '16/20', age: 8,  layout: '3房2廳', rooms: 3, trade: '成屋',   date: '2026-05-10', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.148, lng: 120.665, parking: '坡道平面' },
  { id: 'T0009', community: '達麗世界中心',district: '西區',   road: '公益路', section: '美術館',     type: '住宅大樓', total: 3120, unit: 47.5, ping: 65.7,  floor: '9/18',  age: 7,  layout: '2房2廳', rooms: 2, trade: '成屋',   date: '2026-05-17', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.143, lng: 120.658, parking: '坡道平面' },
  { id: 'T0010', community: '總太东方帝國',district: '南屯區', road: '惠中路', section: '單元三',     type: '住宅大樓', total: 2760, unit: 39.4, ping: 70.1,  floor: '12/18', age: 5,  layout: '3房2廳', rooms: 3, trade: '成屋',   date: '2026-05-03', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.137, lng: 120.628, parking: '坡道平面' },
  { id: 'T0011', community: '惠宇觀韻',   district: '南屯區', road: '黎明路', section: '黎明重劃',   type: '住宅大樓', total: 2240, unit: 35.8, ping: 62.6,  floor: '7/15',  age: 4,  layout: '3房2廳', rooms: 3, trade: '成屋',   date: '2026-05-20', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.131, lng: 120.633, parking: '坡道機械' },
  { id: 'T0012', community: '精銳PARK',   district: '南屯區', road: '公益路', section: '文心森林',   type: '住宅大樓', total: 4480, unit: 55.1, ping: 81.3,  floor: '20/25', age: 6,  layout: '4房2廳', rooms: 4, trade: '成屋',   date: '2026-04-30', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.140, lng: 120.640, parking: '坡道平面' },
  { id: 'T0013', community: '一中商圈套房',district: '北區',   road: '育才街', section: '一中商圈',   type: '套房',     total: 528,  unit: 26.4, ping: 20.0,  floor: '8/12',  age: 11, layout: '套房',   rooms: 1, trade: '成屋',   date: '2026-05-14', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.150, lng: 120.684, parking: '無' },
  { id: 'T0014', community: '城市風laze', district: '北區',   road: '太原路', section: '中國醫',     type: '電梯華廈', total: 1380, unit: 30.1, ping: 45.8,  floor: '6/13',  age: 5,  layout: '2房2廳', rooms: 2, trade: '成屋',   date: '2026-05-06', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.166, lng: 120.681, parking: '坡道機械' },
  { id: 'T0015', community: '帝國糖廠特區',district: '東區',   road: '復興路', section: '帝國糖廠',   type: '住宅大樓', total: 1620, unit: 29.3, ping: 55.3,  floor: '10/17', age: 2,  layout: '3房2廳', rooms: 3, trade: '預售屋', date: '2026-05-22', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.137, lng: 120.692, parking: '坡道平面' },
  { id: 'T0016', community: '興大學區寓', district: '南區',   road: '國光路', section: '中興大學',   type: '華廈',     total: 980,  unit: 27.2, ping: 36.0,  floor: '4/7',   age: 18, layout: '2房2廳', rooms: 2, trade: '成屋',   date: '2026-05-01', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.123, lng: 120.675, parking: '無' },
  { id: 'T0017', community: '國光花園',   district: '大里區', road: '國光路', section: '國光重劃',   type: '住宅大樓', total: 1280, unit: 26.8, ping: 47.8,  floor: '9/15',  age: 3,  layout: '3房2廳', rooms: 3, trade: '成屋',   date: '2026-05-11', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.099, lng: 120.677, parking: '坡道平面' },
  { id: 'T0018', community: '軟體園區寓', district: '大里區', road: '科技路', section: '軟體園區',   type: '電梯華廈', total: 1050, unit: 24.9, ping: 42.2,  floor: '7/12',  age: 4,  layout: '2房2廳', rooms: 2, trade: '成屋',   date: '2026-04-26', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.105, lng: 120.690, parking: '坡道機械' },
  { id: 'T0019', community: '坪林綠意',   district: '太平區', road: '宜昌路', section: '坪林重劃',   type: '住宅大樓', total: 1180, unit: 25.3, ping: 46.6,  floor: '8/14',  age: 2,  layout: '3房2廳', rooms: 3, trade: '預售屋', date: '2026-05-18', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.135, lng: 120.718, parking: '坡道平面' },
  { id: 'T0020', community: '高鐵之心',   district: '烏日區', road: '高鐵五路', section: '高鐵特區',  type: '住宅大樓', total: 1560, unit: 28.4, ping: 54.9,  floor: '13/20', age: 1,  layout: '3房2廳', rooms: 3, trade: '預售屋', date: '2026-05-23', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.111, lng: 120.615, parking: '坡道平面' },
  { id: 'T0021', community: '葫蘆墩之心', district: '豐原區', road: '中正路', section: '豐原車站',   type: '電梯華廈', total: 880,  unit: 21.7, ping: 40.6,  floor: '6/11',  age: 5,  layout: '2房2廳', rooms: 2, trade: '成屋',   date: '2026-05-09', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.254, lng: 120.723, parking: '坡道機械' },
  { id: 'T0022', community: '聯聚方庭',   district: '西屯區', road: '惠來路', section: '七期重劃區', type: '住宅大樓', total: 9650, unit: 89.1, ping: 108.3, floor: '22/28', age: 7,  layout: '4房2廳', rooms: 4, trade: '成屋',   date: '2026-04-24', source: '內政部', crawled: '2026-05-28 03:14', lat: 24.158, lng: 120.640, parking: '坡道平面' },
];

// 單一社區的歷史月成交均價（單價 萬/坪）— 供詳情頁趨勢圖
export const COMMUNITY_TREND: Record<string, number[]> = {
  '聯聚理仁': [78.2, 79.5, 80.1, 81.8, 82.4, 83.9, 84.2, 85.0, 85.6, 86.0, 86.2, 86.4],
};

// 全市月成交均價（單價 萬/坪）近 12 月 — 供分析頁
export const CITY_TREND = {
  months: ['6月','7月','8月','9月','10月','11月','12月','1月','2月','3月','4月','5月'],
  unit:   [30.1, 30.6, 31.2, 31.5, 32.0, 32.4, 32.9, 33.1, 33.5, 34.0, 34.4, 34.8],
  volume: [3820, 3640, 3910, 4120, 4350, 4180, 3760, 3290, 3540, 4010, 4280, 4460],
};

// 價格分布（單價區間 → 件數）— 供分析頁直方圖
export const PRICE_DISTRIBUTION = [
  { range: '~20', count: 142 },
  { range: '20-30', count: 1860 },
  { range: '30-40', count: 2340 },
  { range: '40-50', count: 1280 },
  { range: '50-60', count: 640 },
  { range: '60-80', count: 410 },
  { range: '80+', count: 186 },
];

// 資料來源 / 爬蟲任務狀態
export const CRAWL_TASKS: CrawlTask[] = [
  { id: 'C01', district: '西屯區', status: 'done',    records: 1284, lastRun: '2026-05-28 03:14', next: '2026-05-29 03:00', duration: '4分12秒' },
  { id: 'C02', district: '北屯區', status: 'done',    records: 1607, lastRun: '2026-05-28 03:18', next: '2026-05-29 03:00', duration: '5分02秒' },
  { id: 'C03', district: '南屯區', status: 'done',    records: 1043, lastRun: '2026-05-28 03:23', next: '2026-05-29 03:00', duration: '3分48秒' },
  { id: 'C04', district: '西區',   status: 'running', records: 612,  lastRun: '進行中…',         next: '—',                duration: '—' },
  { id: 'C05', district: '北區',   status: 'queued',  records: 538,  lastRun: '2026-05-27 03:21', next: '排隊中',            duration: '—' },
  { id: 'C06', district: '東區',   status: 'done',    records: 421,  lastRun: '2026-05-28 03:31', next: '2026-05-29 03:00', duration: '2分10秒' },
  { id: 'C07', district: '南區',   status: 'done',    records: 489,  lastRun: '2026-05-28 03:34', next: '2026-05-29 03:00', duration: '2分38秒' },
  { id: 'C08', district: '大里區', status: 'error',   records: 0,    lastRun: '2026-05-28 03:40', next: '待重試',            duration: '逾時' },
];
