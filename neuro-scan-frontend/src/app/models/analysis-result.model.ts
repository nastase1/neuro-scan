export interface AnalysisResult {
  id: string;
  mriScanId: string;
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
  // Report
  medicalReportText: string | null;
  analyzedAt: Date;
}

export interface MriScan {
  id: string;
  patientId: string;
  originalFileName: string;
  storedFilePath: string;
  uploadDate: Date;
  status: ScanStatus;
  reviewedByDoctorId?: string;
  correctedMaskPath?: string;
  reviewedAt?: Date;
  analysisResult?: AnalysisResult;
}

export enum ScanStatus {
  Uploaded = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  UnderReview = 4
}

export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: Date;
  medicalRecordNumber: string;
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
