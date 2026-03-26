import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { MriService } from '../../services/mri.service';
import { AuthService } from '../../services/auth.service';
import { MriScanDetail, Patient, ScanStatus } from '../../models/api.models';
import { PatientService } from '../../services/patient.service';

interface FaqItem {
  category: FaqCategory;
  question: string;
  answer: string;
}

type FaqCategory = 'Workflow' | 'Interpretation' | 'Security' | 'Presentation';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  isLoading = signal(true);
  scans = signal<MriScanDetail[]>([]);
  pendingReviewCount = signal(0);
  openFaqIndex = signal(0);
  selectedFaqCategory = signal<FaqCategory>('Workflow');

  constructor(
    private mriService: MriService,
    private patientService: PatientService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    if (this.authService.isAdmin()) {
      this.isLoading.set(false);
      return;
    }

    if (this.authService.isDoctor()) {
      this.loadDoctorData();
      return;
    }

    this.loadUserData();
  }

  private loadDoctorData(): void {
    this.mriService.getPendingReviewScans().pipe(catchError(() => of([] as MriScanDetail[]))).subscribe({
      next: (pendingScans) => {
        this.pendingReviewCount.set(pendingScans.length);

        this.mriService.getAllScans().pipe(catchError(() => of([] as MriScanDetail[]))).subscribe({
          next: (allScans) => {
            if (allScans.length > 0) {
              this.scans.set(allScans);
              this.isLoading.set(false);
              return;
            }

            // Fallback 1: pending review scans still provide useful real metrics.
            if (pendingScans.length > 0) {
              this.scans.set(pendingScans);
            }

            // Fallback 2: aggregate scans from doctor's patients to avoid zeroed cards.
            this.patientService.getAllPatients().pipe(catchError(() => of([] as Patient[]))).subscribe({
              next: (patients) => {
                if (!patients.length) {
                  this.isLoading.set(false);
                  return;
                }

                const requests = patients.map((patient) =>
                  this.mriService.getPatientScans(patient.id).pipe(catchError(() => of([] as MriScanDetail[])))
                );

                forkJoin(requests).subscribe({
                  next: (results) => {
                    const flattened = results.flat();
                    if (flattened.length > 0) {
                      this.scans.set(flattened);
                    }
                    this.isLoading.set(false);
                  },
                  error: () => {
                    this.isLoading.set(false);
                  }
                });
              },
              error: () => {
                  this.isLoading.set(false);
              }
            });
          },
          error: () => {
            this.isLoading.set(false);
          }
        });
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  private loadUserData(): void {
    this.mriService.getMyScans().pipe(catchError(() => of([] as MriScanDetail[]))).subscribe({
      next: (scans) => {
        this.scans.set(scans);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  get reviewedCount(): number {
    return this.scans().filter(s => s.status === ScanStatus.ReviewedByDoctor).length;
  }

  get analyzedCount(): number {
    return this.scans().filter(s => s.analysisResult !== undefined).length;
  }

  get highRiskCount(): number {
    return this.scans().filter(s => (s.analysisResult?.epilepsyRiskScore ?? 0) >= 70).length;
  }

  get avgRiskScore(): number {
    const withRisk = this.scans().filter(s => s.analysisResult?.epilepsyRiskScore !== undefined);
    if (withRisk.length === 0) {
      return 0;
    }

    const sum = withRisk.reduce((acc, scan) => acc + (scan.analysisResult?.epilepsyRiskScore ?? 0), 0);
    return sum / withRisk.length;
  }

  get recentScans(): MriScanDetail[] {
    return [...this.scans()]
      .sort((a, b) => new Date(b.uploadDate).getTime() - new Date(a.uploadDate).getTime())
      .slice(0, 5);
  }

  get processingCount(): number {
    return this.scans().filter(s => s.status === ScanStatus.Processing).length;
  }

  get reviewedRate(): number {
    const total = this.scans().length;
    if (total === 0) {
      return 0;
    }

    return (this.reviewedCount / total) * 100;
  }

  get latestRiskLevel(): string {
    const latestWithRisk = [...this.scans()]
      .sort((a, b) => new Date(b.uploadDate).getTime() - new Date(a.uploadDate).getTime())
      .find(s => !!s.analysisResult?.epilepsyRiskLevel);

    return latestWithRisk?.analysisResult?.epilepsyRiskLevel ?? 'Pending';
  }

  get avgRiskLevel(): string {
    const score = this.avgRiskScore;
    if (score === 0) return 'Pending';
    if (score >= 70) return 'High';
    if (score >= 40) return 'Moderate';
    return 'Low';
  }

  get faqCategories(): FaqCategory[] {
    if (this.authService.isDoctor()) {
      return ['Workflow', 'Interpretation', 'Security', 'Presentation'];
    }

    return ['Workflow', 'Interpretation', 'Security'];
  }

  get faqItems(): FaqItem[] {
    if (this.authService.isDoctor()) {
      return [
        {
          category: 'Interpretation',
          question: 'How should I interpret AI risk score vs clinical decision?',
          answer: 'The AI risk score is a triage indicator, not a standalone diagnosis. Use it with segmentation consistency, asymmetry index, and your neurological exam context before approving or rejecting results.'
        },
        {
          category: 'Interpretation',
          question: 'Which metrics matter most when risk score and visual impression differ?',
          answer: 'Prioritize consistency across asymmetry index, GM/WM balance, segmentation quality, and longitudinal trend. A single conflicting indicator should trigger deeper slice-level review, not immediate rejection or acceptance.'
        },
        {
          category: 'Interpretation',
          question: 'Can this platform replace EEG or neurologist evaluation?',
          answer: 'No. NeuroScan supports imaging interpretation but does not replace EEG findings, seizure history, medication response, or specialist neurological assessment.'
        },
        {
          category: 'Workflow',
          question: 'How are scans prioritized in this platform?',
          answer: 'High risk cases (score >= 70), pending review backlog, and recent uploads are surfaced first in your workflow so urgent studies can be addressed faster.'
        },
        {
          category: 'Workflow',
          question: 'Can I correct segmentation before final review?',
          answer: 'Yes. Open a scan in Review, paint corrections directly on slices, save corrected slices, then submit final approval/rejection with clinical notes.'
        },
        {
          category: 'Workflow',
          question: 'What is the recommended review sequence for a new case?',
          answer: 'Start with risk banner and trend, inspect raw and segmented slices in parallel, validate asymmetry outliers, annotate corrections if needed, then finalize approval with concise clinical notes.'
        },
        {
          category: 'Workflow',
          question: 'How should I document rejected AI results?',
          answer: 'In review notes, specify the reason for rejection such as segmentation mismatch, artifact influence, or metric inconsistency, and include next actions like repeat imaging or specialist referral.'
        },
        {
          category: 'Presentation',
          question: 'What should I present to a jury about medical safety?',
          answer: 'Explain that NeuroScan is a decision-support system: it accelerates volumetric interpretation, but the final medical interpretation and treatment recommendation remain physician controlled.'
        },
        {
          category: 'Presentation',
          question: 'How can I explain model transparency in the demo?',
          answer: 'Show that outputs are not a single opaque score: the platform exposes segmentation slices, tissue volumes, asymmetry metrics, and editable overlays, so clinicians can verify and correct AI behavior.'
        },
        {
          category: 'Presentation',
          question: 'What thesis value should be emphasized?',
          answer: 'Emphasize workflow acceleration, standardized reporting, and physician-in-the-loop validation. The novelty is integrating AI interpretation with practical clinical review tools in one platform.'
        },
        {
          category: 'Security',
          question: 'How is patient data protected in NeuroScan?',
          answer: 'Access is role-restricted and token-authenticated. Users only see scans within their authorized scope, and clinical notes are tied to specific scan records for traceability.'
        },
        {
          category: 'Security',
          question: 'Are changes in review traceable?',
          answer: 'Yes. Review status, approval decision, and doctor clinical notes are persisted with scan records, enabling audit-friendly tracking of who reviewed what and when.'
        },
        {
          category: 'Security',
          question: 'What happens if a user account is not linked to a patient record?',
          answer: 'The app falls back to user-scoped scan retrieval so access remains functional, while still enforcing role-based boundaries. Linking should still be completed for full history continuity.'
        }
      ];
    }

    return [
      {
        category: 'Interpretation',
        question: 'Does a high risk score mean I definitely have epilepsy?',
        answer: 'No. The score reflects imaging pattern risk and must be interpreted by your doctor together with your symptoms, history, and additional tests.'
      },
      {
        category: 'Interpretation',
        question: 'What does “Moderate risk” usually mean?',
        answer: 'Moderate risk indicates some imaging features may need attention, but it is not a diagnosis by itself. Your doctor will correlate this with clinical findings before decisions are made.'
      },
      {
        category: 'Interpretation',
        question: 'Why does the app show tissue volumes (CSF, GM, WM)?',
        answer: 'These values help quantify structural patterns in the brain. They support interpretation consistency across scans and help your doctor evaluate changes over time.'
      },
      {
        category: 'Security',
        question: 'Who can see my MRI reports?',
        answer: 'Only authorized users in your care flow (you and assigned clinical staff) can view your scan history and doctor review notes.'
      },
      {
        category: 'Security',
        question: 'Can I share my results outside the app?',
        answer: 'Yes, through generated reports when available, but always share with your physician first to avoid misinterpretation without medical context.'
      },
      {
        category: 'Security',
        question: 'Is my doctor note history stored per scan?',
        answer: 'Yes. Clinical notes are attached to specific MRI scans so each session keeps its own review context and recommendations.'
      },
      {
        category: 'Interpretation',
        question: 'Why can two scans have different scores?',
        answer: 'Risk can vary due to anatomy changes, imaging quality, slice alignment, and model confidence. Trend over time plus physician review is more important than a single value.'
      },
      {
        category: 'Workflow',
        question: 'What should I do after a review is available?',
        answer: 'Open your scan history, read doctor notes, and follow the recommended next step such as follow-up consultation or additional imaging if advised.'
      },
      {
        category: 'Workflow',
        question: 'How often should I upload follow-up MRI scans?',
        answer: 'Follow your physician recommendation. NeuroScan helps compare trends across sessions, but follow-up timing should match your clinical plan.'
      },
      {
        category: 'Workflow',
        question: 'What if my scan is still processing?',
        answer: 'Keep the page open or revisit later. The system updates scan status as analysis completes, and reviewed scans will appear in your history with physician feedback.'
      },
      {
        category: 'Workflow',
        question: 'How do I prepare a scan for upload?',
        answer: 'Use a supported MRI format from your imaging provider, ensure file integrity, and avoid renaming essential extensions before upload.'
      }
    ];
  }

  get filteredFaqItems(): FaqItem[] {
    const category = this.selectedFaqCategory();
    return this.faqItems.filter(item => item.category === category);
  }

  selectFaqCategory(category: FaqCategory): void {
    this.selectedFaqCategory.set(category);
    this.openFaqIndex.set(0);
  }

  toggleFaq(index: number): void {
    if (this.openFaqIndex() === index) {
      this.openFaqIndex.set(-1);
      return;
    }

    this.openFaqIndex.set(index);
  }

  getRiskTone(level: string): string {
    if (level === 'High') return 'text-rose-300 bg-rose-500/20 border-rose-500/40';
    if (level === 'Moderate') return 'text-amber-300 bg-amber-500/20 border-amber-500/40';
    if (level === 'Low') return 'text-emerald-300 bg-emerald-500/20 border-emerald-500/40';
    return 'text-slate-300 bg-slate-500/20 border-slate-500/40';
  }

  formatRelativeDate(dateValue: string): string {
    const then = new Date(dateValue).getTime();
    const diffMs = Date.now() - then;
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays <= 0) return 'Today';
    if (diffDays === 1) return '1 day ago';
    if (diffDays < 30) return `${diffDays} days ago`;

    const months = Math.floor(diffDays / 30);
    return months === 1 ? '1 month ago' : `${months} months ago`;
  }
}
