import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';

type PageType = 'terms' | 'privacy' | 'contact' | 'support';

type LegalSection = {
  heading: string;
  body: string;
  bullets?: string[];
};

@Component({
  selector: 'app-legal-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './legal-page.component.html',
  styleUrls: ['./legal-page.component.css']
})
export class LegalPageComponent {
  private route = inject(ActivatedRoute);
  readonly lastUpdated = 'March 19, 2026';

  pageType = computed(() => (this.route.snapshot.data['pageType'] as PageType) ?? 'terms');

  title = computed(() => {
    if (this.pageType() === 'privacy') return 'Privacy Policy';
    if (this.pageType() === 'contact') return 'Contact';
    if (this.pageType() === 'support') return 'Support Center';
    return 'Terms & Conditions';
  });

  subtitle = computed(() => {
    if (this.pageType() === 'privacy') {
      return 'How NeuroScan collects, uses, stores, and protects personal and medical-adjacent data.';
    }
    if (this.pageType() === 'contact') {
      return 'Official communication channels for clinical, technical, and business requests.';
    }
    if (this.pageType() === 'support') {
      return 'Get quick help for account access, scans, reports, billing, and technical issues.';
    }

    return 'Rules for using the NeuroScan platform, including account use, acceptable behavior, and service boundaries.';
  });

  quickActions = computed(() => {
    if (this.pageType() === 'support') {
      return [
        { label: 'Email Support', value: 'support@neuroscan.app' },
        { label: 'Technical Priority', value: 'tech@neuroscan.app' },
        { label: 'Research Contact', value: 'research@neuroscan.app' }
      ];
    }

    if (this.pageType() === 'contact') {
      return [
        { label: 'General', value: 'support@neuroscan.app' },
        { label: 'Partnerships', value: 'partnerships@neuroscan.app' },
        { label: 'Press', value: 'press@neuroscan.app' }
      ];
    }

    return [];
  });

