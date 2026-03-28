import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FilamentMaterial, FilamentType } from '../models';

export interface AddSpoolRequest {
  material: FilamentMaterial;
  totalPrice: number;
  operatingCurrencyId: string | null;
}

export interface RestockRequest {
  addKg: number;
  addMeters: number;
  addTotalPrice: number;
  operatingCurrencyId: string | null;
}

export interface ConsumeRequest {
  kg: number;
  meters: number;
}

@Injectable({ providedIn: 'root' })
export class MaterialsService {
  private readonly base = '/api/materials';

  constructor(private http: HttpClient) {}

  getAll(): Observable<FilamentMaterial[]> {
    return this.http.get<FilamentMaterial[]>(this.base);
  }

  addSpool(req: AddSpoolRequest): Observable<FilamentMaterial> {
    return this.http.post<FilamentMaterial>(`${this.base}/add-spool`, req);
  }

  upsert(material: FilamentMaterial): Observable<void> {
    return this.http.put<void>(`${this.base}/${material.id}`, material);
  }

  restock(id: string, req: RestockRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/restock`, req);
  }

  consume(id: string, req: ConsumeRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/consume`, req);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
