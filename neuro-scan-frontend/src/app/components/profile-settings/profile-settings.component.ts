import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UpdateProfileRequest } from '../../models/api.models';

@Component({
  selector: 'app-profile-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './profile-settings.component.html',
  styleUrls: ['./profile-settings.component.css']
})
export class ProfileSettingsComponent implements OnInit {
  firstName = '';
  lastName = '';
  email = '';
  currentPassword = '';
  newPassword = '';
  confirmNewPassword = '';

  isLoading = signal(false);
  successMessage = signal('');
  errorMessage = signal('');
  showCurrentPassword = false;
  showNewPassword = false;

  constructor(public authService: AuthService) {}

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    if (user) {
      this.firstName = user.firstName;
      this.lastName = user.lastName;
      this.email = user.email;
    }
  }

  saveProfile(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (!this.firstName.trim() || !this.lastName.trim() || !this.email.trim()) {
      this.errorMessage.set('First name, last name, and email are required.');
      return;
    }

    if (this.newPassword && this.newPassword !== this.confirmNewPassword) {
      this.errorMessage.set('New passwords do not match.');
      return;
    }

    if (this.newPassword && !this.currentPassword) {
      this.errorMessage.set('Enter your current password to set a new one.');
      return;
    }

    if (this.newPassword && this.newPassword.length < 6) {
      this.errorMessage.set('New password must be at least 6 characters.');
      return;
    }

    const request: UpdateProfileRequest = {
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      email: this.email.trim(),
    };

    if (this.newPassword) {
      request.currentPassword = this.currentPassword;
      request.newPassword = this.newPassword;
    }

    this.isLoading.set(true);
    this.authService.updateProfile(request).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.success) {
          this.successMessage.set(res.message || 'Profile updated successfully.');
          this.currentPassword = '';
          this.newPassword = '';
          this.confirmNewPassword = '';
        } else {
          this.errorMessage.set(res.message || 'Failed to update profile.');
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'An error occurred while updating your profile.');
      }
    });
  }

  getUserRole(): string {
    if (this.authService.isAdmin()) return 'Admin';
    if (this.authService.isDoctor()) return 'Doctor';
    return 'Standard User';
  }

  getUserInitials(): string {
    const first = this.firstName?.[0] || '';
    const last = this.lastName?.[0] || '';
    return (first + last).toUpperCase();
  }
}
