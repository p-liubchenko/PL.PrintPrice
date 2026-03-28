import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Printer } from '../models';

@Injectable({ providedIn: 'root' })
export class PrintersService {
  private readonly base = '/api/printers';

  constructor(private http: HttpClient) {}

  getAll(): Observable<Printer[]> {
    return this.http.get<Printer[]>(this.base);
  }

  add(printer: Omit<Printer, 'id'>): Observable<Printer> {
    return this.http.post<Printer>(this.base, { ...printer, id: '00000000-0000-0000-0000-000000000000' });
  }

  upsert(printer: Printer): Observable<void> {
    return this.http.put<void>(`${this.base}/${printer.id}`, printer);
  }

  select(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/select`, {});
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
