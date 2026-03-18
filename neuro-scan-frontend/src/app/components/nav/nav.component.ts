import { Component, ElementRef, HostListener, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/api.models';

@Component({
  selector: 'app-nav',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './nav.component.html',
  styleUrl: './nav.component.css'
})
export class NavComponent implements OnInit {
  currentUser: User | null = null;
  isMenuOpen = false;
  isProfileOpen = false;
  isInviteOpen = false;

  inviteCode = signal<string | null>(null);
  inviteCodeCopied = signal(false);

  constructor(
    private authService: AuthService,
    private router: Router,
    private elRef: ElementRef
  ) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elRef.nativeElement.contains(event.target)) {
      this.isProfileOpen = false;
      this.isInviteOpen = false;
      this.isMenuOpen = false;
    }
  }

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user && this.authService.isDoctor()) {
        this.loadInviteCode();
      }
    });
  }

  loadInviteCode(): void {
    this.authService.getMyInviteCode().subscribe({
      next: (res) => this.inviteCode.set(res.inviteCode),
      error: () => {}
    });
  }

  toggleInvite(): void {
    this.isInviteOpen = !this.isInviteOpen;
    if (this.isInviteOpen) {
      this.isProfileOpen = false;
      this.isMenuOpen = false;
    }
  }

  copyInviteCode(): void {
    const code = this.inviteCode();
    if (!code) return;
    navigator.clipboard.writeText(code).then(() => {
      this.inviteCodeCopied.set(true);
      setTimeout(() => this.inviteCodeCopied.set(false), 2000);
    });
  }

  toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
    if (this.isMenuOpen) {
      this.isProfileOpen = false;
      this.isInviteOpen = false;
    }
  }

  toggleProfile(): void {
    this.isProfileOpen = !this.isProfileOpen;
    if (this.isProfileOpen) {
      this.isMenuOpen = false;
      this.isInviteOpen = false;
    }
  }

  closeMenus(): void {
    this.isMenuOpen = false;
    this.isProfileOpen = false;
    this.isInviteOpen = false;
  }

  getUserInitials(): string {
    if (!this.currentUser) return '';
    const first = this.currentUser.firstName?.[0] || '';
    const last = this.currentUser.lastName?.[0] || '';
    return (first + last).toUpperCase();
  }

  getUserRole(): string {
    if (this.authService.isAdmin()) {
      return 'Admin';
    }

    return this.authService.isDoctor() ? 'Doctor' : 'User';
  }

  isDoctor(): boolean {
    return this.authService.isDoctor();
  }

  isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  getHomeRoute(): string {
    return this.authService.getHomeRoute();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
