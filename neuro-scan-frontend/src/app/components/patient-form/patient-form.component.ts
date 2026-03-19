import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { PatientService } from '../../services/patient.service';
import { CreatePatient, UpdatePatient, Patient } from '../../models/api.models';

@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './patient-form.component.html',
  styleUrls: ['./patient-form.component.css']
})
export class PatientFormComponent implements OnInit {
  isEditMode = signal(false);
  patientId: string | null = null;
  returnTo: 'patients-list' | 'patient-detail' = 'patients-list';
  returnPatientId: string | null = null;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal('');

  patient = signal<CreatePatient>({
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    medicalRecordNumber: '',
    email: ''
  });

  constructor(
    private patientService: PatientService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.patientId = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.patientId && this.route.snapshot.url.some(segment => segment.path === 'edit'));
    const returnTo = this.route.snapshot.queryParamMap.get('returnTo');
    const returnPatientId = this.route.snapshot.queryParamMap.get('patientId');

    if (returnTo === 'patient-detail' && returnPatientId) {
      this.returnTo = 'patient-detail';
      this.returnPatientId = returnPatientId;
    }

    if (this.isEditMode() && this.patientId) {
      this.loadPatient(this.patientId);
    }
  }

  loadPatient(patientId: string): void {
    this.isLoading.set(true);
    
    this.patientService.getPatientById(patientId).subscribe({
      next: (patient: Patient) => {
        this.patient.set({
          firstName: patient.firstName,
          lastName: patient.lastName,
          dateOfBirth: patient.dateOfBirth,
          medicalRecordNumber: patient.medicalRecordNumber,
          email: patient.email ?? ''
        });
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading patient:', error);
        this.errorMessage.set('Failed to load patient');
        this.isLoading.set(false);
      }
    });
  }

  onSubmit(): void {
    const currentPatient = this.patient();
    
    // Validation
    if (!currentPatient.firstName || !currentPatient.lastName || 
        !currentPatient.dateOfBirth || !currentPatient.medicalRecordNumber) {
      this.errorMessage.set('Please fill in all fields');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set('');

    if (this.isEditMode() && this.patientId) {
      // Update existing patient
      const updateData: UpdatePatient = {
        firstName: currentPatient.firstName,
        lastName: currentPatient.lastName,
        dateOfBirth: currentPatient.dateOfBirth,
        email: currentPatient.email
      };

      this.patientService.updatePatient(this.patientId, updateData).subscribe({
        next: () => {
          this.navigateAfterEdit();
        },
        error: (error) => {
          console.error('Error updating patient:', error);
          this.errorMessage.set(error.error?.message || 'Failed to update patient');
          this.isSaving.set(false);
        }
      });
    } else {
      // Create new patient
      this.patientService.createPatient(currentPatient).subscribe({
        next: () => {
          this.router.navigate(['/patients']);
        },
        error: (error) => {
          console.error('Error creating patient:', error);
          this.errorMessage.set(error.error?.message || 'Failed to create patient');
          this.isSaving.set(false);
        }
      });
    }
  }

  cancel(): void {
    if (this.isEditMode()) {
      this.navigateAfterEdit();
      return;
    }
    this.router.navigate(['/patients']);
  }

  private navigateAfterEdit(): void {
    if (this.returnTo === 'patient-detail' && this.returnPatientId) {
      this.router.navigate(['/patients', this.returnPatientId]);
      return;
    }

    this.router.navigate(['/patients']);
  }

  getMaxDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  getMinDate(): string {
    const date = new Date();
    date.setFullYear(date.getFullYear() - 120);
    return date.toISOString().split('T')[0];
  }

  // Helper methods for two-way binding with signals
  updatePatientField(field: keyof CreatePatient, value: string): void {
    this.patient.update(p => ({ ...p, [field]: value }));
  }

  getPatientField(field: keyof CreatePatient): string {
    return this.patient()[field] ?? '';
  }
}
