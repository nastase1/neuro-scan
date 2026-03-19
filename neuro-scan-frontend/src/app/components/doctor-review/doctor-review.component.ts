import {
  Component,
  OnInit,
  OnDestroy,
  signal,
  ViewChild,
  ElementRef,
  AfterViewInit,
} from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MriService } from '../../services/mri.service';
import { AuthService } from '../../services/auth.service';
import { MriScanDetail } from '../../models/api.models';

interface BrushColor {
  label: string;
  color: string;
  rgba: string;
}

@Component({
  selector: 'app-doctor-review',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './doctor-review.component.html',
  styleUrls: ['./doctor-review.component.css'],
})
export class DoctorReviewComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('paintCanvas') paintCanvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('paintContainer') paintContainerRef!: ElementRef<HTMLDivElement>;

  scanId: string = '';
  scanDetail = signal<MriScanDetail | null>(null);
  isLoading = signal(true);
  error = signal('');

  // Synchronized slice state (both panels use the same index)
  sliceCount = signal(0);
  currentSliceIndex = signal(0);
  
  // Raw MRI state
  rawSliceUrl = signal<string | null>(null);
  isLoadingRawSlice = signal(false);
  rawSliceCache = new Map<number, string>();

  // Segmentation state
  segSliceUrl = signal<string | null>(null);
  isLoadingSegSlice = signal(false);
  segSliceCache = new Map<number, string>();
  correctedSliceCache = new Set<number>();

  // Paint tool state
  isPainting = signal(false);
  brushSize = signal(1);
  pixelPerfect = signal(true);
  zoom = signal(1);
  panX = signal(0);
  panY = signal(0);
  readonly minZoom = 1;
  readonly maxZoom = 8;
  selectedColor: BrushColor = {
    label: 'Gray Matter',
    color: '#f472b6',
    rgba: 'rgba(244, 114, 182, 0.7)',
  };

  brushColors: BrushColor[] = [
    { label: 'Erase', color: '#000000', rgba: 'rgba(0,0,0,0)' },
    { label: 'CSF', color: '#60a5fa', rgba: 'rgba(96, 165, 250, 0.7)' },
    { label: 'Gray Matter', color: '#f472b6', rgba: 'rgba(244, 114, 182, 0.7)' },
    { label: 'White Matter', color: '#34d399', rgba: 'rgba(52, 211, 153, 0.7)' },
    { label: 'Tumor/Lesion', color: '#f97316', rgba: 'rgba(249, 115, 22, 0.78)' },
  ];

  sliceDirty = signal(false);
  isSavingSlice = signal(false);
  sliceSaveSuccess = signal(false);

  // Review form
  reviewApproved = signal<boolean | null>(null);
  reviewNotes = signal('');
  isSubmitting = signal(false);
  submitSuccess = signal(false);
  submitError = signal('');

  private ctx: CanvasRenderingContext2D | null = null;
  private isMouseDown = false;
  private isRightPanning = false;
  private panStartX = 0;
  private panStartY = 0;
  private panOriginX = 0;
  private panOriginY = 0;
  private rawObjectUrls: string[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private location: Location,
    private mriService: MriService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    window.scrollTo({ top: 0, behavior: 'auto' });

    this.scanId = this.route.snapshot.paramMap.get('scanId') ?? '';
    if (!this.scanId) {
      this.router.navigate(['/dashboard']);
      return;
    }
    this.loadScanData();
  }

  ngAfterViewInit(): void {
    if (this.paintCanvasRef) {
      this.ctx = this.paintCanvasRef.nativeElement.getContext('2d');
      if (this.ctx) {
        this.ctx.imageSmoothingEnabled = false;
      }
    }
  }

  ngOnDestroy(): void {
    this.rawObjectUrls.forEach((u) => URL.revokeObjectURL(u));
  }

  private loadScanData(): void {
    this.isLoading.set(true);
    this.mriService.getScanDetails(this.scanId).subscribe({
      next: (detail) => {
        this.scanDetail.set(detail);
        const segCount = detail.analysisResult?.segmentationSliceCount ?? 0;
        this.sliceCount.set(segCount);
        
        // Load first slice for both views
        if (segCount > 0) {
          this.loadSlice(0);
        }

        // Pre-fill review notes and approval if already reviewed
        const existingNotes = detail.doctorClinicalNotes ?? detail.analysisResult?.doctorReviewNotes;
        if (existingNotes) {
          this.reviewNotes.set(existingNotes);
        }
        if (detail.analysisResult?.doctorApproved !== undefined) {
          this.reviewApproved.set(detail.analysisResult.doctorApproved ?? null);
        }

        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load scan details.');
        this.isLoading.set(false);
      },
    });
  }

  // Load both raw and segmentation slices at the same index (synchronized)
  loadSlice(index: number): void {
    if (index < 0 || index >= this.sliceCount()) return;
    this.currentSliceIndex.set(index);
    this.sliceDirty.set(false);
    this.sliceSaveSuccess.set(false);
    this.panX.set(0);
    this.panY.set(0);

    // Load raw MRI slice
    if (this.rawSliceCache.has(index)) {
      this.rawSliceUrl.set(this.rawSliceCache.get(index)!);
    } else {
      this.isLoadingRawSlice.set(true);
      this.mriService.getRawSlice(this.scanId, index).subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          this.rawObjectUrls.push(url);
          this.rawSliceCache.set(index, url);
          this.rawSliceUrl.set(url);
          this.isLoadingRawSlice.set(false);
        },
        error: () => this.isLoadingRawSlice.set(false),
      });
    }

    // Load segmentation slice
    if (this.segSliceCache.has(index)) {
      this.segSliceUrl.set(this.segSliceCache.get(index)!);
      this.clearPaintCanvas();
      this.tryLoadCorrectedOverlay(index);
    } else {
      this.isLoadingSegSlice.set(true);
      this.mriService.getSegmentationSlice(this.scanId, index).subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          this.rawObjectUrls.push(url);
          this.segSliceCache.set(index, url);
          this.segSliceUrl.set(url);
          this.isLoadingSegSlice.set(false);
          this.clearPaintCanvas();
          this.tryLoadCorrectedOverlay(index);
        },
        error: () => this.isLoadingSegSlice.set(false),
      });
    }
  }

  private tryLoadCorrectedOverlay(index: number): void {
    this.mriService.getCorrectedSlice(this.scanId, index).subscribe({
      next: (blob) => {
        if (blob.size < 50) return; // empty / 404 body
        const url = URL.createObjectURL(blob);
        const img = new Image();
        img.onload = () => {
          const canvas = this.paintCanvasRef?.nativeElement;
          if (!canvas || !this.ctx) return;
          canvas.width = img.width || 256;
          canvas.height = img.height || 256;
          this.ctx.imageSmoothingEnabled = false;
          this.ctx.clearRect(0, 0, canvas.width, canvas.height);
          this.ctx.drawImage(img, 0, 0);
          URL.revokeObjectURL(url);
        };
        img.src = url;
      },
      error: () => {}, // no saved correction yet — fine
    });
  }

  // ---- Paint canvas interactions ----

  initCanvas(event: Event): void {
    const img = event.target as HTMLImageElement;
    const canvas = this.paintCanvasRef?.nativeElement;
    const container = this.paintContainerRef?.nativeElement;
    if (!canvas) return;
    canvas.width = img.naturalWidth || img.width || 256;
    canvas.height = img.naturalHeight || img.height || 256;
    this.ctx = canvas.getContext('2d');
    if (this.ctx) {
      this.ctx.imageSmoothingEnabled = false;
    }

    // Align the paint canvas to the exact rendered image area so all pixels are paintable.
    if (container) {
      const offsetX = Math.max((container.clientWidth - img.clientWidth) / 2, 0);
      const offsetY = Math.max((container.clientHeight - img.clientHeight) / 2, 0);
      canvas.style.width = `${img.clientWidth}px`;
      canvas.style.height = `${img.clientHeight}px`;
      canvas.style.left = `${offsetX}px`;
      canvas.style.top = `${offsetY}px`;
    }
  }

  onMouseDown(event: MouseEvent): void {
    if (event.button !== 0 || this.isRightPanning) return;
    this.isMouseDown = true;
    this.paint(event);
  }

  onMouseMove(event: MouseEvent): void {
    if (!this.isMouseDown) return;
    this.paint(event);
  }

  onMouseUp(): void {
    this.isMouseDown = false;
  }

  onMouseLeave(): void {
    this.isMouseDown = false;
  }

  onPanStart(event: MouseEvent): void {
    if (event.button !== 2) return;
    event.preventDefault();
    this.isRightPanning = true;
    this.panStartX = event.clientX;
    this.panStartY = event.clientY;
    this.panOriginX = this.panX();
    this.panOriginY = this.panY();
  }

  onPanMove(event: MouseEvent): void {
    if (!this.isRightPanning) return;
    event.preventDefault();
    const dx = event.clientX - this.panStartX;
    const dy = event.clientY - this.panStartY;
    this.panX.set(this.panOriginX + dx);
    this.panY.set(this.panOriginY + dy);
  }

  onPanEnd(): void {
    this.isRightPanning = false;
  }

  onSegmentationContextMenu(event: MouseEvent): void {
    event.preventDefault();
  }

  private paint(event: MouseEvent): void {
    const canvas = this.paintCanvasRef?.nativeElement;
    if (!canvas || !this.ctx) return;
    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    const x = (event.clientX - rect.left) * scaleX;
    const y = (event.clientY - rect.top) * scaleY;

    const size = this.brushSize();

    if (this.selectedColor.label === 'Erase') {
      const startX = this.pixelPerfect() ? Math.round(x) - Math.floor(size / 2) : x - size / 2;
      const startY = this.pixelPerfect() ? Math.round(y) - Math.floor(size / 2) : y - size / 2;
      this.ctx.clearRect(startX, startY, size, size);
    } else {
      this.ctx.fillStyle = this.selectedColor.rgba;
      if (this.pixelPerfect()) {
        const px = Math.round(x) - Math.floor(size / 2);
        const py = Math.round(y) - Math.floor(size / 2);
        this.ctx.fillRect(px, py, size, size);
      } else {
        this.ctx.beginPath();
        this.ctx.arc(x, y, size / 2, 0, Math.PI * 2);
        this.ctx.fill();
      }
    }
    this.sliceDirty.set(true);
    this.isPainting.set(true);
  }

  toggleFullscreen(): void {
    // Deprecated: replaced by wheel-based zoom.
  }

  onSegmentationWheel(event: WheelEvent): void {
    event.preventDefault();
    const delta = event.deltaY < 0 ? 0.15 : -0.15;
    const next = Math.min(this.maxZoom, Math.max(this.minZoom, this.zoom() + delta));
    this.zoom.set(Number(next.toFixed(2)));
  }

  get isPanning(): boolean {
    return this.isRightPanning;
  }

  clearPaintCanvas(): void {
    const canvas = this.paintCanvasRef?.nativeElement;
    if (!canvas || !this.ctx) return;
    this.ctx.clearRect(0, 0, canvas.width, canvas.height);
    this.sliceDirty.set(false);
  }

  saveCurrentSlice(): void {
    const canvas = this.paintCanvasRef?.nativeElement;
    if (!canvas) return;
    const base64 = canvas.toDataURL('image/png').split(',')[1];
    this.isSavingSlice.set(true);
    this.mriService
      .saveCorrectedSlice(this.scanId, this.currentSliceIndex(), base64)
      .subscribe({
        next: () => {
          this.isSavingSlice.set(false);
          this.sliceDirty.set(false);
          this.sliceSaveSuccess.set(true);
          this.correctedSliceCache.add(this.currentSliceIndex());
          setTimeout(() => this.sliceSaveSuccess.set(false), 2000);
        },
        error: () => this.isSavingSlice.set(false),
      });
  }

  // ---- Review submission ----

  setApproval(value: boolean): void {
    this.reviewApproved.set(value);
  }

  submitReview(): void {
    if (this.reviewApproved() === null) {
      this.submitError.set('Please select Approve or Reject before submitting.');
      return;
    }
    this.isSubmitting.set(true);
    this.submitError.set('');
    this.mriService
      .submitReview(this.scanId, this.reviewApproved()!, this.reviewNotes())
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.submitSuccess.set(true);
        },
        error: () => {
          this.isSubmitting.set(false);
          this.submitError.set('Failed to submit review. Please try again.');
        },
      });
  }

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    this.router.navigate(['/dashboard']);
  }

  get riskLevelClass(): string {
    const level = this.scanDetail()?.analysisResult?.epilepsyRiskLevel ?? 'Low';
    if (level === 'High') return 'text-red-400 bg-red-500/20';
    if (level === 'Moderate') return 'text-yellow-400 bg-yellow-500/20';
    return 'text-emerald-400 bg-emerald-500/20';
  }
}
