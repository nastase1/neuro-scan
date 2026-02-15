import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { PatientService } from '../../services/patient.service';
import { Patient } from '../../models/api.models';

@Component({
  selector: 'app-patient-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './patient-list.component.html',
  styleUrls: ['./patient-list.component.css']
})
export class PatientListComponent implements OnInit {
  patients = signal<Patient[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');
  searchQuery = signal('');

  constructor(
    private patientService: PatientService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    
    console.log('Loading patients...');
    console.log('Auth token:', this.patientService['authService'].getToken());

    this.patientService.getAllPatients().subscribe({
      next: (patients) => {
        console.log('Patients loaded:', patients);
        this.patients.set(patients);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading patients:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        this.errorMessage.set(`Failed to load patients: ${error.status === 0 ? 'Cannot connect to server' : error.error?.message || error.message}`);
        this.isLoading.set(false);
      }
    });
  }

  get filteredPatients(): Patient[] {
    if (!this.searchQuery()) {
      return this.patients();
    }

    const query = this.searchQuery().toLowerCase();
    return this.patients().filter(patient =>
      patient.firstName.toLowerCase().includes(query) ||
      patient.lastName.toLowerCase().includes(query) ||
      patient.medicalRecordNumber.toLowerCase().includes(query)
    );
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
  }

  createPatient(): void {
    this.router.navigate(['/patients/new']);
  }

  viewPatient(patientId: string): void {
    this.router.navigate(['/patients', patientId]);
  }

  editPatient(patientId: string, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/patients', patientId, 'edit']);
  }

  deletePatient(patientId: string, event: Event): void {
    event.stopPropagation();
    
    if (confirm('Are you sure you want to delete this patient? This action cannot be undone.')) {
      this.patientService.deletePatient(patientId).subscribe({
        next: () => {
          const updatedPatients = this.patients().filter(p => p.id !== patientId);
          this.patients.set(updatedPatients);
        },
        error: (error) => {
          console.error('Error deleting patient:', error);
          alert('Failed to delete patient');
        }
      });
    }
  }

  getAgeDisplay(age: number): string {
    return `${age} years old`;
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    });
  }
}
