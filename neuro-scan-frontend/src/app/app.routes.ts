import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { PatientListComponent } from './components/patient-list/patient-list.component';
import { PatientFormComponent } from './components/patient-form/patient-form.component';
import { PatientDetailComponent } from './components/patient-detail/patient-detail.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { ScanHistoryComponent } from './components/scan-history/scan-history.component';
import { authGuard } from './guards/auth.guard';
import { doctorGuard } from './guards/role.guard';

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
  { path: '**', redirectTo: '/login' }
];
