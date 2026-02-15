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
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
}

export enum UserRole {
  StandardUser = 0,
  Doctor = 1
}

export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  medicalRecordNumber: string;
  age: number;
}

export interface CreatePatient {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  medicalRecordNumber: string;
}

export interface UpdatePatient {
  firstName?: string;
  lastName?: string;
  dateOfBirth?: string;
}

export interface MriScanUpload {
  patientId: string;
  file: File;
}

export interface MriScanResponse {
  id: string;
  patientId: string;
  originalFileName: string;
  uploadDate: string;
  status: ScanStatus;
}

export interface MriScanDetail {
  id: string;
  patientId: string;
  patient: PatientBasic;
  originalFileName: string;
  uploadDate: string;
  status: ScanStatus;
  analysisResult?: AnalysisResult;
}

export interface PatientBasic {
  id: string;
  firstName: string;
  lastName: string;
  medicalRecordNumber: string;
}

export interface AnalysisResult {
  id: string;
  csfVolume: number;
  gmVolume: number;
  wmVolume: number;
  asymmetryIndex: number;
  medicalReportText?: string;
  analyzedAt: string;
}

export enum ScanStatus {
  Uploaded = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  UnderReview = 4
}
