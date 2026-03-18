export interface AuthResponse {
  success: boolean;
  token?: string;
  message?: string;
  user?: User;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role?: UserRole;
  inviteCode?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
  confirmPassword: string;
}

export interface GenericResponse {
  success: boolean;
  message?: string;
}

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  inviteCode?: string;
  assignedDoctorId?: string;
  assignedDoctorName?: string;
}

export enum UserRole {
  StandardUser = 0,
  Doctor = 1,
  Admin = 2
}

export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  medicalRecordNumber: string;
  email?: string;
  age: number;
}

export interface CreatePatient {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  medicalRecordNumber: string;
  email?: string;
}

export interface UpdatePatient {
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
  email?: string;
}

export interface MriScanUpload {
  patientId: string;
  file: File;
}

export interface MriScanResponse {
  scanId: string;
  message: string;
  status: ScanStatus;
}

export interface MriScanDetail {
  id: string;
  patientId: string;
  patient: PatientBasic;
  originalFileName: string;
  uploadDate: string;
  status: ScanStatus;
  doctorClinicalNotes?: string;
  analysisResult?: AnalysisResult;
}

export interface PatientBasic {
  id: string;
  fullName: string;
  firstName?: string;
  lastName?: string;
  medicalRecordNumber: string;
}

export interface AnalysisResult {
  id: string;
  // SegResNet volumetrics
  csfVolume: number;
  gmVolume: number;
  wmVolume: number;
  asymmetryIndex: number;
  // Epilepsy risk
  epilepsyRiskScore: number;
  epilepsyRiskLevel: string; // 'Low' | 'Moderate' | 'High'
  // Segmentation image
  segmentationImagePath?: string;
  segmentationSliceCount?: number;
  medicalReportText?: string;
  analyzedAt: string;
  doctorApproved?: boolean;
  doctorReviewNotes?: string;
}

export enum ScanStatus {
  Uploaded = 0,
  Processing = 1,
  Analyzed = 2,
  Failed = 3,
  ReviewedByDoctor = 4
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminStats {
  totalUsers: number;
  totalDoctors: number;
  totalPatients: number;
  totalScans: number;
  pendingReviews: number;
  analyzedScans: number;
  reviewedScans: number;
  failedScans: number;
}

export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  createdAt: string;
  inviteCode?: string;
  assignedDoctorId?: string;
  assignedDoctorName?: string;
  patientCount: number;
  scanCount: number;
}

export interface AdminUpdateUser {
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
}

export interface AdminPatientSummary {
  id: string;
  fullName: string;
  medicalRecordNumber: string;
  scanCount: number;
  createdAt: string;
}

export interface AdminDoctor {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  inviteCode?: string;
  createdAt: string;
  patientCount: number;
  scanCount: number;
  reviewCount: number;
  patients: AdminPatientSummary[];
}

export interface AdminScan {
  id: string;
  originalFileName: string;
  uploadDate: string;
  status: ScanStatus;
  patientId?: string;
  patientName?: string;
  patientMrn?: string;
  reviewedByDoctorId?: string;
  reviewedByDoctorName?: string;
  reviewedAt?: string;
  doctorApproved?: boolean;
  epilepsyRiskLevel?: string;
}
