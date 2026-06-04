import { AfterViewInit, Component, ElementRef, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/api.models';
import { APP_VERSION } from '../../config/app-version';
import { environment } from '../../../environments/environment';

declare var google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements AfterViewInit {
  @ViewChild('googleBtn') googleBtnRef!: ElementRef;

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
    if (this.authService.isAuthenticated()) {
      this.router.navigate([this.authService.getHomeRoute()]);
    }
  }

  ngAfterViewInit(): void {
    if (typeof google === 'undefined') return;

    google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response: { credential: string }) => this.handleGoogleCredential(response.credential)
    });

    // Match the Google button width to its container (Google caps it at 400px)
    const containerWidth = this.googleBtnRef.nativeElement.clientWidth || 360;
    const width = Math.min(Math.round(containerWidth), 400);

    google.accounts.id.renderButton(this.googleBtnRef.nativeElement, {
      theme: 'outline',
      size: 'large',
      width,
      text: 'continue_with',
      shape: 'pill',
      logo_alignment: 'center'
    });
  }

  private handleGoogleCredential(credential: string): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.googleLogin(credential).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: (response) => {
        if (response.success) {
          this.router.navigate([this.authService.getHomeRoute()]);
        } else {
          this.errorMessage.set(response.message || 'Google login failed');
        }
      },
      error: (error) => {
        this.errorMessage.set(error.error?.message || 'Google login failed. Please try again.');
      }
    });
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
