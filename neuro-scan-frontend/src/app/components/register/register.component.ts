import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { RegisterRequest, UserRole } from '../../models/api.models';
import { APP_VERSION } from '../../config/app-version';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  registerRequest: RegisterRequest = {
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    role: UserRole.StandardUser,
    inviteCode: ''
  };

  confirmPassword = '';
  isLoading = signal(false);
  errorMessage = signal('');
  showPassword = false;
  showConfirmPassword = false;
  readonly UserRole = UserRole;
  readonly appVersion = APP_VERSION;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    // Redirect if already logged in
    if (this.authService.isAuthenticated()) {
      this.router.navigate([this.authService.getHomeRoute()]);
    }
  }

  onSubmit(): void {
    // Validation
    if (!this.registerRequest.firstName || !this.registerRequest.lastName || 
        !this.registerRequest.email || !this.registerRequest.password) {
      this.errorMessage.set('Please fill in all fields');
      return;
    }

    if (this.registerRequest.password !== this.confirmPassword) {
      this.errorMessage.set('Passwords do not match');
      return;
    }

    if (this.registerRequest.password.length < 6) {
      this.errorMessage.set('Password must be at least 6 characters long');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.register(this.registerRequest).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.router.navigate([this.authService.getHomeRoute()]);
        } else {
          this.errorMessage.set(response.message || 'Registration failed');
        }
      },
      error: (error) => {
        console.error('Registration error:', error);
        this.errorMessage.set(
          error.error?.message || error.error?.Message || 'Registration failed. Please try again.'
        );
      }
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPassword(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }
}
