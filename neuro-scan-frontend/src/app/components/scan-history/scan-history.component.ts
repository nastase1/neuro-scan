import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MriService } from '../../services/mri.service';
import { MriScanDetail, ScanStatus } from '../../models/api.models';

@Component({
  selector: 'app-scan-history',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './scan-history.component.html',
  styleUrls: ['./scan-history.component.css']
})
export class ScanHistoryComponent implements OnInit {
  scans = signal<MriScanDetail[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');

  statusFilter = signal<string>('all');
  searchQuery = signal('');
  ScanStatus = ScanStatus;

  filteredScans = computed(() => {
    let list = this.scans();

    if (this.statusFilter() !== 'all') {
      list = list.filter(s => this.getStatusKey(s.status) === this.statusFilter());
    }

    const q = this.searchQuery().toLowerCase();
    if (q) {
      list = list.filter(s => {
        const patientName = s.patient?.fullName?.toLowerCase() ?? '';
        return s.originalFileName.toLowerCase().includes(q) || patientName.includes(q);
      });
    }

    return list;
  });

  constructor(private mriService: MriService, private router: Router) {}

  ngOnInit(): void {
      // First, get the current user's patient record
      this.mriService.getMyPatient().subscribe({
        next: (patient) => {
          // Then, fetch all scans for that patient
          this.mriService.getPatientScans(patient.id).subscribe({
            next: (scans) => {
              this.scans.set(scans);
              this.isLoading.set(false);
            },
            error: () => {
              this.errorMessage.set('Failed to load scan history.');
              this.isLoading.set(false);
            }
          });
        },
        error: () => {
          this.errorMessage.set('No patient record found.');
          this.isLoading.set(false);
        }
      });
  }

  viewScan(scanId: string): void {
    this.router.navigate(['/dashboard'], { queryParams: { scanId } });
  }

  getStatusKey(status: ScanStatus): string {
    const map: Record<ScanStatus, string> = {
      [ScanStatus.Uploaded]: 'uploaded',
      [ScanStatus.Processing]: 'processing',
      [ScanStatus.Analyzed]: 'analyzed',
      [ScanStatus.Failed]: 'failed',
      [ScanStatus.ReviewedByDoctor]: 'reviewed'
    };
    return map[status] ?? 'unknown';
  }

  getStatusLabel(status: ScanStatus): string {
    const map: Record<ScanStatus, string> = {
      [ScanStatus.Uploaded]: 'Uploaded',
      [ScanStatus.Processing]: 'Processing',
      [ScanStatus.Analyzed]: 'Analyzed',
      [ScanStatus.Failed]: 'Failed',
      [ScanStatus.ReviewedByDoctor]: 'Reviewed by Doctor'
    };
    return map[status] ?? 'Unknown';
  }

  getStatusClasses(status: ScanStatus): string {
    const map: Record<ScanStatus, string> = {
      [ScanStatus.Uploaded]: 'bg-blue-500/20 text-blue-300 border-blue-500/30',
      [ScanStatus.Processing]: 'bg-yellow-500/20 text-yellow-300 border-yellow-500/30',
      [ScanStatus.Analyzed]: 'bg-green-500/20 text-green-300 border-green-500/30',
      [ScanStatus.Failed]: 'bg-red-500/20 text-red-300 border-red-500/30',
      [ScanStatus.ReviewedByDoctor]: 'bg-purple-500/20 text-purple-300 border-purple-500/30'
    };
    return map[status] ?? 'bg-gray-500/20 text-gray-300 border-gray-500/30';
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
