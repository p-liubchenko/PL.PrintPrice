import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { PrintersService } from '../../services/printers.service';
import { MaterialsService } from '../../services/materials.service';
import { TransactionsService } from '../../services/transactions.service';
import { SettingsService } from '../../services/settings.service';
import { Printer, FilamentMaterial, AppSettings } from '../../models';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  printers: Printer[] = [];
  materials: FilamentMaterial[] = [];
  settings: AppSettings | null = null;
  printCount = 0;
  loading = true;

  constructor(
    private printersService: PrintersService,
    private materialsService: MaterialsService,
    private transactionsService: TransactionsService,
    private settingsService: SettingsService,
  ) {}

  ngOnInit(): void {
    forkJoin({
      printers: this.printersService.getAll(),
      materials: this.materialsService.getAll(),
      prints: this.transactionsService.getPrints(),
      settings: this.settingsService.get(),
    }).subscribe({
      next: res => {
        this.printers = res.printers;
        this.materials = res.materials;
        this.printCount = res.prints.length;
        this.settings = res.settings;
        this.loading = false;
      },
      error: () => { this.loading = false; },
    });
  }

  get selectedPrinter(): Printer | undefined {
    return this.printers.find(p => p.id === this.settings?.selectedPrinterId);
  }

  get totalStockKg(): number {
    return this.materials.reduce((s, m) => s + m.amountKg, 0);
  }
}
