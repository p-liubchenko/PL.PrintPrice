import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RoleDto {
  id: string;
  name: string;
  permissions: string[];
  isSystem: boolean;
}

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly base = '/api/roles';

  constructor(private http: HttpClient) {}

  getAll(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>(this.base);
  }

  getAllPermissions(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/permissions`);
  }

  create(name: string, permissions: string[]): Observable<RoleDto> {
    return this.http.post<RoleDto>(this.base, { name, permissions });
  }

  update(id: string, permissions: string[]): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, { permissions });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
