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
  router.navigate(['/dashboard']);
  return false;
};
