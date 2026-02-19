import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MriService } from '../../services/mri.service';
import { PatientService } from '../../services/patient.service';
import { AuthService } from '../../services/auth.service';
import { User, AnalysisResult, MriScanDetail, Patient } from '../../models/api.models';
import jsPDF from 'jspdf';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  currentUser: User | null = null;
  
  // Upload states - using signals for zoneless change detection
  isDragging = signal(false);
  isUploading = signal(false);
  uploadProgress = signal(0);
  uploadError = signal('');
  
  // Analysis states - using signals for zoneless change detection
  isAnalyzing = signal(false);
  analysisComplete = signal(false);
  
  // Patient selection - using signals for zoneless change detection
  selectedPatientId = signal<string | null>(null);
  availablePatients = signal<Patient[]>([]);
  showPatientSelector = false;
  
  // Analysis results - using signals for zoneless change detection
  analysisResult = signal<AnalysisResult | null>(null);
  
  // Model 1 (UNet) metrics
  csfVolume = signal(0);
  gmVolume = signal(0);
  wmVolume = signal(0);
  asymmetryIndex = signal(0);
  
  // Model 2 (SegResNet) metrics
  csfVolumeModel2 = signal(0);
  gmVolumeModel2 = signal(0);
  wmVolumeModel2 = signal(0);
  asymmetryIndexModel2 = signal(0);
  
  // Comparison metrics
  diceScoreCsf = signal(0);
  diceScoreGm = signal(0);
  diceScoreWm = signal(0);
  avgDiceScore = signal(0);
  disagreementPercentage = signal(0);
  recommendedModel = signal('unet');
  modelConfidence = signal(0);
  
  medicalReport = signal('');
  
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
        this.availablePatients.set(patients);
        if (patients.length > 0) {
          this.selectedPatientId.set(patients[0].id);
          console.log('Selected patient:', patients[0].id);
        }
      },
      error: (error) => {
        console.error('Failed to load patients:', error);
      }
    });
  }

  onPatientChange(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    const patientId = selectElement.value;
    console.log('Patient changed to:', patientId);
    this.selectedPatientId.set(patientId);
    console.log('Updated selectedPatientId signal:', this.selectedPatientId());
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    
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
    if (!this.selectedPatientId()) {
      alert('Please select a patient first');
      this.showPatientSelector = true;
      return;
    }
    
    this.selectedFile = file;
    this.uploadError.set('');
    this.startUpload();
  }

  private startUpload(): void {
    if (!this.selectedFile || !this.selectedPatientId()) return;
    
    this.isUploading.set(true);
    this.uploadProgress.set(0);
    this.uploadError.set('');
    
    console.log('Starting upload for patient:', this.selectedPatientId());
    console.log('File:', this.selectedFile.name, 'Size:', this.selectedFile.size);
    
    // Real API call
    this.mriService.uploadScan(this.selectedPatientId()!, this.selectedFile).subscribe({
      next: (response) => {
        console.log('Upload successful:', response);
        this.scanId = response.scanId; // Backend returns 'scanId' property
        this.isUploading.set(false);
        this.uploadProgress.set(100);
        this.startAnalysis();
      },
      error: (error) => {
        console.error('Upload failed:', error);
        this.isUploading.set(false);
        const errorMsg = error.error?.message || error.message || 'Upload failed. Please try again.';
        this.uploadError.set(errorMsg);
        alert(errorMsg);
      }
    });
  }

  private startAnalysis(): void {
    if (!this.scanId) {
      console.error('No scan ID available for analysis');
      return;
    }
    
    this.isAnalyzing.set(true);
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
            this.analysisResult.set(result.analysisResult);
            this.displayResults(result.analysisResult);
            this.isAnalyzing.set(false);
            this.analysisComplete.set(true);
          } else if (result.status === 3) { // Failed status
            clearInterval(this.pollingInterval);
            this.isAnalyzing.set(false);
            alert('Analysis failed. Please try again or contact support.');
          } else if (pollAttempts >= maxAttempts) {
            clearInterval(this.pollingInterval);
            this.isAnalyzing.set(false);
            alert('Analysis is taking longer than expected. Please check back later.');
          }
        },
        error: (error: any) => {
          console.error('Failed to fetch results:', error);
          if (pollAttempts >= maxAttempts) {
            clearInterval(this.pollingInterval);
            this.isAnalyzing.set(false);
            alert('Unable to retrieve analysis results. Please try again.');
          }
        }
      });
    }, 2000); // Poll every 2 seconds
  }

  private displayResults(result: AnalysisResult): void {
    console.log('Displaying results:', result);
    
    // Model 1 (UNet) - use existing properties
    this.csfVolume.set(result.csfVolume);
    this.gmVolume.set(result.gmVolume);
    this.wmVolume.set(result.wmVolume);
    this.asymmetryIndex.set(result.asymmetryIndex);
    
    // Model 2 (SegResNet)
    this.csfVolumeModel2.set(result.csfVolumeModel2);
    this.gmVolumeModel2.set(result.gmVolumeModel2);
    this.wmVolumeModel2.set(result.wmVolumeModel2);
    this.asymmetryIndexModel2.set(result.asymmetryIndexModel2);
    
    // Comparison metrics
    this.diceScoreCsf.set(result.diceScoreCsf);
    this.diceScoreGm.set(result.diceScoreGm);
    this.diceScoreWm.set(result.diceScoreWm);
    this.avgDiceScore.set((result.diceScoreCsf + result.diceScoreGm + result.diceScoreWm) / 3);
    this.disagreementPercentage.set(result.disagreementPercentage);
    this.recommendedModel.set(result.recommendedModel || 'unet');
    this.modelConfidence.set(result.modelConfidence);
    
    // Use backend medical report if available, otherwise generate fallback
    this.medicalReport.set(result.medicalReportText || this.generateFallbackReport());
    
    this.isAnalyzing.set(false);
    this.analysisComplete.set(true);
  }

  private generateFallbackReport(): string {
    return `NEURO-IMAGING ANALYSIS REPORT
DUAL-MODEL AI COMPARISON

Patient MRI Analysis - Generated ${new Date().toLocaleDateString()}

MODEL AGREEMENT & CONFIDENCE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Overall Model Confidence: ${this.modelConfidence().toFixed(1)}%
Recommended Model: ${this.recommendedModel().toUpperCase()}
Disagreement: ${this.disagreementPercentage().toFixed(1)}%

Dice Scores (Model Agreement):
• CSF Agreement: ${(this.diceScoreCsf() * 100).toFixed(1)}%
• GM Agreement: ${(this.diceScoreGm() * 100).toFixed(1)}%
• WM Agreement: ${(this.diceScoreWm() * 100).toFixed(1)}%

VOLUMETRIC ANALYSIS - UNet Model:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cerebrospinal Fluid (CSF): ${this.csfVolume()} mL
Grey Matter (GM): ${this.gmVolume()} mL
White Matter (WM): ${this.wmVolume()} mL
Brain Asymmetry Index: ${this.asymmetryIndex()}%

VOLUMETRIC ANALYSIS - SegResNet Model:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cerebrospinal Fluid (CSF): ${this.csfVolumeModel2()} mL
Grey Matter (GM): ${this.gmVolumeModel2()} mL
White Matter (WM): ${this.wmVolumeModel2()} mL
Brain Asymmetry Index: ${this.asymmetryIndexModel2()}%

CLINICAL INTERPRETATION:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Both AI models demonstrate ${this.avgDiceScore() > 0.95 ? 'excellent' : this.avgDiceScore() > 0.90 ? 'good' : 'moderate'} agreement.
The MRI scan demonstrates brain structure with measured tissue volumes.
Asymmetry indices from both models are recorded.

RECOMMENDATIONS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

• Professional medical review recommended
• Model agreement: ${this.modelConfidence().toFixed(1)}%
• Follow clinical guidelines

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Dual-Model AI Analysis | Requires physician validation
Generated by NeuroScan AI v2.0`;
  }

  resetAnalysis(): void {
    this.selectedFile = null;
    this.isUploading.set(false);
    this.uploadProgress.set(0);
    this.uploadError.set('');
    this.isAnalyzing.set(false);
    this.analysisComplete.set(false);
    this.analysisResult.set(null);
    this.scanId = null;
    
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }

  getAsymmetryStatus(): 'normal' | 'warning' | 'critical' {
    if (this.asymmetryIndex() < 10) return 'normal';
    if (this.asymmetryIndex() < 15) return 'warning';
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

  exportToPdf(): void {
    const doc = new jsPDF();
    const pageWidth = doc.internal.pageSize.getWidth();
    const leftMargin = 15;
    const rightMargin = pageWidth - 15;
    let yPos = 20;

    // Header
    doc.setFontSize(20);
    doc.setFont('helvetica', 'bold');
    doc.text('NEUROSCAN', pageWidth / 2, yPos, { align: 'center' });
    yPos += 10;
    doc.setFontSize(16);
    doc.text('Brain MRI Analysis Report', pageWidth / 2, yPos, { align: 'center' });
    yPos += 15;

    // Patient Info
    const selectedPatient = this.availablePatients().find(p => p.id === this.selectedPatientId());
    if (selectedPatient) {
      doc.setFontSize(12);
      doc.setFont('helvetica', 'bold');
      doc.text('Patient Information', leftMargin, yPos);
      yPos += 7;
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(10);
      doc.text(`Name: ${selectedPatient.firstName} ${selectedPatient.lastName}`, leftMargin, yPos);
      yPos += 5;
      doc.text(`MRN: ${selectedPatient.medicalRecordNumber}`, leftMargin, yPos);
      yPos += 5;
      doc.text(`Date of Birth: ${new Date(selectedPatient.dateOfBirth).toLocaleDateString()}`, leftMargin, yPos);
      yPos += 10;
    }

    // Analysis Date
    doc.text(`Analysis Date: ${this.analysisResult()?.analyzedAt || new Date().toLocaleDateString()}`, leftMargin, yPos);
    yPos += 12;

    // Model Agreement Section
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('Model Agreement & Confidence', leftMargin, yPos);
    yPos += 7;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text(`Overall Dice Score: ${(this.avgDiceScore() * 100).toFixed(1)}%`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Model Confidence: ${this.modelConfidence().toFixed(1)}%`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Recommended Model: ${this.recommendedModel().toUpperCase()}`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Disagreement: ${this.disagreementPercentage().toFixed(1)}%`, leftMargin, yPos);
    yPos += 12;

    // UNet Model Results
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('UNet Model Results', leftMargin, yPos);
    yPos += 7;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text(`CSF Volume: ${this.csfVolume().toFixed(2)} mL`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Gray Matter Volume: ${this.gmVolume().toFixed(2)} mL`, leftMargin, yPos);
    yPos += 5;
    doc.text(`White Matter Volume: ${this.wmVolume().toFixed(2)} mL`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Brain Asymmetry: ${this.asymmetryIndex().toFixed(2)}%`, leftMargin, yPos);
    yPos += 12;

    // SegResNet Model Results
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('SegResNet Model Results', leftMargin, yPos);
    yPos += 7;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text(`CSF Volume: ${this.csfVolumeModel2().toFixed(2)} mL`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Gray Matter Volume: ${this.gmVolumeModel2().toFixed(2)} mL`, leftMargin, yPos);
    yPos += 5;
    doc.text(`White Matter Volume: ${this.wmVolumeModel2().toFixed(2)} mL`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Brain Asymmetry: ${this.asymmetryIndexModel2().toFixed(2)}%`, leftMargin, yPos);
    yPos += 12;

    // Dice Scores
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('Model Agreement (Dice Scores)', leftMargin, yPos);
    yPos += 7;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text(`CSF Agreement: ${(this.diceScoreCsf() * 100).toFixed(1)}%`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Gray Matter Agreement: ${(this.diceScoreGm() * 100).toFixed(1)}%`, leftMargin, yPos);
    yPos += 5;
    doc.text(`White Matter Agreement: ${(this.diceScoreWm() * 100).toFixed(1)}%`, leftMargin, yPos);
    yPos += 12;

    // Medical Report
    if (yPos > 240) {
      doc.addPage();
      yPos = 20;
    }
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('Medical Report', leftMargin, yPos);
    yPos += 7;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);

    // Split medical report into lines that fit the page width
    const report = this.medicalReport();
    const lines = doc.splitTextToSize(report, rightMargin - leftMargin);
    
    for (let i = 0; i < lines.length; i++) {
      if (yPos > 280) {
        doc.addPage();
        yPos = 20;
      }
      doc.text(lines[i], leftMargin, yPos);
      yPos += 5;
    }

    // Footer
    const pageCount = doc.getNumberOfPages();
    for (let i = 1; i <= pageCount; i++) {
      doc.setPage(i);
      doc.setFontSize(8);
      doc.setFont('helvetica', 'italic');
      doc.text(
        `Page ${i} of ${pageCount} | Generated by NeuroScan AI | ${new Date().toLocaleString()}`,
        pageWidth / 2,
        doc.internal.pageSize.getHeight() - 10,
        { align: 'center' }
      );
    }

    // Save the PDF
    const fileName = `NeuroScan_Analysis_${selectedPatient?.lastName || 'Patient'}_${new Date().toISOString().split('T')[0]}.pdf`;
    doc.save(fileName);
  }
}
