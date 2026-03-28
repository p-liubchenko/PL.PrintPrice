import { Component, OnInit } from '@angular/core';
import { MaterialsService } from '../../services/materials.service';
import { SettingsService } from '../../services/settings.service';
import { CurrenciesService } from '../../services/currencies.service';
import { FilamentMaterial, FilamentType, FILAMENT_TYPE_LABELS } from '../../models';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-materials',
  standalone: false,
  templateUrl: './materials.component.html',
})
export class MaterialsComponent implements OnInit {
  materials: FilamentMaterial[] = [];
  operatingCurrencyId: string | null = null;
  loading = true;
  error = '';

  filamentTypes = Object.entries(FILAMENT_TYPE_LABELS).map(([k, v]) => ({
    value: Number(k) as FilamentType,
    label: v,
  }));

  showAddForm = false;
  addForm = this.emptyAddForm();

  restockTarget: FilamentMaterial | null = null;
  restockForm = { addKg: 0, addMeters: 0, addTotalPrice: 0 };

  consumeTarget: FilamentMaterial | null = null;
  consumeForm = { kg: 0, meters: 0 };

  constructor(
    private materialsService: MaterialsService,
    private settingsService: SettingsService,
    private currenciesService: CurrenciesService,
    public auth: AuthService,
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.settingsService.get().subscribe(s => {
      this.operatingCurrencyId = s.operatingCurrencyId;
      this.materialsService.getAll().subscribe({
        next: list => { this.materials = list; this.loading = false; },
        error: () => { this.loading = false; },
      });
    });
  }

  openAdd(): void {
    this.addForm = this.emptyAddForm();
    this.showAddForm = true;
  }

  addSpool(): void {
    const material: FilamentMaterial = {
      id: '00000000-0000-0000-0000-000000000000',
      name: this.addForm.name,
      color: this.addForm.color,
      type: this.addForm.type,
      grade: this.addForm.grade,
      amountKg: this.addForm.amountKg,
      estimatedLengthMeters: this.addForm.estimatedLengthMeters,
      averagePricePerKgMoney: { amount: 0, currencyId: null },
    };
    this.materialsService.addSpool({
      material,
      totalPrice: this.addForm.totalPrice,
      operatingCurrencyId: this.operatingCurrencyId,
    }).subscribe({
      next: () => { this.showAddForm = false; this.load(); },
      error: (err: any) => { this.error = err?.error ?? 'Failed to add spool.'; },
    });
  }

  openRestock(m: FilamentMaterial): void {
    this.restockTarget = m;
    this.restockForm = { addKg: 0, addMeters: 0, addTotalPrice: 0 };
  }

  restock(): void {
    if (!this.restockTarget) return;
    this.materialsService.restock(this.restockTarget.id, {
      ...this.restockForm,
      operatingCurrencyId: this.operatingCurrencyId,
    }).subscribe({
      next: () => { this.restockTarget = null; this.load(); },
      error: (err: any) => { this.error = err?.error ?? 'Restock failed.'; },
    });
  }

  openConsume(m: FilamentMaterial): void {
    this.consumeTarget = m;
    this.consumeForm = { kg: 0, meters: 0 };
  }

  consume(): void {
    if (!this.consumeTarget) return;
    this.materialsService.consume(this.consumeTarget.id, this.consumeForm).subscribe({
      next: () => { this.consumeTarget = null; this.load(); },
      error: (err: any) => { this.error = err?.error ?? 'Consume failed.'; },
    });
  }

  remove(id: string): void {
    if (!confirm('Delete this material?')) return;
    this.materialsService.remove(id).subscribe({
      next: () => this.load(),
      error: (err: any) => { this.error = err?.error ?? 'Cannot delete material.'; },
    });
  }

  filamentLabel(type: FilamentType): string {
    return FILAMENT_TYPE_LABELS[type] ?? String(type);
  }

  private emptyAddForm() {
    return {
      name: '', color: '', type: FilamentType.PLA, grade: '',
      amountKg: 1, estimatedLengthMeters: 330, totalPrice: 0,
    };
  }
}
