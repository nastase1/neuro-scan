import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PatientService } from '../../services/patient.service';
import { MriService } from '../../services/mri.service';
import { Patient, MriScanDetail, ScanStatus } from '../../models/api.models';

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './patient-detail.component.html',
  styleUrls: ['./patient-detail.component.css']
})
export class PatientDetailComponent implements OnInit {
  patient = signal<Patient | null>(null);
  scans = signal<MriScanDetail[]>([]);
  isLoading = signal(true);
  isLoadingScans = signal(true);
  errorMessage = signal('');
  
  // Expose Math for template
  Math = Math;
  
  // Pagination
  currentPage = signal(1);
  itemsPerPage = signal(10);
  
  // Filtering
  statusFilter = signal<string>('all');
  searchQuery = signal('');
  sortBy = signal<'date' | 'status'>('date');
  sortOrder = signal<'asc' | 'desc'>('desc');

  // Expose ScanStatus enum for template
  ScanStatus = ScanStatus;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private patientService: PatientService,
    private mriService: MriService
  ) {}

  ngOnInit(): void {
    const patientId = this.route.snapshot.paramMap.get('id');
    if (patientId) {
      this.loadPatientDetails(patientId);
      this.loadPatientScans(patientId);
    }
  }

  loadPatientDetails(patientId: string): void {
    this.isLoading.set(true);
    this.patientService.getPatientById(patientId).subscribe({
      next: (patient) => {
        this.patient.set(patient);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading patient:', error);
        this.errorMessage.set('Failed to load patient details');
        this.isLoading.set(false);
      }
    });
  }

  loadPatientScans(patientId: string): void {
    this.isLoadingScans.set(true);
    this.mriService.getPatientScans(patientId).subscribe({
      next: (scans) => {
        this.scans.set(scans);
        this.isLoadingScans.set(false);
      },
      error: (error) => {
        console.error('Error loading scans:', error);
        this.isLoadingScans.set(false);
      }
    });
  }

  // Filtered and sorted scans
  filteredScans = computed(() => {
    let filtered = this.scans();

    // Apply status filter
    if (this.statusFilter() !== 'all') {
      const filterStatus = this.statusFilter();
      filtered = filtered.filter(scan => {
        const statusName = this.getStatusName(scan.status);
        return statusName.toLowerCase() === filterStatus.toLowerCase();
      });
    }

    // Apply search filter
    if (this.searchQuery()) {
      const query = this.searchQuery().toLowerCase();
      filtered = filtered.filter(scan =>
        scan.originalFileName.toLowerCase().includes(query)
      );
    }

    // Apply sorting
    filtered = [...filtered].sort((a, b) => {
      if (this.sortBy() === 'date') {
        const dateA = new Date(a.uploadDate).getTime();
        const dateB = new Date(b.uploadDate).getTime();
        return this.sortOrder() === 'asc' ? dateA - dateB : dateB - dateA;
      } else {
        const statusA = a.status;
        const statusB = b.status;
        return this.sortOrder() === 'asc' ? statusA - statusB : statusB - statusA;
      }
    });

    return filtered;
  });

  // Paginated scans
  paginatedScans = computed(() => {
    const filtered = this.filteredScans();
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    const end = start + this.itemsPerPage();
    return filtered.slice(start, end);
  });

  totalPages = computed(() => {
    return Math.ceil(this.filteredScans().length / this.itemsPerPage());
  });

  // Pagination controls
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  nextPage(): void {
    this.goToPage(this.currentPage() + 1);
  }

  previousPage(): void {
    this.goToPage(this.currentPage() - 1);
  }

  // Filter and sort controls
  onStatusFilterChange(status: string): void {
    this.statusFilter.set(status);
    this.currentPage.set(1); // Reset to first page
  }

  onSearchChange(query: string): void {
    this.searchQuery.set(query);
    this.currentPage.set(1);
  }

  onSortChange(sortBy: 'date' | 'status'): void {
    if (this.sortBy() === sortBy) {
      // Toggle order if same field
      this.sortOrder.set(this.sortOrder() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(sortBy);
      this.sortOrder.set('desc');
    }
  }

  // Actions
  editPatient(): void {
    if (this.patient()) {
      this.router.navigate(['/patients', this.patient()!.id, 'edit'], {
        queryParams: {
          returnTo: 'patient-detail',
          patientId: this.patient()!.id
        }
      });
    }
  }

  backToList(): void {
    this.router.navigate(['/patients']);
  }

  viewScanDetails(scanId: string): void {
    const patientId = this.patient()?.id;
    this.router.navigate(['/dashboard'], {
      queryParams: {
        scanId,
        source: 'doctor-history',
        patientId: patientId ?? undefined
      }
    });
  }

  deleteScan(scanId: string, event: Event): void {
    event.stopPropagation();
    if (confirm('Are you sure you want to delete this scan? This action cannot be undone.')) {
      // TODO: Implement delete scan functionality
      console.log('Delete scan:', scanId);
    }
  }

  // Helper methods
  formatDate(dateString: string): string {
    // Ensure UTC dates are parsed correctly (append Z if no timezone info)
    const utc = /[Z+]/.test(dateString) ? dateString : dateString + 'Z';
    const date = new Date(utc);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getStatusName(status: ScanStatus): string {
    const names: { [key in ScanStatus]: string } = {
      [ScanStatus.Uploaded]: 'Uploaded',
      [ScanStatus.Processing]: 'Processing',
      [ScanStatus.Analyzed]: 'Analyzed',
      [ScanStatus.Failed]: 'Failed',
      [ScanStatus.ReviewedByDoctor]: 'Reviewed by Doctor'
    };
    return names[status] || 'Unknown';
  }

  getStatusColor(status: ScanStatus): string {
    const colors: { [key in ScanStatus]: string } = {
      [ScanStatus.Uploaded]: 'text-blue-400 bg-blue-400/20',
      [ScanStatus.Processing]: 'text-yellow-400 bg-yellow-400/20',
      [ScanStatus.Analyzed]: 'text-green-400 bg-green-400/20',
      [ScanStatus.Failed]: 'text-red-400 bg-red-400/20',
      [ScanStatus.ReviewedByDoctor]: 'text-purple-400 bg-purple-400/20'
    };
    return colors[status] || 'text-gray-400 bg-gray-400/20';
  }

  getStatusIcon(status: ScanStatus): string {
    const icons: { [key in ScanStatus]: string } = {
      [ScanStatus.Uploaded]: 'M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12',
      [ScanStatus.Processing]: 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15',
      [ScanStatus.Analyzed]: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
      [ScanStatus.Failed]: 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
      [ScanStatus.ReviewedByDoctor]: 'M15 12a3 3 0 11-6 0 3 3 0 016 0z'
    };
    return icons[status] || '';
  }

  getAgeDisplay(age: number): string {
    return `${age} years old`;
  }

  getPatientInitials(): string {
    const patient = this.patient();
    if (!patient) return '';
    return `${patient.firstName[0]}${patient.lastName[0]}`.toUpperCase();
  }
}
