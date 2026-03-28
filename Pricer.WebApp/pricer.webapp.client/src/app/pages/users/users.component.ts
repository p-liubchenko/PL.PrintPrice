import { Component, OnInit } from '@angular/core';
import { UsersService, UserDto } from '../../services/users.service';

@Component({
  selector: 'app-users',
  standalone: false,
  templateUrl: './users.component.html',
})
export class UsersComponent implements OnInit {
  users: UserDto[] = [];
  loading = true;
  error = '';

  showCreateForm = false;
  createForm = { username: '', tempPassword: '' };

  resetTarget: UserDto | null = null;
  resetPassword = '';

  constructor(private usersService: UsersService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.usersService.getAll().subscribe({
      next: list => { this.users = list; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  create(): void {
    this.error = '';
    this.usersService.create(this.createForm.username, this.createForm.tempPassword).subscribe({
      next: () => { this.showCreateForm = false; this.createForm = { username: '', tempPassword: '' }; this.load(); },
      error: (err: any) => { this.error = Array.isArray(err?.error) ? err.error.join(' ') : (err?.error ?? 'Failed to create user.'); },
    });
  }

  openReset(user: UserDto): void {
    this.resetTarget = user;
    this.resetPassword = '';
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
}
