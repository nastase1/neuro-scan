import { Component, OnInit, signal, computed, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MriService } from '../../services/mri.service';
import { AuthService } from '../../services/auth.service';
import { PatientService } from '../../services/patient.service';
import { PatientEvolution, EvolutionDataPoint, EvolutionSummary } from '../../models/api.models';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-patient-evolution',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './patient-evolution.component.html',
  styleUrls: ['./patient-evolution.component.css']
})
export class PatientEvolutionComponent implements OnInit, AfterViewInit, OnDestroy {
  evolution = signal<PatientEvolution | null>(null);
  isLoading = signal(true);
  error = signal('');
  patientId = '';
  activeTab = signal<'volumes' | 'tumor' | 'cortex' | 'risk' | 'report'>('volumes');

  patients = signal<any[]>([]);
  showPatientSelector = signal(false);
  isDoctorUser = false;
  hasRouteParam = false;

  // Patient selector: search, sort, pagination
  patientSearch = signal('');
  patientSortField = signal<'name' | 'dob' | 'id'>('name');
  patientSortAsc = signal(true);
  patientPage = signal(1);
  patientsPerPage = 9;

  filteredPatients = computed(() => {
    let list = this.patients();
    const q = this.patientSearch().toLowerCase().trim();
    if (q) {
      list = list.filter(p =>
        `${p.firstName} ${p.lastName}`.toLowerCase().includes(q) ||
        p.medicalRecordNumber?.toLowerCase().includes(q) ||
        p.id?.toLowerCase().includes(q)
      );
    }
    const field = this.patientSortField();
    const asc = this.patientSortAsc();
    list = [...list].sort((a, b) => {
      let cmp = 0;
      if (field === 'name') cmp = `${a.firstName} ${a.lastName}`.localeCompare(`${b.firstName} ${b.lastName}`);
      else if (field === 'dob') cmp = new Date(a.dateOfBirth).getTime() - new Date(b.dateOfBirth).getTime();
      else cmp = (a.id || '').localeCompare(b.id || '');
      return asc ? cmp : -cmp;
    });
    return list;
  });

  totalPatientPages = computed(() => Math.max(1, Math.ceil(this.filteredPatients().length / this.patientsPerPage)));

  paginatedPatients = computed(() => {
    const start = (this.patientPage() - 1) * this.patientsPerPage;
    return this.filteredPatients().slice(start, start + this.patientsPerPage);
  });

  onPatientSearchChange(value: string) {
    this.patientSearch.set(value);
    this.patientPage.set(1);
  }

  togglePatientSort(field: 'name' | 'dob' | 'id') {
    if (this.patientSortField() === field) {
      this.patientSortAsc.set(!this.patientSortAsc());
    } else {
      this.patientSortField.set(field);
      this.patientSortAsc.set(true);
    }
    this.patientPage.set(1);
  }

  goToPatientPage(page: number) {
    if (page >= 1 && page <= this.totalPatientPages()) {
      this.patientPage.set(page);
    }
  }

  private charts: Chart[] = [];

  @ViewChild('volumeChart') volumeChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('tumorChart') tumorChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('cortexChart') cortexChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('riskChart') riskChartRef!: ElementRef<HTMLCanvasElement>;

