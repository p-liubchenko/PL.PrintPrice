import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PrintTransaction, StockTransaction, PrintCostResult } from '../models';

export interface RecordPrintRequest {
  materialId: string;
  filamentGrams: number;
  printHours: number;
  extraFixedCost: number;
}

@Injectable({ providedIn: 'root' })
export class TransactionsService {
  private readonly base = '/api/transactions';

  constructor(private http: HttpClient) {}

  getPrints(): Observable<PrintTransaction[]> {
    return this.http.get<PrintTransaction[]>(`${this.base}/prints`);
  }

  getStock(): Observable<StockTransaction[]> {
    return this.http.get<StockTransaction[]>(`${this.base}/stock`);
  }

  recordPrint(req: RecordPrintRequest): Observable<PrintCostResult> {
    return this.http.post<PrintCostResult>(`${this.base}/record-print`, req);
  }

  revertPrint(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/prints/${id}/revert`, {});
  }

  deletePrint(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/prints/${id}`);
  }
}
