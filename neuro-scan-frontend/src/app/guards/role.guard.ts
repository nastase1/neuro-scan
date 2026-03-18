import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/api.models';

export const doctorGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const currentUser = authService.getCurrentUser();
  
  if (currentUser && currentUser.role === UserRole.Doctor) {
    return true;
  }

  // Redirect to dashboard if not a doctor
  router.navigate([authService.getHomeRoute()]);
  return false;
};

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const currentUser = authService.getCurrentUser();

  if (currentUser && currentUser.role === UserRole.Admin) {
    return true;
  }

  router.navigate([authService.getHomeRoute()]);
  return false;
};
