import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Currency } from '../models';

@Injectable({ providedIn: 'root' })
export class CurrenciesService {
  private readonly base = '/api/currencies';

  constructor(private http: HttpClient) {}

  getAll(): Observable<Currency[]> {
    return this.http.get<Currency[]>(this.base);
  }

  add(currency: Omit<Currency, 'id'>): Observable<void> {
    return this.http.post<void>(this.base, { ...currency, id: '00000000-0000-0000-0000-000000000000' });
  }

  upsert(currency: Currency): Observable<void> {
    return this.http.put<void>(`${this.base}/${currency.id}`, currency);
  }

  setBase(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/set-base`, {});
  }

  setOperating(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/set-operating`, {});
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
