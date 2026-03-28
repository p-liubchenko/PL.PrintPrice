import { Component, OnInit } from '@angular/core';
import { UsersService, UserDto } from '../../services/users.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-users',
  standalone: false,
  templateUrl: './users.component.html',
})
export class UsersComponent implements OnInit {
  users: UserDto[] = [];
  availableRoles: string[] = [];
  loading = true;
  error = '';
  success = '';

  showCreateForm = false;
  createForm = { username: '', tempPassword: '' };

  resetTarget: UserDto | null = null;
  resetPassword = '';

  roleTarget: UserDto | null = null;
  roleToAdd = '';

  constructor(
    private usersService: UsersService,
    public auth: AuthService,
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.usersService.getAll().subscribe({
      next: list => {
        this.users = list;
        this.loading = false;
      },
      error: () => { this.loading = false; },
    });
    this.usersService.getAvailableRoles().subscribe({
      next: roles => { this.availableRoles = roles; },
    });
  }

  create(): void {
    this.error = '';
    this.usersService.create(this.createForm.username, this.createForm.tempPassword).subscribe({
      next: () => {
        this.showCreateForm = false;
        this.createForm = { username: '', tempPassword: '' };
        this.load();
      },
      error: (err: any) => {
        this.error = Array.isArray(err?.error) ? err.error.join(' ') : (err?.error ?? 'Failed to create user.');
      },
    });
  }

  openReset(user: UserDto): void {
    this.resetTarget = user;
    this.roleTarget = null;
    this.resetPassword = '';
    this.error = '';
  }

  doReset(): void {
    if (!this.resetTarget) return;
    this.usersService.resetPassword(this.resetTarget.id, this.resetPassword).subscribe({
      next: () => { this.resetTarget = null; this.load(); },
      error: (err: any) => { this.error = err?.error ?? 'Reset failed.'; },
    });
  }

  delete(user: UserDto): void {
    if (!confirm(`Delete user "${user.username}"? This cannot be undone.`)) return;
    this.usersService.delete(user.id).subscribe({
      next: () => this.load(),
      error: (err: any) => { this.error = err?.error ?? 'Cannot delete user.'; },
    });
  }

  // ── Role management ───────────────────────────────────────────────────

  openRoles(user: UserDto): void {
    this.roleTarget = user;
    this.resetTarget = null;
    this.roleToAdd = '';
    this.error = '';
    this.success = '';
  }

  get unassignedRoles(): string[] {
    if (!this.roleTarget) return [];
    return this.availableRoles.filter(r => !this.roleTarget!.roles.includes(r));
  }

  assignRole(): void {
    if (!this.roleTarget || !this.roleToAdd) return;
    this.usersService.assignRole(this.roleTarget.id, this.roleToAdd).subscribe({
      next: () => {
        this.roleTarget!.roles = [...this.roleTarget!.roles, this.roleToAdd].sort();
        this.success = `Role "${this.roleToAdd}" assigned.`;
        this.roleToAdd = '';
      },
      error: (err: any) => { this.error = err?.error ?? 'Failed to assign role.'; },
    });
  }

  removeRole(roleName: string): void {
    if (!this.roleTarget) return;
    this.usersService.removeRole(this.roleTarget.id, roleName).subscribe({
      next: () => {
        this.roleTarget!.roles = this.roleTarget!.roles.filter(r => r !== roleName);
        this.success = `Role "${roleName}" removed.`;
      },
      error: (err: any) => { this.error = err?.error ?? 'Failed to remove role.'; },
    });
  }
}
