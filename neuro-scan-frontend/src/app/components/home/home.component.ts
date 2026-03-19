import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { MriService } from '../../services/mri.service';
import { AuthService } from '../../services/auth.service';
import { MriScanDetail, Patient, ScanStatus } from '../../models/api.models';
import { PatientService } from '../../services/patient.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  isLoading = signal(true);
  scans = signal<MriScanDetail[]>([]);
  pendingReviewCount = signal(0);

  constructor(
    private mriService: MriService,
    private patientService: PatientService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    if (this.authService.isAdmin()) {
      this.isLoading.set(false);
      return;
    }

    if (this.authService.isDoctor()) {
      this.loadDoctorData();
      return;
    }

    this.loadUserData();
  }

  private loadDoctorData(): void {
    this.mriService.getPendingReviewScans().pipe(catchError(() => of([] as MriScanDetail[]))).subscribe({
      next: (pendingScans) => {
        this.pendingReviewCount.set(pendingScans.length);

        this.mriService.getAllScans().pipe(catchError(() => of([] as MriScanDetail[]))).subscribe({
          next: (allScans) => {
            if (allScans.length > 0) {
              this.scans.set(allScans);
              this.isLoading.set(false);
              return;
            }

            // Fallback 1: pending review scans still provide useful real metrics.
            if (pendingScans.length > 0) {
              this.scans.set(pendingScans);
            }

            // Fallback 2: aggregate scans from doctor's patients to avoid zeroed cards.
            this.patientService.getAllPatients().pipe(catchError(() => of([] as Patient[]))).subscribe({
              next: (patients) => {
                if (!patients.length) {
                  this.isLoading.set(false);
                  return;
                }

                const requests = patients.map((patient) =>
                  this.mriService.getPatientScans(patient.id).pipe(catchError(() => of([] as MriScanDetail[])))
                );

                forkJoin(requests).subscribe({
                  next: (results) => {
                    const flattened = results.flat();
                    if (flattened.length > 0) {
                      this.scans.set(flattened);
                    }
                    this.isLoading.set(false);
                  },
                  error: () => {
                    this.isLoading.set(false);
                  }
                });
              },
              error: () => {
                  this.isLoading.set(false);
              }
            });
          },
          error: () => {
            this.isLoading.set(false);
          }
        });
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  private loadUserData(): void {
    this.mriService.getMyScans().pipe(catchError(() => of([] as MriScanDetail[]))).subscribe({
      next: (scans) => {
        this.scans.set(scans);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  get reviewedCount(): number {
    return this.scans().filter(s => s.status === ScanStatus.ReviewedByDoctor).length;
  }

  get analyzedCount(): number {
    return this.scans().filter(s => s.analysisResult !== undefined).length;
  }

  get highRiskCount(): number {
    return this.scans().filter(s => (s.analysisResult?.epilepsyRiskScore ?? 0) >= 70).length;
  }

  get avgRiskScore(): number {
    const withRisk = this.scans().filter(s => s.analysisResult?.epilepsyRiskScore !== undefined);
    if (withRisk.length === 0) {
      return 0;
    }

    const sum = withRisk.reduce((acc, scan) => acc + (scan.analysisResult?.epilepsyRiskScore ?? 0), 0);
    return sum / withRisk.length;
  }

  get recentScans(): MriScanDetail[] {
    return [...this.scans()]
      .sort((a, b) => new Date(b.uploadDate).getTime() - new Date(a.uploadDate).getTime())
      .slice(0, 5);
  }

  formatRelativeDate(dateValue: string): string {
    const then = new Date(dateValue).getTime();
    const diffMs = Date.now() - then;
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays <= 0) return 'Today';
    if (diffDays === 1) return '1 day ago';
    if (diffDays < 30) return `${diffDays} days ago`;

    const months = Math.floor(diffDays / 30);
    return months === 1 ? '1 month ago' : `${months} months ago`;
  }
}
