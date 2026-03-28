import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { PrintersService } from '../../services/printers.service';
import { Printer } from '../../models';
import { SettingsService } from '../../services/settings.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-printers',
  standalone: false,
  templateUrl: './printers.component.html',
})
export class PrintersComponent implements OnInit {
  printers: Printer[] = [];
  selectedPrinterId: string | null = null;
  loading = true;
  error = '';

  showForm = false;
  editTarget: Printer | null = null;
  form = this.emptyForm();

  constructor(
    private printersService: PrintersService,
    private settingsService: SettingsService,
    public auth: AuthService,
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.settingsService.get().subscribe(s => {
      this.selectedPrinterId = s.selectedPrinterId;
      this.printersService.getAll().subscribe({
        next: list => { this.printers = list; this.loading = false; },
        error: () => { this.loading = false; },
      });
    });
  }

  openAdd(): void {
    this.editTarget = null;
    this.form = this.emptyForm();
    this.showForm = true;
  }

  openEdit(p: Printer): void {
    this.editTarget = p;
    this.form = {
      name: p.name,
      averagePowerWatts: p.averagePowerWatts,
      hourlyCostAmount: p.hourlyCostMoney.amount,
    };
    this.showForm = true;
  }

  save(): void {
    const data: Omit<Printer, 'id'> = {
      name: this.form.name,
      averagePowerWatts: this.form.averagePowerWatts,
      hourlyCostMoney: { amount: this.form.hourlyCostAmount, currencyId: null },
    };

    const obs: Observable<unknown> = this.editTarget
      ? this.printersService.upsert({ ...data, id: this.editTarget.id })
      : this.printersService.add(data);

    obs.subscribe({
      next: () => { this.showForm = false; this.load(); },
      error: (err: any) => { this.error = err?.error ?? 'Failed to save printer.'; },
    });
  }

  select(id: string): void {
    this.printersService.select(id).subscribe(() => this.load());
  }

  remove(id: string): void {
    if (!confirm('Delete this printer?')) return;
    this.printersService.remove(id).subscribe({
      next: () => this.load(),
      error: (err: any) => { this.error = err?.error ?? 'Cannot delete printer.'; },
    });
  }

  private emptyForm() {
    return { name: '', averagePowerWatts: 0, hourlyCostAmount: 0 };
  }
}
