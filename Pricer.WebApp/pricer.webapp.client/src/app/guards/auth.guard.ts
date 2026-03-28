import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Protects main app routes. Handles all redirect cases in order. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.setupRequired) {
    router.navigate(['/onboarding']);
    return false;
  }

  if (!auth.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }

  if (auth.mustChangePassword()) {
    router.navigate(['/change-password']);
    return false;
  }

  return true;
};
