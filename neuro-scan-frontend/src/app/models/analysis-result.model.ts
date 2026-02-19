export interface AnalysisResult {
  id: string;
  mriScanId: string;
  // Model 1 (UNet) metrics
  csfVolume: number;
  gmVolume: number;
  wmVolume: number;
  asymmetryIndex: number;
  // Model 2 (SegResNet) metrics
  csfVolumeModel2: number;
  gmVolumeModel2: number;
  wmVolumeModel2: number;
  asymmetryIndexModel2: number;
  // Comparison metrics
  diceScoreCsf: number;
  diceScoreGm: number;
  diceScoreWm: number;
  disagreementPercentage: number;
  recommendedModel: string;
  modelConfidence: number;
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
