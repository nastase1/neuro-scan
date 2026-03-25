import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/api.models';
import { APP_VERSION } from '../../config/app-version';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginRequest: LoginRequest = {
    email: '',
    password: ''
  };

  isLoading = signal(false);
  errorMessage = signal('');
  showPassword = false;
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
    if (!this.loginRequest.email || !this.loginRequest.password) {
      this.errorMessage.set('Please fill in all fields');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.login(this.loginRequest).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.router.navigate([this.authService.getHomeRoute()]);
        } else {
          this.errorMessage.set(response.message || 'Invalid email or password');
        }
      },
      error: (error) => {
        console.error('Login error:', error);
        this.errorMessage.set(
          error.error?.message || error.error?.Message || 'Invalid email or password. Please try again.'
        );
      }
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
}
