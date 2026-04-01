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

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
  currentPassword?: string;
  newPassword?: string;
}

export interface UpdateProfileResponse {
  success: boolean;
  message?: string;
  user?: User;
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
  storedFilePath: string;
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
  // Tumor detection
  tumorDetected: boolean;
  tumorVolume: number;
  tumorSurfaceArea: number;
  // Cortex thickness
  cortexThicknessAvg: number;
  cortexThicknessMin: number;
  cortexThicknessMax: number;
  // White matter density
  wmDensityScore: number;
  wmMeanIntensity: number;
  wmCoefficientOfVariation: number;
  // Segmentation image
  segmentationImagePath?: string;
  segmentationSliceCount?: number;
  // Tumor overlay
  tumorOverlaySliceCount?: number;
  medicalReportText?: string;
  analyzedAt: string;
  doctorApproved?: boolean;
  doctorReviewNotes?: string;
}

// Patient evolution interfaces
export interface PatientEvolution {
  patientId: string;
  patientName: string;
  dataPoints: EvolutionDataPoint[];
  summary: EvolutionSummary;
  llmInterpretation?: string;
}

export interface EvolutionDataPoint {
  scanId: string;
  scanDate: string;
  csfVolume: number;
  gmVolume: number;
  wmVolume: number;
  totalBrainVolume: number;
  asymmetryIndex: number;
  epilepsyRiskScore: number;
  tumorDetected: boolean;
  tumorVolume: number;
  tumorSurfaceArea: number;
  cortexThicknessAvg: number;
  wmDensityScore: number;
}

export interface EvolutionSummary {
  totalScans: number;
  monthsSpan: number;
  brainVolumeDelta: number;
  gmVolumeDelta: number;
  wmVolumeDelta: number;
  csfVolumeDelta: number;
  tumorVolumeDelta: number;
  cortexThicknessDelta: number;
  wmDensityDelta: number;
  brainVolumeChangeRate: number;
  degradationLevel: string;
}

// 3D mesh interfaces
export interface MeshDataResponse {
  success: boolean;
  regions: { [key: string]: MeshRegion };
}

export interface MeshRegion {
  vertices: number[][];
  faces: number[][];
  color: string;
  opacity: number;
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
