import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MriScanResponse, MriScanDetail, AnalysisResult } from '../models/api.models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class MriService {
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

  uploadScan(patientId: string, file: File): Observable<MriScanResponse> {
    const formData = new FormData();
    formData.append('File', file);
    formData.append('PatientId', patientId);

    return this.http.post<MriScanResponse>(
      `${this.apiUrl}/mriscan/upload`,
      formData,
      { headers: this.getHeaders() }
    );
  }

  getScanDetails(scanId: string): Observable<MriScanDetail> {
    return this.http.get<MriScanDetail>(
      `${this.apiUrl}/mriscan/${scanId}`,
      { headers: this.getHeaders() }
    );
  }

  submitCorrectedMask(scanId: string, file: File): Observable<void> {
    const formData = new FormData();
    formData.append('correctedMask', file);

    return this.http.post<void>(
      `${this.apiUrl}/mriscan/${scanId}/correct-mask`,
      formData,
      { headers: this.getHeaders() }
    );
  }

  getAllScans(): Observable<MriScanDetail[]> {
    return this.http.get<MriScanDetail[]>(
      `${this.apiUrl}/mriscan`,
      { headers: this.getHeaders() }
    );
  }
}
