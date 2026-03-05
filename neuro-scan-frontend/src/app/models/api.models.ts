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
  // Model 1 (UNet) results
  csfVolume: number;
  gmVolume: number;
  wmVolume: number;
  asymmetryIndex: number;
  // Model 2 (SegResNet) results
  csfVolumeModel2: number;
  gmVolumeModel2: number;
  wmVolumeModel2: number;
  asymmetryIndexModel2: number;
  // Comparison metrics
  diceScoreCsf: number;
  diceScoreGm: number;
  diceScoreWm: number;
  disagreementPercentage: number;
  recommendedModel?: string;
  modelConfidence: number;
  medicalReportText?: string;
  analyzedAt: string;
}

export enum ScanStatus {
  Uploaded = 0,
  Processing = 1,
  Analyzed = 2,
  Failed = 3,
  ReviewedByDoctor = 4
}
