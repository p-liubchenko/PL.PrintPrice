import { Component } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-profile',
  standalone: false,
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css'],
})
export class ProfileComponent {
  username = '';
  permissions: string[] = [];

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  saving = false;
  error = '';
  success = '';

  constructor(private auth: AuthService, private router: Router) {
    this.username = auth.getUsername() ?? '';
    this.permissions = auth.getPermissions().sort();
  }

  get passwordMismatch(): boolean {
    return this.newPassword.length > 0 && this.confirmPassword.length > 0
      && this.newPassword !== this.confirmPassword;
  }

  changePassword(): void {
    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }
    this.saving = true;
    this.error = '';
    this.success = '';

    this.auth.changePassword(this.newPassword, this.currentPassword).subscribe({
      next: () => {
        this.saving = false;
        this.success = 'Password changed successfully.';
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
      },
      error: (err: any) => {
        this.saving = false;
        this.error = Array.isArray(err?.error) ? err.error.join(' ') : (err?.error ?? 'Failed to change password.');
      },
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
