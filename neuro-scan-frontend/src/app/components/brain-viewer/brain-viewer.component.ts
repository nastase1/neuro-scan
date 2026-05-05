import { Component, OnInit, OnDestroy, signal, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MriService } from '../../services/mri.service';
import { MeshDataResponse, MeshRegion } from '../../models/api.models';

declare const Plotly: any;

@Component({
  selector: 'app-brain-viewer',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './brain-viewer.component.html',
  styleUrls: ['./brain-viewer.component.css']
})
export class BrainViewerComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('plotlyContainer') plotlyContainer!: ElementRef<HTMLDivElement>;

  isLoading = signal(true);
  error = signal('');
  scanId = '';
  meshData = signal<MeshDataResponse | null>(null);

  visibleRegions = signal<{ [key: string]: boolean }>({
    brain: true,
    gray_matter: true,
    white_matter: true,
    tumor: true
  });

  regionLabels: { [key: string]: string } = {
    brain: 'Brain Mesh (Wireframe)',
    gray_matter: 'Gray Matter',
    white_matter: 'White Matter',
    tumor: 'Tumor'
  };

  regionColors: { [key: string]: string } = {
    brain: '#22d3ee',
    gray_matter: '#a78bfa',
    white_matter: '#34d399',
    tumor: '#f87171'
  };

  private plotlyLoaded = false;
  private resizeHandler: (() => void) | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private mriService: MriService
  ) {}

  ngOnInit() {
    this.scanId = this.route.snapshot.paramMap.get('scanId') ?? '';
    this.loadPlotlyScript();
    
    // Add window resize listener for responsive plot
    this.resizeHandler = () => {
      if (this.plotlyContainer?.nativeElement && this.plotlyLoaded) {
        try {
          Plotly.Plots.resize(this.plotlyContainer.nativeElement);
        } catch (e) {
          console.error('Error resizing plot:', e);
        }
      }
    };
    window.addEventListener('resize', this.resizeHandler);
  }

  ngAfterViewInit() {}

  ngOnDestroy() {
    if (this.resizeHandler) {
      window.removeEventListener('resize', this.resizeHandler);
    }
    if (this.plotlyContainer?.nativeElement && this.plotlyLoaded) {
      try { Plotly.purge(this.plotlyContainer.nativeElement); } catch {}
    }
  }

  private loadPlotlyScript() {
    if (typeof Plotly !== 'undefined') {
      this.plotlyLoaded = true;
      this.loadMeshData();
      return;
    }

    const script = document.createElement('script');
    script.src = 'https://cdn.plot.ly/plotly-2.27.0.min.js';
    script.onload = () => {
      this.plotlyLoaded = true;
      this.loadMeshData();
    };
    script.onerror = () => {
      this.error.set('Failed to load 3D visualization library');
      this.isLoading.set(false);
    };
    document.head.appendChild(script);
  }

  loadMeshData() {
    if (!this.scanId) {
      this.error.set('No scan ID provided');
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.mriService.get3DMesh(this.scanId).subscribe({
      next: (data) => {
        this.meshData.set(data);
        this.isLoading.set(false);
        setTimeout(() => this.renderPlot(), 100);
      },
      error: (err) => {
        this.error.set('Failed to load 3D mesh data. The scan may still be processing.');
        this.isLoading.set(false);
      }
    });
  }

  toggleRegion(region: string) {
    const current = this.visibleRegions();
    this.visibleRegions.set({ ...current, [region]: !current[region] });
    this.renderPlot();
  }

  renderPlot() {
    if (!this.plotlyLoaded || !this.meshData() || !this.plotlyContainer) return;

    const data = this.meshData()!;
    const traces: any[] = [];
    const visible = this.visibleRegions();

    for (const [regionKey, region] of Object.entries(data.regions)) {
      if (!visible[regionKey] || !region.vertices || region.vertices.length === 0) continue;

      if (regionKey === 'brain') {
        // Render brain as wireframe (Scatter3d lines from triangle edges)
        const verts = region.vertices;
        const faces = region.faces;
        const edgesX: (number | null)[] = [];
        const edgesY: (number | null)[] = [];
        const edgesZ: (number | null)[] = [];

        // Sample every 3rd face to reduce density while keeping the mesh look
        for (let fi = 0; fi < faces.length; fi += 3) {
          const face = faces[fi];
          for (let ei = 0; ei < 3; ei++) {
            const v1 = verts[face[ei]];
            const v2 = verts[face[(ei + 1) % 3]];
            edgesX.push(v1[0], v2[0], null);
            edgesY.push(v1[1], v2[1], null);
            edgesZ.push(v1[2], v2[2], null);
          }
        }

        traces.push({
          type: 'scatter3d',
          x: edgesX,
          y: edgesY,
          z: edgesZ,
          mode: 'lines',
          line: { color: region.color, width: 1.5 },
          name: this.regionLabels[regionKey] ?? regionKey,
          opacity: 0.3,
          hoverinfo: 'skip'
        });
      } else {
        // Render other regions (gray_matter, white_matter, tumor) as solid meshes
        const x = region.vertices.map((v: number[]) => v[0]);
        const y = region.vertices.map((v: number[]) => v[1]);
        const z = region.vertices.map((v: number[]) => v[2]);
        const i = region.faces.map((f: number[]) => f[0]);
        const j = region.faces.map((f: number[]) => f[1]);
        const k = region.faces.map((f: number[]) => f[2]);

        traces.push({
          type: 'mesh3d',
          x, y, z, i, j, k,
          color: region.color,
          opacity: region.opacity,
          name: this.regionLabels[regionKey] ?? regionKey,
          flatshading: true,
          lighting: {
            ambient: 0.7,
            diffuse: 1.0,
            specular: 1.0,
            roughness: 0.1
          },
          lightposition: { x: 100, y: 200, z: 300 }
        });
      }
    }

    const axisConfig = {
      showbackground: false,
      showgrid: true,
      gridcolor: 'rgba(128, 128, 128, 0.3)',
      zerolinecolor: 'white',
      tickfont: { color: 'white' },
      titlefont: { color: '#22d3ee' }
    };

    const layout = {
      autosize: true,
      paper_bgcolor: 'rgba(10, 15, 25, 1)',
      plot_bgcolor: 'rgba(10, 15, 25, 1)',
      scene: {
        bgcolor: 'rgba(10, 15, 25, 1)',
        xaxis: { title: 'X (Sagittal)', ...axisConfig },
        yaxis: { title: 'Y (Coronal)', ...axisConfig },
        zaxis: { title: 'Z (Axial)', ...axisConfig },
        camera: {
          eye: { x: 1.5, y: 1.5, z: 1.0 },
          up: { x: 0, y: 0, z: 1 }
        },
        aspectmode: 'data'
      },
      margin: { l: 0, r: 0, t: 0, b: 0 },
      showlegend: true,
      legend: {
        font: { color: 'white', size: 12 },
        bgcolor: 'rgba(30,41,59,0.8)',
        bordercolor: 'rgba(71,85,105,0.5)',
        borderwidth: 1,
        orientation: 'v',
        x: 0,
        y: 1
      }
    };

    const config = {
      responsive: true,
      displayModeBar: true,
      modeBarButtonsToRemove: ['toImage', 'sendDataToCloud'],
      displaylogo: false,
      toImageButtonOptions: {
        format: 'png',
        filename: 'brain_3d_view'
      }
    };

    Plotly.newPlot(this.plotlyContainer.nativeElement, traces, layout, config);
  }

  goBack() {
    window.history.back();
  }

  getRegionKeys(): string[] {
    return Object.keys(this.meshData()?.regions ?? {});
  }
}
