import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MriService } from '../../services/mri.service';
import { PatientService } from '../../services/patient.service';
import { AuthService } from '../../services/auth.service';
import { User, AnalysisResult, MriScanDetail, Patient } from '../../models/api.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  currentUser: User | null = null;
  
  // Upload states
  isDragging = false;
  isUploading = false;
  uploadProgress = 0;
  uploadError = '';
  
  // Analysis states
  isAnalyzing = false;
  analysisComplete = false;
  
  // Patient selection
  selectedPatientId: string | null = null;
  availablePatients: Patient[] = [];
  showPatientSelector = false;
  
  // Analysis results
  analysisResult: AnalysisResult | null = null;
  
  // Model 1 (UNet) metrics
  csfVolume = 0;
  gmVolume = 0;
  wmVolume = 0;
  asymmetryIndex = 0;
  
  // Model 2 (SegResNet) metrics
  csfVolumeModel2 = 0;
  gmVolumeModel2 = 0;
  wmVolumeModel2 = 0;
  asymmetryIndexModel2 = 0;
  
  // Comparison metrics
  diceScoreCsf = 0;
  diceScoreGm = 0;
  diceScoreWm = 0;
  avgDiceScore = 0;
  disagreementPercentage = 0;
  recommendedModel = 'unet';
  modelConfidence = 0;
  
  medicalReport = '';
  
  // UI states
  selectedFile: File | null = null;
  scanId: string | null = null;
  pollingInterval: any;

  constructor(
    private mriService: MriService,
    private patientService: PatientService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
    
    // Load available patients
    this.loadPatients();
  }

  ngOnDestroy(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }

  loadPatients(): void {
    console.log('Loading patients...');
    this.patientService.getAllPatients().subscribe({
      next: (patients) => {
        console.log('Patients loaded:', patients.length, patients);
        this.availablePatients = patients;
        if (patients.length > 0) {
          this.selectedPatientId = patients[0].id;
          console.log('Selected patient:', this.selectedPatientId);
        }
      },
      error: (error) => {
        console.error('Failed to load patients:', error);
      }
    });
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFileSelection(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFileSelection(input.files[0]);
    }
  }

  private handleFileSelection(file: File): void {
    // Validate file type
    const validExtensions = ['.nii', '.nii.gz', '.dcm'];
    const isValidType = validExtensions.some(ext => file.name.toLowerCase().endsWith(ext));
    
    if (!isValidType) {
      alert('Please upload a valid MRI file (.nii, .nii.gz, or .dcm)');
      return;
    }
    
    // Check if patient is selected
    if (!this.selectedPatientId) {
      alert('Please select a patient first');
      this.showPatientSelector = true;
      return;
    }
    
    this.selectedFile = file;
    this.uploadError = '';
    this.startUpload();
  }

  private startUpload(): void {
    if (!this.selectedFile || !this.selectedPatientId) return;
    
    this.isUploading = true;
    this.uploadProgress = 0;
    this.uploadError = '';
    
    console.log('Starting upload for patient:', this.selectedPatientId);
    console.log('File:', this.selectedFile.name, 'Size:', this.selectedFile.size);
    
    // Real API call
    this.mriService.uploadScan(this.selectedPatientId, this.selectedFile).subscribe({
      next: (response) => {
        console.log('Upload successful:', response);
        this.scanId = response.scanId; // Backend returns 'scanId' property
        this.isUploading = false;
        this.uploadProgress = 100;
        this.startAnalysis();
      },
      error: (error) => {
        console.error('Upload failed:', error);
        this.isUploading = false;
        this.uploadError = error.error?.message || error.message || 'Upload failed. Please try again.';
        alert(this.uploadError);
      }
    });
  }

  private startAnalysis(): void {
    if (!this.scanId) {
      console.error('No scan ID available for analysis');
      return;
    }
    
    this.isAnalyzing = true;
    console.log('Starting analysis for scan:', this.scanId);
    
    // Poll for results every 2 seconds
    this.pollForResults();
  }

  private pollForResults(): void {
    if (!this.scanId) return;
    
    let pollAttempts = 0;
    const maxAttempts = 60; // 2 minutes max (60 * 2 seconds)
    
    this.pollingInterval = setInterval(() => {
      pollAttempts++;
      console.log(`Polling attempt ${pollAttempts}/${maxAttempts}`);
      
      this.mriService.getScanDetails(this.scanId!).subscribe({
        next: (result: MriScanDetail) => {
          console.log('Scan status:', result.status);
          
          if (result.analysisResult) {
            console.log('Analysis complete!', result.analysisResult);
            clearInterval(this.pollingInterval);
            this.analysisResult = result.analysisResult;
            this.displayResults(result.analysisResult);
            this.isAnalyzing = false;
            this.analysisComplete = true;
          } else if (result.status === 3) { // Failed status
            clearInterval(this.pollingInterval);
            this.isAnalyzing = false;
            alert('Analysis failed. Please try again or contact support.');
          } else if (pollAttempts >= maxAttempts) {
            clearInterval(this.pollingInterval);
            this.isAnalyzing = false;
            alert('Analysis is taking longer than expected. Please check back later.');
          }
        },
        error: (error: any) => {
          console.error('Failed to fetch results:', error);
          if (pollAttempts >= maxAttempts) {
            clearInterval(this.pollingInterval);
            this.isAnalyzing = false;
            alert('Unable to retrieve analysis results. Please try again.');
          }
        }
      });
    }, 2000); // Poll every 2 seconds
  }

  private displayResults(result: AnalysisResult): void {
    console.log('Displaying results:', result);
    
    // Model 1 (UNet) - use existing properties
    this.csfVolume = result.csfVolume;
    this.gmVolume = result.gmVolume;
    this.wmVolume = result.wmVolume;
    this.asymmetryIndex = result.asymmetryIndex;
    
    // Model 2 (SegResNet)
    this.csfVolumeModel2 = result.csfVolumeModel2;
    this.gmVolumeModel2 = result.gmVolumeModel2;
    this.wmVolumeModel2 = result.wmVolumeModel2;
    this.asymmetryIndexModel2 = result.asymmetryIndexModel2;
    
    // Comparison metrics
    this.diceScoreCsf = result.diceScoreCsf;
    this.diceScoreGm = result.diceScoreGm;
    this.diceScoreWm = result.diceScoreWm;
    this.avgDiceScore = (this.diceScoreCsf + this.diceScoreGm + this.diceScoreWm) / 3;
    this.disagreementPercentage = result.disagreementPercentage;
    this.recommendedModel = result.recommendedModel || 'unet';
    this.modelConfidence = result.modelConfidence;
    
    // Use backend medical report if available, otherwise generate fallback
    this.medicalReport = result.medicalReportText || this.generateFallbackReport();
    
    this.isAnalyzing = false;
    this.analysisComplete = true;
  }

  private generateFallbackReport(): string {
    return `NEURO-IMAGING ANALYSIS REPORT
DUAL-MODEL AI COMPARISON

Patient MRI Analysis - Generated ${new Date().toLocaleDateString()}

MODEL AGREEMENT & CONFIDENCE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Overall Model Confidence: ${this.modelConfidence.toFixed(1)}%
Recommended Model: ${this.recommendedModel.toUpperCase()}
Disagreement: ${this.disagreementPercentage.toFixed(1)}%

Dice Scores (Model Agreement):
• CSF Agreement: ${(this.diceScoreCsf * 100).toFixed(1)}%
• GM Agreement: ${(this.diceScoreGm * 100).toFixed(1)}%
• WM Agreement: ${(this.diceScoreWm * 100).toFixed(1)}%

VOLUMETRIC ANALYSIS - UNet Model:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cerebrospinal Fluid (CSF): ${this.csfVolume} mL
Grey Matter (GM): ${this.gmVolume} mL
White Matter (WM): ${this.wmVolume} mL
Brain Asymmetry Index: ${this.asymmetryIndex}%

VOLUMETRIC ANALYSIS - SegResNet Model:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cerebrospinal Fluid (CSF): ${this.csfVolumeModel2} mL
Grey Matter (GM): ${this.gmVolumeModel2} mL
White Matter (WM): ${this.wmVolumeModel2} mL
Brain Asymmetry Index: ${this.asymmetryIndexModel2}%

CLINICAL INTERPRETATION:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Both AI models demonstrate ${this.avgDiceScore > 0.95 ? 'excellent' : this.avgDiceScore > 0.90 ? 'good' : 'moderate'} agreement.
The MRI scan demonstrates brain structure with measured tissue volumes.
Asymmetry indices from both models are recorded.

RECOMMENDATIONS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

• Professional medical review recommended
• Model agreement: ${this.modelConfidence.toFixed(1)}%
• Follow clinical guidelines

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Dual-Model AI Analysis | Requires physician validation
Generated by NeuroScan AI v2.0`;
  }

  resetAnalysis(): void {
    this.selectedFile = null;
    this.isUploading = false;
    this.uploadProgress = 0;
    this.uploadError = '';
    this.isAnalyzing = false;
    this.analysisComplete = false;
    this.analysisResult = null;
    this.scanId = null;
    
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }

  getAsymmetryStatus(): 'normal' | 'warning' | 'critical' {
    if (this.asymmetryIndex < 10) return 'normal';
    if (this.asymmetryIndex < 15) return 'warning';
    return 'critical';
  }

  getAsymmetryColor(): string {
    const status = this.getAsymmetryStatus();
    if (status === 'normal') return 'from-emerald-500 to-teal-500';
    if (status === 'warning') return 'from-yellow-500 to-orange-500';
    return 'from-red-500 to-pink-500';
  }

  getAsymmetryShadow(): string {
    const status = this.getAsymmetryStatus();
    if (status === 'normal') return 'shadow-emerald-500/40';
    if (status === 'warning') return 'shadow-yellow-500/40';
    return 'shadow-red-500/40';
  }
}
