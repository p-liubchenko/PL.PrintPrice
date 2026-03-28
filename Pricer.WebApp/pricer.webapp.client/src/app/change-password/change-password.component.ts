import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: false,
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.css',
})
export class ChangePasswordComponent {
  newPassword = '';
  confirmPassword = '';
  currentPassword = '';
  error = '';
  loading = false;

  constructor(public auth: AuthService, private router: Router) {}

  get isForced(): boolean {
    return this.auth.mustChangePassword();
  }

  get passwordMismatch(): boolean {
    return this.confirmPassword.length > 0 && this.newPassword !== this.confirmPassword;
  }

  submit(): void {
    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }
    this.error = '';
    this.loading = true;

    const current = this.isForced ? undefined : this.currentPassword;
    this.auth.changePassword(this.newPassword, current).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err: any) => {
        this.error = Array.isArray(err?.error) ? err.error.join(' ') : (err?.error ?? 'Failed to change password.');
        this.loading = false;
      },
    });
  }
}
