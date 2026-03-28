import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Route guard factory — restricts access to users who hold the given permission.
 *
 * Usage in route config:
 *   canActivate: [requirePermission('roles.manage')]
 */
export function requirePermission(permission: string): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (auth.isLoggedIn() && auth.hasPermission(permission)) return true;
    router.navigate(['/']);
    return false;
  };
}
