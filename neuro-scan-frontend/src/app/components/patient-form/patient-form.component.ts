import { Component, OnInit } from '@angular/core';
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
  isEditMode = false;
  patientId: string | null = null;
  isLoading = false;
  isSaving = false;
  errorMessage = '';

  patient: CreatePatient = {
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    medicalRecordNumber: ''
  };

  constructor(
    private patientService: PatientService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.patientId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.patientId && this.route.snapshot.url.some(segment => segment.path === 'edit');

    if (this.isEditMode && this.patientId) {
      this.loadPatient(this.patientId);
    }
  }

  loadPatient(patientId: string): void {
    this.isLoading = true;
    
    this.patientService.getPatientById(patientId).subscribe({
      next: (patient: Patient) => {
        this.patient = {
          firstName: patient.firstName,
          lastName: patient.lastName,
          dateOfBirth: patient.dateOfBirth,
          medicalRecordNumber: patient.medicalRecordNumber
        };
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading patient:', error);
        this.errorMessage = 'Failed to load patient';
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    // Validation
    if (!this.patient.firstName || !this.patient.lastName || 
        !this.patient.dateOfBirth || !this.patient.medicalRecordNumber) {
      this.errorMessage = 'Please fill in all fields';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    if (this.isEditMode && this.patientId) {
      // Update existing patient
      const updateData: UpdatePatient = {
        firstName: this.patient.firstName,
        lastName: this.patient.lastName,
        dateOfBirth: this.patient.dateOfBirth
      };

      this.patientService.updatePatient(this.patientId, updateData).subscribe({
        next: () => {
          this.router.navigate(['/patients']);
        },
        error: (error) => {
          console.error('Error updating patient:', error);
          this.errorMessage = error.error?.message || 'Failed to update patient';
          this.isSaving = false;
        }
      });
    } else {
      // Create new patient
      this.patientService.createPatient(this.patient).subscribe({
        next: () => {
          this.router.navigate(['/patients']);
        },
        error: (error) => {
          console.error('Error creating patient:', error);
          this.errorMessage = error.error?.message || 'Failed to create patient';
          this.isSaving = false;
        }
      });
    }
  }

  cancel(): void {
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
}
