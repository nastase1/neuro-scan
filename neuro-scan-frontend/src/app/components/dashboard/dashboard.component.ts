import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MriService } from '../../services/mri.service';
import { AuthService } from '../../services/auth.service';
import { User, AnalysisResult, ScanStatus } from '../../models/analysis-result.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  currentUser: User | null = null;
  
  // Upload states
  isDragging = false;
  isUploading = false;
  uploadProgress = 0;
  
  // Analysis states
  isAnalyzing = false;
  analysisComplete = false;
  
  // Analysis results
  analysisResult: AnalysisResult | null = null;
  
  // Metrics for display
  csfVolume = 0;
  gmVolume = 0;
  wmVolume = 0;
  asymmetryIndex = 0;
  medicalReport = '';
  
  // UI states
  selectedFile: File | null = null;
  scanId: string | null = null;

  constructor(
    private mriService: MriService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
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
    const validTypes = ['image/jpeg', 'image/png', 'application/dicom', 'image/nifti'];
    const validExtensions = ['.nii', '.nii.gz', '.dcm', '.jpg', '.jpeg', '.png'];
    
    const isValidType = validTypes.includes(file.type) || 
                       validExtensions.some(ext => file.name.toLowerCase().endsWith(ext));
    
    if (!isValidType) {
      alert('Please upload a valid MRI file (DICOM, NIfTI, JPEG, PNG)');
      return;
    }
    
    this.selectedFile = file;
    this.startUpload();
  }

  private startUpload(): void {
    if (!this.selectedFile) return;
    
    this.isUploading = true;
    this.uploadProgress = 0;
    
    // Mock upload progress
    const progressInterval = setInterval(() => {
      this.uploadProgress += 10;
      if (this.uploadProgress >= 100) {
        clearInterval(progressInterval);
        this.isUploading = false;
        this.startAnalysis();
      }
    }, 300);
    
    // Uncomment for real API integration
    /*
    const patientId = 'your-patient-id'; // Get from form or context
    this.mriService.uploadScan(patientId, this.selectedFile).subscribe({
      next: (scan) => {
        this.scanId = scan.id;
        this.isUploading = false;
        this.startAnalysis();
      },
      error: (error) => {
        console.error('Upload failed:', error);
        this.isUploading = false;
        alert('Upload failed. Please try again.');
      }
    });
    */
  }

  private startAnalysis(): void {
    this.isAnalyzing = true;
    
    // Mock analysis with delay
    setTimeout(() => {
      this.completeAnalysis();
    }, 3000);
    
    // Uncomment for real API integration
    /*
    if (this.scanId) {
      this.pollForResults();
    }
    */
  }

  private pollForResults(): void {
    if (!this.scanId) return;
    
    const pollInterval = setInterval(() => {
      this.mriService.getAnalysisResult(this.scanId!).subscribe({
        next: (result) => {
          if (result) {
            clearInterval(pollInterval);
            this.analysisResult = result;
            this.displayResults(result);
          }
        },
        error: (error) => {
          console.error('Failed to fetch results:', error);
          clearInterval(pollInterval);
        }
      });
    }, 2000);
  }

  private completeAnalysis(): void {
    // Mock data for demonstration
    this.csfVolume = 145.3;
    this.gmVolume = 832.7;
    this.wmVolume = 512.8;
    this.asymmetryIndex = 8.5;
    this.medicalReport = `NEURO-IMAGING ANALYSIS REPORT

Patient MRI Analysis - Generated ${new Date().toLocaleDateString()}

VOLUMETRIC ANALYSIS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cerebrospinal Fluid (CSF): ${this.csfVolume} mL
- Within normal limits for age group
- No indications of hydrocephalus

Grey Matter (GM): ${this.gmVolume} mL
- Volume consistent with healthy baseline
- Cortical thickness appears normal

White Matter (WM): ${this.wmVolume} mL
- No significant white matter hyperintensities detected
- Structural integrity maintained

ASYMMETRY ANALYSIS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Brain Asymmetry Index: ${this.asymmetryIndex}%
- Below clinical significance threshold (< 10%)
- Bilateral hemisphere volumes within normal variance

CLINICAL INTERPRETATION:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The MRI scan demonstrates normal brain structure with appropriate
tissue volumes for the patient's demographic profile. No acute
pathological findings detected. Asymmetry index within acceptable
range, suggesting balanced cerebral development.

RECOMMENDATIONS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

• Routine follow-up in 12 months
• Continue current treatment plan if applicable
• No immediate intervention required

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
AI-Assisted Analysis | Requires physician validation
Generated by NeuroScan AI v2.0`;
    
    this.isAnalyzing = false;
    this.analysisComplete = true;
  }

  private displayResults(result: AnalysisResult): void {
    this.csfVolume = result.csfVolume;
    this.gmVolume = result.gmVolume;
    this.wmVolume = result.wmVolume;
    this.asymmetryIndex = result.asymmetryIndex;
    this.medicalReport = result.medicalReportText || 'No report available';
    
    this.isAnalyzing = false;
    this.analysisComplete = true;
  }

  resetAnalysis(): void {
    this.selectedFile = null;
    this.isUploading = false;
    this.uploadProgress = 0;
    this.isAnalyzing = false;
    this.analysisComplete = false;
    this.analysisResult = null;
    this.scanId = null;
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
