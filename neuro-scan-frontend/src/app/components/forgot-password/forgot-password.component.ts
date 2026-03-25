import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { APP_VERSION } from '../../config/app-version';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
})
export class ForgotPasswordComponent {
  // Step control
  step = signal<1 | 2>(1);

  // Step 1 fields
  email = '';

  // Step 2 fields
  code = '';
  newPassword = '';
  confirmPassword = '';

  // State
  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');
  readonly appVersion = APP_VERSION;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  submitEmail(): void {
    if (!this.email) return;
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.forgotPassword({ email: this.email }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.step.set(2);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('Something went wrong. Please try again.');
      }
    });
  }

  submitReset(): void {
    if (!this.code || !this.newPassword || !this.confirmPassword) return;

    if (this.newPassword !== this.confirmPassword) {
      this.errorMessage.set('Passwords do not match.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.resetPassword({
      email: this.email,
      code: this.code,
      newPassword: this.newPassword,
      confirmPassword: this.confirmPassword
    }).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.success) {
          this.successMessage.set('Password reset successfully! Redirecting to login...');
          setTimeout(() => this.router.navigate(['/login']), 2000);
        } else {
          this.errorMessage.set(res.message ?? 'Invalid or expired code.');
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Failed to reset password. Please try again.');
      }
    });
  }

  goBack(): void {
    this.step.set(1);
    this.errorMessage.set('');
    this.code = '';
    this.newPassword = '';
    this.confirmPassword = '';
  }
}
