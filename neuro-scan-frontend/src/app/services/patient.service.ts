import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Patient, CreatePatient, UpdatePatient } from '../models/api.models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private apiUrl = 'http://localhost:5133/api';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  getAllPatients(): Observable<Patient[]> {
    return this.http.get<Patient[]>(
      `${this.apiUrl}/patient`,
      { headers: this.getHeaders() }
    );
  }

  getPatientById(patientId: string): Observable<Patient> {
    return this.http.get<Patient>(
      `${this.apiUrl}/patient/${patientId}`,
      { headers: this.getHeaders() }
    );
  }

  createPatient(patient: CreatePatient): Observable<Patient> {
    return this.http.post<Patient>(
      `${this.apiUrl}/patient`,
      patient,
      { headers: this.getHeaders() }
    );
  }

  updatePatient(patientId: string, patient: UpdatePatient): Observable<Patient> {
    return this.http.put<Patient>(
      `${this.apiUrl}/patient/${patientId}`,
      patient,
      { headers: this.getHeaders() }
    );
  }

  deletePatient(patientId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/patient/${patientId}`,
      { headers: this.getHeaders() }
    );
  }
}
