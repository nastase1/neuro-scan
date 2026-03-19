import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MriService } from '../../services/mri.service';
import { PatientService } from '../../services/patient.service';
import { AuthService } from '../../services/auth.service';
import { User, AnalysisResult, MriScanDetail, Patient, ScanStatus } from '../../models/api.models';
import jsPDF from 'jspdf';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  readonly Math = Math;
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
  patientSearchQuery = signal('');
  isPatientDropdownOpen = signal(false);

  get filteredPatients(): Patient[] {
    const q = this.patientSearchQuery().toLowerCase();
    return this.availablePatients().filter(p =>
      !q ||
      p.firstName.toLowerCase().includes(q) ||
      p.lastName.toLowerCase().includes(q) ||
      p.medicalRecordNumber.toLowerCase().includes(q)
    );
  }

  get selectedPatientLabel(): string {
    const p = this.availablePatients().find(p => p.id === this.selectedPatientId());
    return p ? `${p.firstName} ${p.lastName} — MRN: ${p.medicalRecordNumber}` : 'Select a patient...';
  }

  selectPatient(patientId: string): void {
    this.selectedPatientId.set(patientId);
    this.isPatientDropdownOpen.set(false);
    this.patientSearchQuery.set('');
  }

  togglePatientDropdown(): void {
    this.isPatientDropdownOpen.set(!this.isPatientDropdownOpen());
    if (this.isPatientDropdownOpen()) {
      setTimeout(() => {
        const input = document.getElementById('patientSearchInput');
        if (input) input.focus();
      }, 50);
    }
  }

  closePatientDropdown(): void {
    this.isPatientDropdownOpen.set(false);
    this.patientSearchQuery.set('');
  }
  showPatientSelector = false;
  
  // Analysis results - using signals for zoneless change detection
  analysisResult = signal<AnalysisResult | null>(null);
  
  // SegResNet metrics
  csfVolume = signal(0);
  gmVolume = signal(0);
  wmVolume = signal(0);
  asymmetryIndex = signal(0);
  
  // Epilepsy risk
  epilepsyRiskScore = signal(0);
  epilepsyRiskLevel = signal('Low');
  
  // Segmentation image URL and slice navigation
  segmentationImageUrl = signal<string | null>(null);
  segmentationSliceCount = signal(0);
  currentSliceIndex = signal(0);
  isLoadingSlice = signal(false);
  
  medicalReport = signal('');
  
  // UI states
  selectedFile: File | null = null;
  scanId: string | null = null;
  scanSource: 'user-history' | 'doctor-history' | null = null;
  sourcePatientId: string | null = null;
  pollingInterval: any;

  private segmentationObjectUrl: string | null = null;

  constructor(
    private mriService: MriService,
    private patientService: PatientService,
    private route: ActivatedRoute,
    private router: Router,
    public authService: AuthService
  ) {}

  isDoctor(): boolean {
    return this.authService.isDoctor();
  }

  isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  isStandardUser(): boolean {
    return this.authService.isStandardUser();
  }

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;

      if (user && this.authService.isAdmin()) {
        this.router.navigate(['/admin']);
      }
    });
    
    if (this.authService.isDoctor()) {
      this.loadPatients();
    }
    
    // Check if we have a scanId in query params (coming from scan list)
    this.route.queryParams.subscribe(params => {
      const scanId = params['scanId'];
      const source = params['source'];
      const patientId = params['patientId'];

      this.scanSource = source === 'user-history' || source === 'doctor-history' ? source : null;
      this.sourcePatientId = typeof patientId === 'string' ? patientId : null;

      if (scanId) {
        console.log('Loading scan from query param:', scanId);
        this.loadExistingScan(scanId);
      }
    });
  }

  ngOnDestroy(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
    if (this.segmentationObjectUrl) {
      URL.revokeObjectURL(this.segmentationObjectUrl);
    }
  }

  loadPatients(): void {
    if (!this.authService.isDoctor()) {
      return;
    }

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

  loadExistingScan(scanId: string): void {
    console.log('Loading existing scan:', scanId);
    this.scanId = scanId;
    this.isAnalyzing.set(true);
    
    this.mriService.getScanDetails(scanId).subscribe({
      next: (result: MriScanDetail) => {
        console.log('Scan loaded:', result);
        
        if (result.analysisResult) {
          // Display the analysis results
          this.analysisResult.set(result.analysisResult);
          this.displayResults(result.analysisResult);
          this.isAnalyzing.set(false);
          this.analysisComplete.set(true);
        } else if (result.status === ScanStatus.Processing) { // Processing
          // Still processing, start polling
          this.isAnalyzing.set(true);
          this.pollForResults();
        } else {
          // No results yet or failed
          this.isAnalyzing.set(false);
          if (result.status === ScanStatus.Failed) { // Failed
            alert('This scan analysis has failed.');
          } else {
            alert('Analysis results are not ready yet.');
          }
        }
      },
      error: (error: any) => {
        console.error('Failed to load scan:', error);
        this.isAnalyzing.set(false);
        alert('Unable to load scan details. You may not have access to this scan.');
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
    
    // For doctors, check if patient is selected. For standard users, skip patient check.
    if (this.isDoctor() && !this.selectedPatientId()) {
      alert('Please select a patient first');
      this.showPatientSelector = true;
      return;
    }
    
    this.selectedFile = file;
    this.uploadError.set('');
    this.startUpload();
  }

  private startUpload(): void {
    if (!this.selectedFile) return;
    
    // For doctors, patientId is required. For standard users, it's not.
    if (this.isDoctor() && !this.selectedPatientId()) return;
    
    this.isUploading.set(true);
    this.uploadProgress.set(0);
    this.uploadError.set('');
    
    console.log('Starting upload for patient:', this.selectedPatientId());
    console.log('File:', this.selectedFile.name, 'Size:', this.selectedFile.size);
    
    // Call different endpoint based on user role
    const uploadObservable = this.isStandardUser()
      ? this.mriService.uploadSelfScan(this.selectedFile)
      : this.mriService.uploadScan(this.selectedPatientId()!, this.selectedFile);
    
    // Real API call
    uploadObservable.subscribe({
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
          } else if (result.status === ScanStatus.Failed) { // Failed status
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
    
    this.csfVolume.set(result.csfVolume);
    this.gmVolume.set(result.gmVolume);
    this.wmVolume.set(result.wmVolume);
    this.asymmetryIndex.set(result.asymmetryIndex);
    this.epilepsyRiskScore.set(result.epilepsyRiskScore);
    this.epilepsyRiskLevel.set(result.epilepsyRiskLevel ?? 'Low');
    
    // Load segmentation slices
    const sliceCount = result.segmentationSliceCount ?? 0;
    this.segmentationSliceCount.set(sliceCount);
    if (this.scanId && sliceCount > 0) {
      // Start at the middle slice so the most informative cut shows first
      const midSlice = Math.floor(sliceCount / 2);
      this.currentSliceIndex.set(midSlice);
      this.loadSlice(midSlice);
    }
    
    this.medicalReport.set(result.medicalReportText || 'Medical report not available.');
    
    this.isAnalyzing.set(false);
    this.analysisComplete.set(true);
  }

  loadSlice(index: number): void {
    if (!this.scanId) return;
    this.isLoadingSlice.set(true);
    this.mriService.getSegmentationSlice(this.scanId, index).subscribe({
      next: (blob) => {
        if (this.segmentationObjectUrl) {
          URL.revokeObjectURL(this.segmentationObjectUrl);
        }
        this.segmentationObjectUrl = URL.createObjectURL(blob);
        this.segmentationImageUrl.set(this.segmentationObjectUrl);
        this.isLoadingSlice.set(false);
      },
      error: () => {
        this.segmentationImageUrl.set(null);
        this.isLoadingSlice.set(false);
      }
    });
  }

  onSliderChange(event: Event): void {
    const index = parseInt((event.target as HTMLInputElement).value, 10);
    this.currentSliceIndex.set(index);
    this.loadSlice(index);
  }

  canGoBackToHistory(): boolean {
    return !!this.scanId && !!this.scanSource;
  }

  backToHistory(): void {
    if (this.scanSource === 'doctor-history' && this.sourcePatientId) {
      this.router.navigate(['/patients', this.sourcePatientId]);
      return;
    }

    this.router.navigate(['/scan-history']);
  }

  get backToHistoryLabel(): string {
    if (this.scanSource === 'doctor-history') {
      return 'Back to Patient History';
    }

    if (this.scanSource === 'user-history') {
      return 'Back to My History';
    }

    return 'Back to History';
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
    this.segmentationSliceCount.set(0);
    this.currentSliceIndex.set(0);
    if (this.segmentationObjectUrl) {
      URL.revokeObjectURL(this.segmentationObjectUrl);
      this.segmentationObjectUrl = null;
    }
    this.segmentationImageUrl.set(null);
    
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
    doc.setFontSize(14);
    doc.text('Brain MRI Analysis Report — Epilepsy Assessment', pageWidth / 2, yPos, { align: 'center' });
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

    doc.text(`Analysis Date: ${this.analysisResult()?.analyzedAt || new Date().toLocaleDateString()}`, leftMargin, yPos);
    yPos += 12;

    // Epilepsy Risk
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('Epilepsy Risk Assessment', leftMargin, yPos);
    yPos += 7;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text(`Risk Level: ${this.epilepsyRiskLevel()}`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Risk Score: ${this.epilepsyRiskScore().toFixed(0)}/100`, leftMargin, yPos);
    yPos += 12;

    // SegResNet Volumetrics
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('SegResNet Volumetric Analysis', leftMargin, yPos);
    yPos += 7;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text(`CSF Volume: ${this.csfVolume().toFixed(2)} cm3`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Gray Matter Volume: ${this.gmVolume().toFixed(2)} cm3`, leftMargin, yPos);
    yPos += 5;
    doc.text(`White Matter Volume: ${this.wmVolume().toFixed(2)} cm3`, leftMargin, yPos);
    yPos += 5;
    doc.text(`Asymmetry Index: ${this.asymmetryIndex().toFixed(4)}%`, leftMargin, yPos);
    yPos += 12;

    // Medical Report
    if (yPos > 240) { doc.addPage(); yPos = 20; }
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('Medical Report', leftMargin, yPos);
    yPos += 7;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);

    const report = this.medicalReport();
    const lines = doc.splitTextToSize(report, rightMargin - leftMargin);
    for (let i = 0; i < lines.length; i++) {
      if (yPos > 280) { doc.addPage(); yPos = 20; }
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

    const fileName = `NeuroScan_EpilepsyReport_${selectedPatient?.lastName || 'Patient'}_${new Date().toISOString().split('T')[0]}.pdf`;
    doc.save(fileName);
  }
}
