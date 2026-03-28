import { Component, OnInit } from '@angular/core';
import { CurrenciesService } from '../../services/currencies.service';
import { SettingsService } from '../../services/settings.service';
import { Currency } from '../../models';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-currencies',
  standalone: false,
  templateUrl: './currencies.component.html',
})
export class CurrenciesComponent implements OnInit {
  currencies: Currency[] = [];
  operatingCurrencyId: string | null = null;
  loading = true;
  error = '';

  showForm = false;
  editTarget: Currency | null = null;
  form = this.emptyForm();

  constructor(
    private currenciesService: CurrenciesService,
    private settingsService: SettingsService,
    public auth: AuthService,
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.settingsService.get().subscribe(s => {
      this.operatingCurrencyId = s.operatingCurrencyId;
      this.currenciesService.getAll().subscribe({
        next: list => { this.currencies = list; this.loading = false; },
        error: () => { this.loading = false; },
      });
    });
  }

  openAdd(): void {
    this.editTarget = null;
    this.form = this.emptyForm();
    this.showForm = true;
  }

  openEdit(c: Currency): void {
    this.editTarget = c;
    this.form = { code: c.code, value: c.value };
    this.showForm = true;
  }

  save(): void {
    const obs = this.editTarget
      ? this.currenciesService.upsert({ ...this.form, id: this.editTarget.id })
      : this.currenciesService.add(this.form);

    obs.subscribe({
      next: () => { this.showForm = false; this.load(); },
      error: (err: any) => { this.error = err?.error ?? 'Failed to save currency.'; },
    });
  }

  setBase(id: string): void {
    this.currenciesService.setBase(id).subscribe({
      next: () => this.load(),
      error: (err: any) => { this.error = err?.error ?? 'Failed.'; },
    });
  }

  setOperating(id: string): void {
    this.currenciesService.setOperating(id).subscribe({
      next: () => this.load(),
      error: (err: any) => { this.error = err?.error ?? 'Failed.'; },
    });
  }

  remove(id: string): void {
    if (!confirm('Delete this currency?')) return;
    this.currenciesService.remove(id).subscribe({
      next: () => this.load(),
      error: (err: any) => { this.error = err?.error ?? 'Cannot delete currency.'; },
    });
  }

  isBase(c: Currency): boolean { return c.value === 1; }

  private emptyForm() { return { code: '', value: 1 }; }
}
