import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { PatientListComponent } from './components/patient-list/patient-list.component';
import { PatientFormComponent } from './components/patient-form/patient-form.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { 
    path: 'dashboard', 
    component: DashboardComponent,
    canActivate: [authGuard]
  },
  { 
    path: 'patients', 
    component: PatientListComponent,
    canActivate: [authGuard]
  },
  { 
    path: 'patients/new', 
    component: PatientFormComponent,
    canActivate: [authGuard]
  },
  { 
    path: 'patients/:id/edit', 
    component: PatientFormComponent,
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: '/login' }
];
