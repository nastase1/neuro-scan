import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { NavComponent } from './components/nav/nav.component';
import { AuthService } from './services/auth.service';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, NavComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('neuro-scan-frontend');
  showNav = false;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    // Update nav visibility on route changes
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updateNavVisibility();
      });

    // Also subscribe to auth changes
    this.authService.isAuthenticated$.subscribe(() => {
      this.updateNavVisibility();
    });
  }

  private updateNavVisibility(): void {
    const publicRoutes = ['/login', '/register'];
    const currentRoute = this.router.url;
    this.showNav = this.authService.isAuthenticated() && !publicRoutes.includes(currentRoute);
  }
}
