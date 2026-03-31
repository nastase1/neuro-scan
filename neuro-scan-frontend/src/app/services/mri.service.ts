import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MriScanResponse, MriScanDetail, AnalysisResult, PatientEvolution, MeshDataResponse } from '../models/api.models';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MriService {
  private apiUrl = environment.apiUrl;

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

  uploadSelfScan(file: File): Observable<MriScanResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<MriScanResponse>(
      `${this.apiUrl}/mriscan/upload-self`,
      formData,
      { headers: this.getHeaders() }
    );
  }

  uploadTumorScan(patientId: string, t1: File, t1ce: File, t2: File, flair: File): Observable<MriScanResponse> {
    const formData = new FormData();
    formData.append('PatientId', patientId);
    formData.append('T1File', t1);
    formData.append('T1ceFile', t1ce);
    formData.append('T2File', t2);
    formData.append('FlairFile', flair);

    return this.http.post<MriScanResponse>(
      `${this.apiUrl}/mriscan/upload-tumor`,
      formData,
      { headers: this.getHeaders() }
    );
  }

  uploadSelfTumorScan(t1: File, t1ce: File, t2: File, flair: File): Observable<MriScanResponse> {
    const formData = new FormData();
    formData.append('t1', t1);
    formData.append('t1ce', t1ce);
    formData.append('t2', t2);
    formData.append('flair', flair);

    return this.http.post<MriScanResponse>(
      `${this.apiUrl}/mriscan/upload-self-tumor`,
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

    getMyPatient(): Observable<any> {
      return this.http.get<any>(
        `${this.apiUrl}/patient/my-patient`,
        { headers: this.getHeaders() }
      );
    }
  getPatientScans(patientId: string): Observable<MriScanDetail[]> {
    return this.http.get<MriScanDetail[]>(
      `${this.apiUrl}/mriscan/patient/${patientId}`,
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

  getSegmentationImage(scanId: string): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/mriscan/${scanId}/segmentation-image`,
      { headers: this.getHeaders(), responseType: 'blob' }
    );
  }

  getSegmentationSlice(scanId: string, sliceIndex: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/mriscan/${scanId}/segmentation-image/${sliceIndex}`,
      { headers: this.getHeaders(), responseType: 'blob' }
    );
  }

  getTumorOverlaySlice(scanId: string, sliceIndex: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/mriscan/${scanId}/tumor-overlay/${sliceIndex}`,
      { headers: this.getHeaders(), responseType: 'blob' }
    );
  }

  getMyScans(): Observable<MriScanDetail[]> {
    return this.http.get<MriScanDetail[]>(
      `${this.apiUrl}/mriscan/my-scans`,
      { headers: this.getHeaders() }
    );
  }

  getPendingReviewScans(): Observable<MriScanDetail[]> {
    return this.http.get<MriScanDetail[]>(
      `${this.apiUrl}/mriscan/pending-review`,
      { headers: this.getHeaders() }
    );
  }

  getRawSliceCount(scanId: string): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(
      `${this.apiUrl}/mriscan/${scanId}/raw-slice/count`,
      { headers: this.getHeaders() }
    );
  }

  getRawSlice(scanId: string, sliceIndex: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/mriscan/${scanId}/raw-slice/${sliceIndex}`,
      { headers: this.getHeaders(), responseType: 'blob' }
    );
  }

  submitReview(scanId: string, approved: boolean, notes: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/mriscan/${scanId}/review`,
      { approved, notes },
      { headers: this.getHeaders() }
    );
  }

  saveCorrectedSlice(scanId: string, sliceIndex: number, base64Png: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/mriscan/${scanId}/corrected-slice/${sliceIndex}`,
      { base64Png },
      { headers: this.getHeaders() }
    );
  }

  getCorrectedSlice(scanId: string, sliceIndex: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/mriscan/${scanId}/corrected-slice/${sliceIndex}`,
      { headers: this.getHeaders(), responseType: 'blob' }
    );
  }

  getPatientEvolution(patientId: string): Observable<PatientEvolution> {
    return this.http.get<PatientEvolution>(
      `${this.apiUrl}/mriscan/patient/${patientId}/evolution`,
      { headers: this.getHeaders() }
    );
  }

  get3DMesh(scanId: string): Observable<MeshDataResponse> {
    return this.http.get<MeshDataResponse>(
      `${this.apiUrl}/mriscan/${scanId}/3d-mesh`,
      { headers: this.getHeaders() }
    );
  }
}
