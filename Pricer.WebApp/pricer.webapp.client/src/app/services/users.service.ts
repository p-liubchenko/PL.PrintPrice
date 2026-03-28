import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserDto {
  id: string;
  username: string;
  roles: string[];
  mustChangePassword: boolean;
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly base = '/api/users';

  constructor(private http: HttpClient) {}

  getAll(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(this.base);
  }

  create(username: string, tempPassword: string): Observable<{ id: string; userName: string }> {
    return this.http.post<{ id: string; userName: string }>(this.base, { username, tempPassword });
  }

  resetPassword(id: string, newTempPassword: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reset-password`, { newTempPassword });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  getAvailableRoles(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/roles`);
  }

  assignRole(userId: string, roleName: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${userId}/roles/${roleName}`, {});
  }

  removeRole(userId: string, roleName: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${userId}/roles/${roleName}`);
  }
}
