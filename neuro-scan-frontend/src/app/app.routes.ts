import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { PatientListComponent } from './components/patient-list/patient-list.component';
import { PatientFormComponent } from './components/patient-form/patient-form.component';
import { PatientDetailComponent } from './components/patient-detail/patient-detail.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { ScanHistoryComponent } from './components/scan-history/scan-history.component';
import { DoctorReviewComponent } from './components/doctor-review/doctor-review.component';
import { AdminComponent } from './components/admin/admin.component';
import { HomeComponent } from './components/home/home.component';
import { LegalPageComponent } from './components/legal-page/legal-page.component';
import { authGuard } from './guards/auth.guard';
import { doctorGuard, adminGuard } from './guards/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  {
    path: 'scan-history',
    component: ScanHistoryComponent,
    canActivate: [authGuard]
  },
  {
    path: 'home',
    component: HomeComponent,
    canActivate: [authGuard]
  },
  { 
    path: 'dashboard', 
    component: DashboardComponent,
    canActivate: [authGuard]
  },
  { 
    path: 'patients', 
    component: PatientListComponent,
    canActivate: [authGuard, doctorGuard]
  },
  { 
    path: 'patients/new', 
    component: PatientFormComponent,
    canActivate: [authGuard, doctorGuard]
  },
  { 
    path: 'patients/:id', 
    component: PatientDetailComponent,
    canActivate: [authGuard, doctorGuard]
  },
  { 
    path: 'patients/:id/edit', 
    component: PatientFormComponent,
    canActivate: [authGuard, doctorGuard]
  },
  {
    path: 'review/:scanId',
    component: DoctorReviewComponent,
    canActivate: [authGuard, doctorGuard]
  },
  {
    path: 'admin',
    component: AdminComponent,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'terms',
    component: LegalPageComponent,
    canActivate: [authGuard],
    data: { pageType: 'terms' }
  },
  {
    path: 'privacy',
    component: LegalPageComponent,
    canActivate: [authGuard],
    data: { pageType: 'privacy' }
  },
  {
    path: 'contact',
    component: LegalPageComponent,
    canActivate: [authGuard],
    data: { pageType: 'contact' }
  },
  { path: '**', redirectTo: '/login' }
];
