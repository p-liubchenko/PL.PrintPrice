export interface Money {
  amount: number;
  currencyId: string | null;
}

export interface Currency {
  id: string;
  code: string;
  value: number;
}

export interface Printer {
  id: string;
  name: string;
  averagePowerWatts: number;
  hourlyCostMoney: Money;
}

export enum FilamentType {
  PLA = 0,
  PLAPlus = 1,
  PETG = 2,
  ABS = 3,
  ASA = 4,
  TPU = 5,
}

export const FILAMENT_TYPE_LABELS: Record<FilamentType, string> = {
  [FilamentType.PLA]: 'PLA',
  [FilamentType.PLAPlus]: 'PLA+',
  [FilamentType.PETG]: 'PETG',
  [FilamentType.ABS]: 'ABS',
  [FilamentType.ASA]: 'ASA',
  [FilamentType.TPU]: 'TPU',
};

export interface FilamentMaterial {
  id: string;
  name: string;
  color: string;
  type: FilamentType;
  grade: string;
  amountKg: number;
  estimatedLengthMeters: number;
  averagePricePerKgMoney: Money;
}

export interface AppSettings {
  id: string;
  electricityPricePerKwhMoney: Money;
  fixedCostPerPrintMoney: Money;
  selectedPrinterId: string | null;
  operatingCurrencyId: string | null;
}

export enum PrintTransactionStatus {
  Completed = 0,
  Reverted = 1,
}

export interface PrintTransaction {
  id: string;
  createdAt: string;
  status: PrintTransactionStatus;
  materialId: string;
  materialNameSnapshot: string;
  filamentKg: number;
  estimatedMetersUsed: number;
  printerId: string;
  printerNameSnapshot: string;
  printHours: number;
  materialCost: number;
  electricityKwh: number;
  electricityCost: number;
  printerWearCost: number;
  fixedCost: number;
  extraFixedCost: number;
  note: string | null;
  revertedByTransactionId: string | null;
  revertedAt: string | null;
}

export enum StockTransactionType {
  SpoolPurchase = 0,
  ManualConsume = 1,
  PrintConsume = 2,
  Restock = 3,
}

export const STOCK_TRANSACTION_TYPE_LABELS: Record<StockTransactionType, string> = {
  [StockTransactionType.SpoolPurchase]: 'Spool Purchase',
  [StockTransactionType.ManualConsume]: 'Manual Consume',
  [StockTransactionType.PrintConsume]: 'Print Consume',
  [StockTransactionType.Restock]: 'Restock',
};

export interface StockTransaction {
  id: string;
  createdAt: string;
  type: StockTransactionType;
  materialId: string;
  materialNameSnapshot: string;
  kgDelta: number;
  metersDelta: number;
  note: string | null;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  username: string;
}

export interface PrintCostResult {
  filamentKg: number;
  estimatedMetersUsed: number;
  materialCost: number;
  electricityKwh: number;
  electricityCost: number;
  printerWearCost: number;
  fixedCost: number;
  extraFixedCost: number;
  total: number;
}
