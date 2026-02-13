import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MriScan, AnalysisResult } from '../models/analysis-result.model';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class MriService {
  private apiUrl = 'http://localhost:5000/api';

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

  uploadScan(patientId: string, file: File): Observable<MriScan> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('patientId', patientId);

    return this.http.post<MriScan>(
      `${this.apiUrl}/mriscans/upload`,
      formData,
      { headers: this.getHeaders() }
    );
  }

  getScanById(scanId: string): Observable<MriScan> {
    return this.http.get<MriScan>(
      `${this.apiUrl}/mriscans/${scanId}`,
      { headers: this.getHeaders() }
    );
  }

  getAnalysisResult(scanId: string): Observable<AnalysisResult> {
    return this.http.get<AnalysisResult>(
      `${this.apiUrl}/mriscans/${scanId}/analysis`,
      { headers: this.getHeaders() }
    );
  }

  getAllScans(): Observable<MriScan[]> {
    return this.http.get<MriScan[]>(
      `${this.apiUrl}/mriscans`,
      { headers: this.getHeaders() }
    );
  }
}
