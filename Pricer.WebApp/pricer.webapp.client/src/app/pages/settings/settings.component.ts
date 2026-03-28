import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { SettingsService } from '../../services/settings.service';
import { PrintersService } from '../../services/printers.service';
import { CurrenciesService } from '../../services/currencies.service';
import { AppSettings, Printer, Currency } from '../../models';

@Component({
  selector: 'app-settings',
  standalone: false,
  templateUrl: './settings.component.html',
})
export class SettingsComponent implements OnInit {
  settings: AppSettings | null = null;
  printers: Printer[] = [];
  currencies: Currency[] = [];
  loading = true;
  saved = false;
  error = '';

  form = {
    electricityAmount: 0,
    fixedCostAmount: 0,
    selectedPrinterId: null as string | null,
    operatingCurrencyId: null as string | null,
  };

  constructor(
    private settingsService: SettingsService,
    private printersService: PrintersService,
    private currenciesService: CurrenciesService,
  ) {}

  ngOnInit(): void {
    forkJoin({
      settings: this.settingsService.get(),
      printers: this.printersService.getAll(),
      currencies: this.currenciesService.getAll(),
    }).subscribe({
      next: res => {
        this.settings = res.settings;
        this.printers = res.printers;
        this.currencies = res.currencies;
        this.form = {
          electricityAmount: res.settings.electricityPricePerKwhMoney.amount,
          fixedCostAmount: res.settings.fixedCostPerPrintMoney.amount,
          selectedPrinterId: res.settings.selectedPrinterId,
          operatingCurrencyId: res.settings.operatingCurrencyId,
        };
        this.loading = false;
      },
      error: () => { this.loading = false; },
    });
  }

  save(): void {
    if (!this.settings) return;
    const updated: AppSettings = {
      ...this.settings,
      electricityPricePerKwhMoney: { amount: this.form.electricityAmount, currencyId: null },
      fixedCostPerPrintMoney: { amount: this.form.fixedCostAmount, currencyId: null },
      selectedPrinterId: this.form.selectedPrinterId || null,
      operatingCurrencyId: this.form.operatingCurrencyId || null,
    };
    this.settingsService.update(updated).subscribe({
      next: () => { this.saved = true; setTimeout(() => this.saved = false, 2500); },
      error: (err: any) => { this.error = err?.error ?? 'Failed to save settings.'; },
    });
  }
}