  summary = computed(() => this.evolution()?.summary ?? null);
  dataPoints = computed(() => this.evolution()?.dataPoints ?? []);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private mriService: MriService,
    private authService: AuthService,
    private patientService: PatientService
  ) {}

  ngOnInit() {
    this.isDoctorUser = this.authService.isDoctor();
    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam) {
      this.hasRouteParam = true;
      this.patientId = idParam;
      this.loadEvolution();
    } else if (this.isDoctorUser) {
      this.isLoading.set(false);
      this.showPatientSelector.set(true);
      this.patientService.getAllPatients().subscribe({
        next: (patients) => this.patients.set(patients),
        error: () => this.error.set('Failed to load patients')
      });
    } else {
      this.mriService.getMyPatient().subscribe({
        next: (patient: any) => {
          if (patient && patient.id) {
            this.patientId = patient.id.toString();
            this.loadEvolution();
          } else {
            this.isLoading.set(false);
            this.error.set('No patient profile linked to your account.');
          }
        },
        error: () => {
          this.isLoading.set(false);
          this.error.set('Could not resolve your patient profile.');
        }
      });
    }
  }

  selectPatient(patientId: number) {
    this.patientId = patientId.toString();
    this.showPatientSelector.set(false);
    this.loadEvolution();
  }

  ngAfterViewInit() {}

  ngOnDestroy() {
    this.charts.forEach(c => c.destroy());
  }

  loadEvolution() {
    this.isLoading.set(true);
    this.mriService.getPatientEvolution(this.patientId).subscribe({
      next: (data) => {
        this.evolution.set(data);
        this.isLoading.set(false);
        setTimeout(() => this.renderCharts(), 100);
      },
      error: (err) => {
        this.error.set('Failed to load evolution data');
        this.isLoading.set(false);
      }
    });
  }

  setTab(tab: 'volumes' | 'tumor' | 'cortex' | 'risk' | 'report') {
    this.activeTab.set(tab);
    setTimeout(() => this.renderCharts(), 100);
  }

  private renderCharts() {
    this.charts.forEach(c => c.destroy());
    this.charts = [];

    const points = this.dataPoints();
    if (points.length < 2) return;

    const labels = points.map(p => new Date(p.scanDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }));

    const tab = this.activeTab();
    if (tab === 'volumes' && this.volumeChartRef) {
      this.charts.push(this.createChart(this.volumeChartRef.nativeElement, {
        type: 'line',
        data: {
          labels,
          datasets: [
            { label: 'Total Brain Volume (cm³)', data: points.map(p => p.totalBrainVolume), borderColor: '#22d3ee', backgroundColor: 'rgba(34,211,238,0.1)', tension: 0.3, fill: true },
            { label: 'Gray Matter (cm³)', data: points.map(p => p.gmVolume), borderColor: '#a78bfa', backgroundColor: 'rgba(167,139,250,0.1)', tension: 0.3 },
            { label: 'White Matter (cm³)', data: points.map(p => p.wmVolume), borderColor: '#34d399', backgroundColor: 'rgba(52,211,153,0.1)', tension: 0.3 },
            { label: 'CSF (cm³)', data: points.map(p => p.csfVolume), borderColor: '#fbbf24', backgroundColor: 'rgba(251,191,36,0.1)', tension: 0.3 }
          ]
        },
        options: this.getChartOptions('Brain Tissue Volumes Over Time')
      }));
    }

    if (tab === 'tumor' && this.tumorChartRef) {
      this.charts.push(this.createChart(this.tumorChartRef.nativeElement, {
        type: 'line',
        data: {
          labels,
          datasets: [
            { label: 'Tumor Volume (cm³)', data: points.map(p => p.tumorVolume), borderColor: '#f87171', backgroundColor: 'rgba(248,113,113,0.15)', tension: 0.3, fill: true },
            { label: 'Tumor Surface Area (cm²)', data: points.map(p => p.tumorSurfaceArea), borderColor: '#fb923c', backgroundColor: 'rgba(251,146,60,0.1)', tension: 0.3, yAxisID: 'y1' }
          ]
        },
        options: {
          ...this.getChartOptions('Tumor Metrics Over Time'),
          scales: {
            ...this.getChartOptions('').scales,
            y1: { type: 'linear', position: 'right', grid: { drawOnChartArea: false }, ticks: { color: '#9ca3af' } }
          }
        }
      }));
    }

    if (tab === 'cortex' && this.cortexChartRef) {
      this.charts.push(this.createChart(this.cortexChartRef.nativeElement, {
        type: 'line',
        data: {
          labels,
          datasets: [
            { label: 'Cortex Thickness (avg)', data: points.map(p => p.cortexThicknessAvg), borderColor: '#e879f9', backgroundColor: 'rgba(232,121,249,0.15)', tension: 0.3, fill: true },
            { label: 'WM Density Score', data: points.map(p => p.wmDensityScore), borderColor: '#38bdf8', backgroundColor: 'rgba(56,189,248,0.1)', tension: 0.3, yAxisID: 'y1' }
          ]
        },
        options: {
          ...this.getChartOptions('Cortex & White Matter Over Time'),
          scales: {
            ...this.getChartOptions('').scales,
            y1: { type: 'linear', position: 'right', grid: { drawOnChartArea: false }, ticks: { color: '#9ca3af' } }
          }
        }
      }));
    }

    if (tab === 'risk' && this.riskChartRef) {
      this.charts.push(this.createChart(this.riskChartRef.nativeElement, {
        type: 'line',
        data: {
          labels,
          datasets: [
            { label: 'Epilepsy Risk Score', data: points.map(p => p.epilepsyRiskScore), borderColor: '#f87171', backgroundColor: 'rgba(248,113,113,0.15)', tension: 0.3, fill: true },
            { label: 'Asymmetry Index (%)', data: points.map(p => p.asymmetryIndex), borderColor: '#facc15', backgroundColor: 'rgba(250,204,21,0.1)', tension: 0.3, yAxisID: 'y1' }
          ]
        },
        options: {
          ...this.getChartOptions('Risk Assessment Over Time'),
          scales: {
            ...this.getChartOptions('').scales,
            y1: { type: 'linear', position: 'right', grid: { drawOnChartArea: false }, ticks: { color: '#9ca3af' } }
          }
        }
      }));
    }
  }

  private createChart(canvas: HTMLCanvasElement, config: ChartConfiguration): Chart {
    return new Chart(canvas, config);
  }

  private getChartOptions(title: string): any {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        title: { display: !!title, text: title, color: '#e2e8f0', font: { size: 16 } },
        legend: { labels: { color: '#9ca3af', usePointStyle: true } },
        tooltip: { mode: 'index', intersect: false }
      },
      scales: {
        x: { ticks: { color: '#9ca3af' }, grid: { color: 'rgba(75,85,99,0.3)' } },
        y: { ticks: { color: '#9ca3af' }, grid: { color: 'rgba(75,85,99,0.3)' } }
      },
      interaction: { mode: 'nearest', axis: 'x', intersect: false }
    };
  }

  getDegradationColor(level: string): string {
    switch (level) {
      case 'Severe': return 'text-red-400';
      case 'Moderate': return 'text-yellow-400';
      case 'Mild': return 'text-amber-400';
      default: return 'text-green-400';
    }
  }

  getDeltaColor(value: number): string {
    if (value > 0.01) return 'text-green-400';
    if (value < -0.01) return 'text-red-400';
    return 'text-gray-400';
  }

  formatDelta(value: number): string {
    if (!value) return '0';
    return (value > 0 ? '+' : '') + value.toFixed(3);
  }

  goBack() {
    if (this.hasRouteParam) {
      this.router.navigate(['/patients', this.patientId]);
    } else if (this.isDoctorUser && this.evolution()) {
      this.evolution.set(null);
      this.patientId = '';
      this.showPatientSelector.set(true);
      this.error.set('');
    } else {
      this.router.navigate(['/home']);
    }
  }
}
