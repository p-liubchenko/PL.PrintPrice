import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Only allows access to /onboarding when setup hasn't been done yet. */
export const setupGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.setupRequired) {
    router.navigate([auth.isLoggedIn() ? '/' : '/login']);
    return false;
  }

  return true;
};
