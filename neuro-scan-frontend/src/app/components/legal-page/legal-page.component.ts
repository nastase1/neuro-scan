import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';

@Component({
  selector: 'app-legal-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './legal-page.component.html',
  styleUrls: ['./legal-page.component.css']
})
export class LegalPageComponent {
  private route = inject(ActivatedRoute);

  pageType = computed(() => (this.route.snapshot.data['pageType'] as 'terms' | 'privacy' | 'contact') ?? 'terms');

  title = computed(() => {
    if (this.pageType() === 'privacy') return 'Privacy Policy';
    if (this.pageType() === 'contact') return 'Contact';
    return 'Terms & Conditions';
  });

  subtitle = computed(() => {
    if (this.pageType() === 'privacy') return 'How NeuroScan handles and protects data.';
    if (this.pageType() === 'contact') return 'Ways to reach the NeuroScan team.';
    return 'Usage terms for the NeuroScan platform.';
  });

  sections = computed(() => {
    if (this.pageType() === 'privacy') {
      return [
        {
          heading: 'Data Scope',
          body: 'NeuroScan stores user account details, scan metadata, and clinical review notes to provide platform functionality and auditability.'
        },
        {
          heading: 'Access Control',
          body: 'Data visibility is role-based. Doctors, patients, and admins only access data within authorized permissions.'
        },
        {
          heading: 'Security',
          body: 'Authentication is token-based and sensitive records are protected by backend authorization checks.'
        }
      ];
    }

    if (this.pageType() === 'contact') {
      return [
        {
          heading: 'General Support',
          body: 'Email: support@neuroscan.app'
        },
        {
          heading: 'Clinical Questions',
          body: 'For medical interpretation, contact your assigned physician via your clinic workflow.'
        },
        {
          heading: 'Thesis / Demo Inquiries',
          body: 'Email: research@neuroscan.app'
        }
      ];
    }

    return [
      {
        heading: 'Medical Use Disclaimer',
        body: 'NeuroScan is decision-support software. Final diagnosis and treatment decisions must be made by licensed clinicians.'
      },
      {
        heading: 'User Responsibilities',
        body: 'Users must upload valid MRI files and maintain account confidentiality. Unauthorized sharing is prohibited.'
      },
      {
        heading: 'Platform Updates',
        body: 'Features and models may evolve to improve quality and safety. Version history is available in the application footer.'
      }
    ];
  });
}
