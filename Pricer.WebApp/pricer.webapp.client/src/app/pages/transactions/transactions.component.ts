import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { TransactionsService, RecordPrintRequest } from '../../services/transactions.service';
import { MaterialsService } from '../../services/materials.service';
import { SettingsService } from '../../services/settings.service';
import {
  PrintTransaction, StockTransaction, FilamentMaterial, AppSettings,
  PrintTransactionStatus, STOCK_TRANSACTION_TYPE_LABELS, StockTransactionType,
} from '../../models';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-transactions',
  standalone: false,
  templateUrl: './transactions.component.html',
})
export class TransactionsComponent implements OnInit {
  prints: PrintTransaction[] = [];
  stock: StockTransaction[] = [];
  materials: FilamentMaterial[] = [];
  settings: AppSettings | null = null;
  tab: 'prints' | 'stock' = 'prints';
  loading = true;
  error = '';

  showRecordForm = false;
  recordForm: RecordPrintRequest = { materialId: '', filamentGrams: 0, printHours: 0, extraFixedCost: 0 };
  recordResult: { total: number; materialCost: number; electricityCost: number; printerWearCost: number; fixedCost: number } | null = null;

  PrintTransactionStatus = PrintTransactionStatus;
  stockTypeLabel = (t: StockTransactionType) => STOCK_TRANSACTION_TYPE_LABELS[t] ?? String(t);

  constructor(
    private transactionsService: TransactionsService,
    private materialsService: MaterialsService,
    private settingsService: SettingsService,
    public auth: AuthService,
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    forkJoin({
      prints: this.transactionsService.getPrints(),
      stock: this.transactionsService.getStock(),
      materials: this.materialsService.getAll(),
      settings: this.settingsService.get(),
    }).subscribe({
      next: res => {
        this.prints = res.prints.sort((a, b) => b.createdAt.localeCompare(a.createdAt));
        this.stock = res.stock.sort((a, b) => b.createdAt.localeCompare(a.createdAt));
        this.materials = res.materials;
        this.settings = res.settings;
        this.loading = false;
      },
      error: () => { this.loading = false; },
    });
  }

  openRecord(): void {
    this.recordResult = null;
    this.recordForm = {
      materialId: this.materials[0]?.id ?? '',
      filamentGrams: 0,
      printHours: 0,
      extraFixedCost: 0,
    };
    this.showRecordForm = true;
  }

  recordPrint(): void {
    this.error = '';
    this.transactionsService.recordPrint(this.recordForm).subscribe({
      next: res => {
        this.recordResult = res;
        this.showRecordForm = false;
        this.load();
      },
      error: (err: any) => { this.error = err?.error ?? 'Failed to record print.'; },
    });
  }

  revert(id: string): void {
    if (!confirm('Revert this print? Stock will be restored.')) return;
    this.transactionsService.revertPrint(id).subscribe({
      next: () => this.load(),
      error: (err: any) => { this.error = err?.error ?? 'Revert failed.'; },
    });
  }

  delete(id: string): void {
    if (!confirm('Delete this print record? This cannot be undone.')) return;
    this.transactionsService.deletePrint(id).subscribe({
      next: () => this.load(),
      error: (err: any) => { this.error = err?.error ?? 'Delete failed.'; },
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleString();
  }
}
