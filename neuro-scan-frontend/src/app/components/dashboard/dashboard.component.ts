import { Component, OnInit, OnDestroy, signal, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MriService } from '../../services/mri.service';
import { PatientService } from '../../services/patient.service';
import { AuthService } from '../../services/auth.service';
import { User, AnalysisResult, MriScanDetail, Patient, ScanStatus } from '../../models/api.models';
import jsPDF from 'jspdf';
import { catchError, forkJoin, of } from 'rxjs';

interface DashboardStatCard {
  title: string;
  value: string;
  subtitle: string;
  tone: 'cyan' | 'emerald' | 'amber' | 'rose' | 'violet';
}

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
  @ViewChild('patientDropdownWrapper') patientDropdownWrapper?: ElementRef<HTMLDivElement>;
  
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
    return p ? `${p.firstName} ${p.lastName} — MRI: ${p.medicalRecordNumber}` : 'Select Patient';
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

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.isPatientDropdownOpen()) {
      return;
    }

    const target = event.target as Node;
    if (this.patientDropdownWrapper && !this.patientDropdownWrapper.nativeElement.contains(target)) {
      this.closePatientDropdown();
    }
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
  dashboardLoading = signal(false);
  dashboardCards = signal<DashboardStatCard[]>([]);
  dashboardNarrative = signal<string[]>([]);
  dashboardRecentScans = signal<MriScanDetail[]>([]);
  trendScores = signal<number[]>([]);
  trendLabels = signal<string[]>([]);
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

    this.loadDashboardInsights();
    
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
      },
      error: (error) => {
        console.error('Failed to load patients:', error);
      }
    });
  }

  loadDashboardInsights(): void {
    this.dashboardLoading.set(true);

    if (this.authService.isDoctor()) {
      forkJoin({
        pending: this.mriService.getPendingReviewScans().pipe(catchError(() => of([] as MriScanDetail[]))),
        scans: this.mriService.getAllScans().pipe(catchError(() => of([] as MriScanDetail[])))
      }).subscribe({
        next: ({ pending, scans }) => {
          this.applyDashboardInsights(scans, pending.length);
          this.dashboardLoading.set(false);
        },
        error: () => {
          this.dashboardLoading.set(false);
        }
      });
      return;
    }

    this.mriService.getMyScans().pipe(catchError(() => of([] as MriScanDetail[]))).subscribe({
      next: (scans) => {
        this.applyDashboardInsights(scans, 0);
        this.dashboardLoading.set(false);
      },
      error: () => {
        this.dashboardLoading.set(false);
      }
    });
  }

  private applyDashboardInsights(scans: MriScanDetail[], pendingCount: number): void {
    const sorted = [...scans].sort((a, b) => new Date(b.uploadDate).getTime() - new Date(a.uploadDate).getTime());
    const recent = sorted.slice(0, 6);
    this.dashboardRecentScans.set(recent);

    const scored = sorted.filter(s => s.analysisResult?.epilepsyRiskScore !== undefined);
    const highRiskCount = scored.filter(s => (s.analysisResult?.epilepsyRiskScore ?? 0) >= 70).length;
    const avgRisk = scored.length > 0
      ? scored.reduce((acc, s) => acc + (s.analysisResult?.epilepsyRiskScore ?? 0), 0) / scored.length
      : 0;

    const thisWeekCutoff = Date.now() - 7 * 24 * 60 * 60 * 1000;
    const weeklyScans = sorted.filter(s => new Date(s.uploadDate).getTime() >= thisWeekCutoff).length;

    const trend = scored.slice(0, 8).reverse();
    this.trendScores.set(trend.map(s => Number((s.analysisResult?.epilepsyRiskScore ?? 0).toFixed(1))));
    this.trendLabels.set(trend.map(s => new Date(s.uploadDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })));

    if (this.authService.isDoctor()) {
      this.dashboardCards.set([
        {
          title: 'Pending Reviews',
          value: String(pendingCount),
          subtitle: 'Scans waiting for clinical decision',
          tone: 'amber'
        },
        {
          title: 'Tracked Scans',
          value: String(sorted.length),
          subtitle: 'Scans available in your scope',
          tone: 'cyan'
        },
        {
          title: 'High-Risk Cases',
          value: String(highRiskCount),
          subtitle: 'Risk score >= 70',
          tone: 'rose'
        },
        {
          title: 'Avg Risk Signature',
          value: `${avgRisk.toFixed(1)} / 100`,
          subtitle: 'Mean AI risk across analyzed scans',
          tone: 'violet'
        }
      ]);

      this.dashboardNarrative.set([
        `You have ${pendingCount} review${pendingCount === 1 ? '' : 's'} queued for validation.`,
        `${weeklyScans} scan${weeklyScans === 1 ? '' : 's'} entered the pipeline in the last 7 days.`,
        highRiskCount > 0
          ? `${highRiskCount} case${highRiskCount === 1 ? '' : 's'} currently classify as high risk and should be prioritized.`
          : 'No high-risk cases detected in your current dataset.'
      ]);
      return;
    }

    const reviewedCount = sorted.filter(s => s.status === ScanStatus.ReviewedByDoctor).length;
    const lastReviewed = sorted.find(s => s.status === ScanStatus.ReviewedByDoctor);

    this.dashboardCards.set([
      {
        title: 'My Scans',
        value: String(sorted.length),
        subtitle: 'Total studies in your timeline',
        tone: 'cyan'
      },
      {
        title: 'Reviewed by Doctor',
        value: String(reviewedCount),
        subtitle: 'Completed clinical reviews',
        tone: 'emerald'
      },
      {
        title: 'Current Avg Risk',
        value: `${avgRisk.toFixed(1)} / 100`,
        subtitle: 'Average across analyzed scans',
        tone: 'violet'
      },
      {
        title: 'Last Reviewed',
        value: lastReviewed ? this.formatRelativeDate(lastReviewed.uploadDate) : 'Pending',
        subtitle: 'Most recent doctor-reviewed scan',
        tone: 'amber'
      }
    ]);

    this.dashboardNarrative.set([
      `${reviewedCount} scan${reviewedCount === 1 ? '' : 's'} have already been validated by a physician.`,
      `${weeklyScans} scan${weeklyScans === 1 ? '' : 's'} were uploaded in the past week.`,
      sorted.length > 0
        ? 'Use the history panel to compare your latest report with previous sessions.'
        : 'Start by uploading your first MRI scan to generate an AI report.'
    ]);
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

    this.dashboardNarrative.set(this.buildClinicalNarrative(result));
    
    this.isAnalyzing.set(false);
    this.analysisComplete.set(true);
    this.loadDashboardInsights();
  }

  private buildClinicalNarrative(result: AnalysisResult): string[] {
    const asymmetryStatement = result.asymmetryIndex < 10
      ? 'Inter-hemispheric asymmetry is within a low-variance range.'
      : result.asymmetryIndex < 15
        ? 'Inter-hemispheric asymmetry is moderately elevated and should be monitored.'
        : 'Inter-hemispheric asymmetry is elevated and warrants closer clinical inspection.';

    const tissueLead = [
      { label: 'CSF', value: result.csfVolume },
      { label: 'Gray Matter', value: result.gmVolume },
      { label: 'White Matter', value: result.wmVolume }
    ].sort((a, b) => b.value - a.value)[0];

    return [
      `AI classifies this study as ${result.epilepsyRiskLevel} risk with a score of ${result.epilepsyRiskScore.toFixed(1)}/100.`,
      `${asymmetryStatement}`,
      `${tissueLead.label} is the dominant segmented tissue in this scan.`
    ];
  }

  get trendPolylinePoints(): string {
    const scores = this.trendScores();
    if (scores.length === 0) {
      return '';
    }

    const width = 420;
    const height = 130;
    const step = scores.length === 1 ? width : width / (scores.length - 1);

    return scores
      .map((score, index) => {
        const x = index * step;
        const y = height - (Math.max(0, Math.min(score, 100)) / 100) * height;
        return `${x},${y}`;
      })
      .join(' ');
  }

  get trendFillPoints(): string {
    const line = this.trendPolylinePoints;
    if (!line) {
      return '';
    }

    return `0,130 ${line} 420,130`;
  }

  get latestTrendScore(): number {
    const scores = this.trendScores();
    return scores.length > 0 ? scores[scores.length - 1] : 0;
  }

  formatRelativeDate(dateValue: string): string {
    const then = new Date(dateValue).getTime();
    const diffMs = Date.now() - then;
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays <= 0) {
      return 'Today';
    }

    if (diffDays === 1) {
      return '1 day ago';
    }

    if (diffDays < 30) {
      return `${diffDays} days ago`;
    }

    const diffMonths = Math.floor(diffDays / 30);
    return diffMonths === 1 ? '1 month ago' : `${diffMonths} months ago`;
  }

  getCardToneClasses(tone: DashboardStatCard['tone']): string {
    const toneMap: Record<DashboardStatCard['tone'], string> = {
      cyan: 'from-cyan-500/20 to-cyan-400/5 border-cyan-400/30',
      emerald: 'from-emerald-500/20 to-emerald-400/5 border-emerald-400/30',
      amber: 'from-amber-500/20 to-amber-400/5 border-amber-400/30',
      rose: 'from-rose-500/20 to-rose-400/5 border-rose-400/30',
      violet: 'from-violet-500/20 to-violet-400/5 border-violet-400/30'
    };

    return toneMap[tone];
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
