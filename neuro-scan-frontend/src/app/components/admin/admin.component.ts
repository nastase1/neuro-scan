import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AdminService } from '../../services/admin.service';
import { AuthService } from '../../services/auth.service';
import {
  AdminStats, AdminUser, AdminUpdateUser,
  AdminDoctor, AdminScan, PagedResult, UserRole, ScanStatus
} from '../../models/api.models';

type Tab = 'overview' | 'users' | 'doctors' | 'scans';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit {
  private adminSvc = inject(AdminService);
  private authSvc = inject(AuthService);
  private route = inject(ActivatedRoute);

  UserRole = UserRole;
  ScanStatus = ScanStatus;

  // ── Tab ──────────────────────────────────────────────────────────────────
  activeTab = signal<Tab>('overview');

  // ── Stats ─────────────────────────────────────────────────────────────────
  stats = signal<AdminStats | null>(null);

  // ── Users ─────────────────────────────────────────────────────────────────
  usersResult = signal<PagedResult<AdminUser> | null>(null);
  userSearch = signal('');
  userRoleFilter = signal<number | undefined>(undefined);
  userPage = signal(1);
  usersLoading = signal(false);

  // ── Doctors ───────────────────────────────────────────────────────────────
  doctors = signal<AdminDoctor[]>([]);
  doctorSearch = signal('');
  doctorsLoading = signal(false);
  expandedDoctorId = signal<string | null>(null);

  filteredDoctors = computed(() => {
    const q = this.doctorSearch().toLowerCase();
    if (!q) return this.doctors();
    return this.doctors().filter(d =>
      `${d.firstName} ${d.lastName}`.toLowerCase().includes(q) ||
      d.email.toLowerCase().includes(q)
    );
  });

  // ── Scans ─────────────────────────────────────────────────────────────────
  scansResult = signal<PagedResult<AdminScan> | null>(null);
  scanSearch = signal('');
  scanStatusFilter = signal<number | undefined>(undefined);
  scanPage = signal(1);
  scansLoading = signal(false);

  // ── Edit User Modal ────────────────────────────────────────────────────────
  editingUser = signal<AdminUser | null>(null);
  editForm = signal<AdminUpdateUser>({ firstName: '', lastName: '', email: '', role: UserRole.StandardUser });
  editSaving = signal(false);
  editError = signal('');

  // ── Reset Password Modal ───────────────────────────────────────────────────
  resetPasswordUserId = signal<string | null>(null);
  newPassword = signal('');
  resetSaving = signal(false);
  resetError = signal('');

  // ── Delete confirm ─────────────────────────────────────────────────────────
  deletingUserId = signal<string | null>(null);
  deletingScanId = signal<string | null>(null);

  // ── Feedback ──────────────────────────────────────────────────────────────
  successMsg = signal('');
  errorMsg = signal('');

  ngOnInit() {
    this.route.queryParamMap.subscribe(params => {
      const rawTab = (params.get('tab') || 'overview').toLowerCase();
      const tab: Tab = this.isTab(rawTab) ? rawTab : 'overview';
      this.switchTab(tab);
    });
  }

  // ── Navigation ────────────────────────────────────────────────────────────
  switchTab(tab: Tab) {
    this.activeTab.set(tab);
    if (tab === 'overview' && !this.stats()) this.loadStats();
    if (tab === 'users') this.loadUsers();
    if (tab === 'doctors') this.loadDoctors();
    if (tab === 'scans') this.loadScans();
  }

  // ── Stats ─────────────────────────────────────────────────────────────────
  loadStats() {
    this.adminSvc.getStats().subscribe({
      next: s => this.stats.set(s),
      error: () => {}
    });
  }

  // ── Users ─────────────────────────────────────────────────────────────────
  loadUsers() {
    this.usersLoading.set(true);
    this.adminSvc.getUsers(this.userSearch(), this.userRoleFilter(), this.userPage()).subscribe({
      next: r => { this.usersResult.set(r); this.usersLoading.set(false); },
      error: () => this.usersLoading.set(false)
    });
  }

  onUserSearch(value: string) {
    this.userSearch.set(value);
    this.userPage.set(1);
    this.loadUsers();
  }

  onUserRoleFilter(value: string) {
    this.userRoleFilter.set(value === '' ? undefined : +value);
    this.userPage.set(1);
    this.loadUsers();
  }

  setUserPage(p: number) {
    this.userPage.set(p);
    this.loadUsers();
  }

  openEditUser(user: AdminUser) {
    this.editingUser.set(user);
    this.editForm.set({ firstName: user.firstName, lastName: user.lastName, email: user.email, role: user.role });
    this.editError.set('');
  }

  saveEditUser() {
    const user = this.editingUser();
    if (!user) return;
    this.editSaving.set(true);
    this.adminSvc.updateUser(user.id, this.editForm()).subscribe({
      next: () => {
        this.editSaving.set(false);
        this.editingUser.set(null);
        this.showSuccess('User updated successfully.');
        this.loadUsers();
      },
      error: () => { this.editSaving.set(false); this.editError.set('Failed to update user.'); }
    });
  }

  confirmDeleteUser(id: string) { this.deletingUserId.set(id); }

  deleteUser() {
    const id = this.deletingUserId();
    if (!id) return;
    this.adminSvc.deleteUser(id).subscribe({
      next: () => { this.deletingUserId.set(null); this.showSuccess('User deleted.'); this.loadUsers(); },
      error: () => { this.deletingUserId.set(null); this.showError('Failed to delete user.'); }
    });
  }

  openResetPassword(userId: string) {
    this.resetPasswordUserId.set(userId);
    this.newPassword.set('');
    this.resetError.set('');
  }

  saveResetPassword() {
    const id = this.resetPasswordUserId();
    const pw = this.newPassword();
    if (!id || !pw) return;
    this.resetSaving.set(true);
    this.adminSvc.resetPassword(id, pw).subscribe({
      next: () => {
        this.resetSaving.set(false);
        this.resetPasswordUserId.set(null);
        this.showSuccess('Password reset successfully.');
      },
      error: () => { this.resetSaving.set(false); this.resetError.set('Failed to reset password.'); }
    });
  }

  // ── Doctors ───────────────────────────────────────────────────────────────
  loadDoctors() {
    this.doctorsLoading.set(true);
    this.adminSvc.getDoctors().subscribe({
      next: d => { this.doctors.set(d); this.doctorsLoading.set(false); },
      error: () => this.doctorsLoading.set(false)
    });
  }

  toggleDoctor(id: string) {
    this.expandedDoctorId.set(this.expandedDoctorId() === id ? null : id);
  }

  // ── Scans ─────────────────────────────────────────────────────────────────
  loadScans() {
    this.scansLoading.set(true);
    this.adminSvc.getScans(this.scanSearch(), this.scanStatusFilter(), this.scanPage()).subscribe({
      next: r => { this.scansResult.set(r); this.scansLoading.set(false); },
      error: () => this.scansLoading.set(false)
    });
  }

  onScanSearch(value: string) {
    this.scanSearch.set(value);
    this.scanPage.set(1);
    this.loadScans();
  }

  onScanStatusFilter(value: string) {
    this.scanStatusFilter.set(value === '' ? undefined : +value);
    this.scanPage.set(1);
    this.loadScans();
  }

  setScanPage(p: number) {
    this.scanPage.set(p);
    this.loadScans();
  }

  confirmDeleteScan(id: string) { this.deletingScanId.set(id); }

  deleteScan() {
    const id = this.deletingScanId();
    if (!id) return;
    this.adminSvc.deleteScan(id).subscribe({
      next: () => { this.deletingScanId.set(null); this.showSuccess('Scan deleted.'); this.loadScans(); },
      error: () => { this.deletingScanId.set(null); this.showError('Failed to delete scan.'); }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  roleName(role: UserRole): string {
    switch (role) {
      case UserRole.StandardUser: return 'User';
      case UserRole.Doctor: return 'Doctor';
      case UserRole.Admin: return 'Admin';
      default: return 'Unknown';
    }
  }

  scanStatusName(status: ScanStatus): string {
    switch (status) {
      case ScanStatus.Uploaded: return 'Uploaded';
      case ScanStatus.Processing: return 'Processing';
      case ScanStatus.Analyzed: return 'Analyzed';
      case ScanStatus.Failed: return 'Failed';
      case ScanStatus.ReviewedByDoctor: return 'Reviewed';
      default: return 'Unknown';
    }
  }

  scanStatusClass(status: ScanStatus): string {
    switch (status) {
      case ScanStatus.Analyzed: return 'badge-blue';
      case ScanStatus.ReviewedByDoctor: return 'badge-green';
      case ScanStatus.Failed: return 'badge-red';
      case ScanStatus.Processing: return 'badge-yellow';
      default: return 'badge-gray';
    }
  }

  riskClass(risk?: string): string {
    if (!risk) return 'badge-gray';
    const r = risk.toLowerCase();
    if (r === 'high') return 'badge-red';
    if (r === 'medium') return 'badge-yellow';
    return 'badge-green';
  }

  pages(total: number): number[] {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  pct(value: number, total: number): number {
    if (total <= 0) return 0;
    const raw = Math.round((value / total) * 100);
    return Math.min(100, Math.max(0, raw));
  }

  updateEditField(field: keyof AdminUpdateUser, value: string | number) {
    this.editForm.update(f => ({ ...f, [field]: value }));
  }

  logout() { this.authSvc.logout(); }

  private isTab(value: string): value is Tab {
    return value === 'overview' || value === 'users' || value === 'doctors' || value === 'scans';
  }

  private showSuccess(msg: string) {
    this.successMsg.set(msg);
    setTimeout(() => this.successMsg.set(''), 3000);
  }
  private showError(msg: string) {
    this.errorMsg.set(msg);
    setTimeout(() => this.errorMsg.set(''), 4000);
  }
}