  sections = computed<LegalSection[]>(() => {
    if (this.pageType() === 'privacy') {
      return [
        {
          heading: '1. Data We Collect',
          body: 'We collect account details, platform activity logs, scan metadata, and doctor review notes to deliver core product features and maintain safety controls.',
          bullets: [
            'Identity and account data: full name, email, role, authentication data.',
            'Operational data: login history, session details, audit events, and actions performed.',
            'Clinical workflow data: patient linkage metadata, upload timestamps, status updates, and doctor annotations.',
            'Support data: communications sent to our support channels.'
          ]
        },
        {
          heading: '2. Why We Process Data',
          body: 'Processing is limited to delivering platform services, securing the environment, improving quality, and complying with legal obligations.',
          bullets: [
            'Authenticate users and enforce role-based access.',
            'Generate and display scan analysis outputs.',
            'Maintain system reliability, fraud prevention, and abuse detection.',
            'Respond to support requests and service notifications.'
          ]
        },
        {
          heading: '3. Legal Basis',
          body: 'We rely on contract performance, legitimate interests, legal obligations, and consent where applicable.'
        },
        {
          heading: '4. Sharing and Subprocessors',
          body: 'We share only the minimum required data with vetted subprocessors (for infrastructure, notifications, and analytics) under contractual safeguards.',
          bullets: [
            'No sale of personal data.',
            'No disclosure for unrelated advertising purposes.',
            'Controlled access on a need-to-know basis.'
          ]
        },
        {
          heading: '5. Retention',
          body: 'Data is retained based on service, compliance, and safety requirements, then deleted or anonymized when no longer needed.'
        },
        {
          heading: '6. Security Measures',
          body: 'We apply technical and organizational controls to protect confidentiality, integrity, and availability.',
          bullets: [
            'Role-based authorization checks across protected endpoints.',
            'Token-based authentication and secure password handling.',
            'Monitoring, logging, and controlled incident response procedures.'
          ]
        },
        {
          heading: '7. Your Rights',
          body: 'Subject to local law, you may request access, correction, deletion, restriction, objection, and portability of personal data.'
        },
        {
          heading: '8. Cookies and Diagnostics',
          body: 'We may use essential cookies and minimal diagnostics to keep the service available and performant.'
        },
        {
          heading: '9. Updates to This Policy',
          body: 'When this policy changes materially, we will publish the latest version in-app with an updated effective date.'
        }
      ];
    }

    if (this.pageType() === 'contact') {
      return [
        {
          heading: '1. Customer Support',
          body: 'For account access, dashboard usage, report visibility, and scan workflow questions.'
        },
        {
          heading: '2. Clinical Workflow Assistance',
          body: 'For issues related to review queues, clinical notes, or doctor validation flow, contact the medical operations channel.'
        },
        {
          heading: '3. Business and Partnerships',
          body: 'For institutional onboarding, pilot programs, and integration requests.'
        },
        {
          heading: '4. Company Details',
          body: 'NeuroScan Platform Team, Bucharest, Romania. Registration and legal details are provided in onboarding contracts and partner documentation.'
        },
        {
          heading: '5. Response Windows',
          body: 'We answer most contact requests in 1-2 business days. Priority incidents are processed faster through support triage.'
        },
        {
          heading: '6. Contact Channels',
          body: 'Use the addresses below for the fastest routing.',
          bullets: [
            'General: support@neuroscan.app',
            'Technical: tech@neuroscan.app',
            'Research: research@neuroscan.app',
            'Partnerships: partnerships@neuroscan.app'
          ]
        }
      ];
    }

    if (this.pageType() === 'support') {
      return [
        {
          heading: '1. Common Help Topics',
          body: 'Most users need support for login issues, scan upload errors, delayed processing, report visibility, and role permissions.',
          bullets: [
            'Cannot log in or forgot password.',
            'Upload failed for MRI files.',
            'Scan stuck in processing state.',
            'Doctor review does not appear in history.',
            'Access denied for a feature that should be available.'
          ]
        },
        {
          heading: '2. Before You Contact Support',
          body: 'Please include details that help us reproduce and fix the issue quickly.',
          bullets: [
            'Account email and user role.',
            'Scan ID or patient identifier (when applicable).',
            'Approximate time when issue occurred.',
            'Error message text or screenshot.',
            'Browser and device details.'
          ]
        },
        {
          heading: '3. Priority and SLA',
          body: 'We triage requests by impact and urgency.',
          bullets: [
            'Critical service outage: response target under 4 business hours.',
            'Major feature degradation: response target within 1 business day.',
            'General support request: response target within 1-2 business days.'
          ]
        },
        {
          heading: '4. Product Return and Service Note',
          body: 'NeuroScan is software. Hardware returns, imaging hardware warranty, or external service logistics are handled by the hardware vendor or partner organization.'
        },
        {
          heading: '5. Escalation Path',
          body: 'If your issue remains unresolved, request escalation in the same support thread and include previous ticket references.'
        },
        {
          heading: '6. Safety and Clinical Disclaimer',
          body: 'Support can help with platform operation but cannot provide clinical diagnosis. Medical decisions must be made by licensed professionals.'
        }
      ];
    }

    return [
      {
        heading: '1. Scope and Acceptance',
        body: 'These Terms govern use of the NeuroScan web platform and related services. By creating an account or using the platform, you accept these Terms and applicable laws.'
      },
      {
        heading: '2. Definitions',
        body: 'User means any authorized account holder. Organization means the clinic, institution, or partner using NeuroScan. Content means data entered, uploaded, generated, or displayed through the service.'
      },
      {
        heading: '3. Account and Access',
        body: 'Users are responsible for account confidentiality and lawful usage. Access is role-based and can be suspended for violations, security risks, or misuse.'
      },
      {
        heading: '4. Acceptable Use',
        body: 'You agree not to abuse the service, bypass security, upload harmful content, or use automation that disrupts normal operation.',
        bullets: [
          'No unauthorized access or data scraping.',
          'No falsified identity or intentionally misleading data.',
          'No content that is illegal, discriminatory, abusive, or unsafe.'
        ]
      },
      {
        heading: '5. Clinical Responsibility',
        body: 'NeuroScan provides decision support only. It does not replace medical judgment, diagnosis, or treatment by licensed clinicians.'
      },
      {
        heading: '6. Orders, Subscriptions, and Billing',
        body: 'Commercial terms for paid plans, invoicing, and renewals are defined in plan-specific agreements or order forms.'
      },
      {
        heading: '7. Data and Confidentiality',
        body: 'Each party must protect confidential information. Customer data remains under customer control subject to applicable law and service operation needs.'
      },
      {
        heading: '8. Intellectual Property',
        body: 'NeuroScan branding, software, and service design are protected. You receive a limited, non-transferable right to use the service under these Terms.'
      },
      {
        heading: '9. Service Changes',
        body: 'We may improve, modify, or retire features to maintain quality, security, and legal compliance. Material updates are communicated in-product.'
      },
      {
        heading: '10. Limitation of Liability',
        body: 'To the maximum extent allowed by law, NeuroScan is not liable for indirect or consequential damages, including loss from misuse or third-party interruptions.'
      },
      {
        heading: '11. Suspension and Termination',
        body: 'We may suspend or terminate access for serious violations, legal requirements, or security threats. Users may stop using the service at any time.'
      },
      {
        heading: '12. Complaints and Dispute Resolution',
        body: 'Support complaints should be sent through contact channels first. We aim for amicable resolution before escalation to competent courts.'
      },
      {
        heading: '13. Governing Law',
        body: 'These Terms are governed by applicable Romanian and EU legislation, unless a mandatory law provides otherwise.'
      },
      {
        heading: '14. Policy Updates',
        body: 'The latest Terms version is always published in-app. Continued use after updates means acceptance of revised terms.'
      }
    ];
  });
}
