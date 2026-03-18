import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AdminStats,
  AdminUser,
  AdminUpdateUser,
  AdminDoctor,
  AdminScan,
  PagedResult,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/admin`;

  getStats(): Observable<AdminStats> {
    return this.http.get<AdminStats>(`${this.base}/stats`);
  }

  getUsers(search?: string, role?: number, page = 1, pageSize = 20): Observable<PagedResult<AdminUser>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (role !== undefined && role !== null) params = params.set('role', role);
    return this.http.get<PagedResult<AdminUser>>(`${this.base}/users`, { params });
  }

  getUser(id: string): Observable<AdminUser> {
    return this.http.get<AdminUser>(`${this.base}/users/${id}`);
  }

  updateUser(id: string, dto: AdminUpdateUser): Observable<void> {
    return this.http.put<void>(`${this.base}/users/${id}`, dto);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/users/${id}`);
  }

  resetPassword(id: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.base}/users/${id}/reset-password`, { newPassword });
  }

  getDoctors(): Observable<AdminDoctor[]> {
    return this.http.get<AdminDoctor[]>(`${this.base}/doctors`);
  }

  getScans(search?: string, status?: number, page = 1, pageSize = 20): Observable<PagedResult<AdminScan>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status !== undefined && status !== null) params = params.set('status', status);
    return this.http.get<PagedResult<AdminScan>>(`${this.base}/scans`, { params });
  }

  deleteScan(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/scans/${id}`);
  }
}
