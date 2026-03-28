import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppSettings } from '../models';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly base = '/api/settings';

  constructor(private http: HttpClient) {}

  get(): Observable<AppSettings> {
    return this.http.get<AppSettings>(this.base);
  }

  update(settings: AppSettings): Observable<void> {
    return this.http.put<void>(this.base, settings);
  }
}
