import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { authGuard } from './guards/auth.guard';
import { setupGuard } from './guards/setup.guard';
import { changePasswordGuard } from './guards/change-password.guard';
import { requirePermission } from './guards/permission.guard';

import { LoginComponent } from './login/login.component';
import { OnboardingComponent } from './onboarding/onboarding.component';
import { ChangePasswordComponent } from './change-password/change-password.component';
import { ProfileComponent } from './profile/profile.component';

import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { PrintersComponent } from './pages/printers/printers.component';
import { MaterialsComponent } from './pages/materials/materials.component';
import { CurrenciesComponent } from './pages/currencies/currencies.component';
import { SettingsComponent } from './pages/settings/settings.component';
import { TransactionsComponent } from './pages/transactions/transactions.component';
import { UsersComponent } from './pages/users/users.component';
import { RolesComponent } from './pages/roles/roles.component';

const routes: Routes = [
  { path: 'onboarding',      component: OnboardingComponent,     canActivate: [setupGuard] },
  { path: 'login',           component: LoginComponent },
  { path: 'change-password', component: ChangePasswordComponent, canActivate: [changePasswordGuard] },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: '',             component: DashboardComponent },
      { path: 'profile',      component: ProfileComponent },
      { path: 'printers',     component: PrintersComponent,     canActivate: [requirePermission('printers.view')] },
      { path: 'materials',    component: MaterialsComponent,    canActivate: [requirePermission('materials.view')] },
      { path: 'currencies',   component: CurrenciesComponent,   canActivate: [requirePermission('currencies.view')] },
      { path: 'settings',     component: SettingsComponent,     canActivate: [requirePermission('settings.view')] },
      { path: 'transactions', component: TransactionsComponent, canActivate: [requirePermission('transactions.view')] },
      { path: 'users',        component: UsersComponent,        canActivate: [requirePermission('users.manage')] },
      { path: 'roles',        component: RolesComponent,        canActivate: [requirePermission('roles.manage')] },
    ],
  },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
