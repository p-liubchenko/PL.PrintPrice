import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { authGuard } from './guards/auth.guard';
import { setupGuard } from './guards/setup.guard';
import { changePasswordGuard } from './guards/change-password.guard';
import { adminGuard } from './guards/admin.guard';

import { LoginComponent } from './login/login.component';
import { OnboardingComponent } from './onboarding/onboarding.component';
import { ChangePasswordComponent } from './change-password/change-password.component';

import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { PrintersComponent } from './pages/printers/printers.component';
import { MaterialsComponent } from './pages/materials/materials.component';
import { CurrenciesComponent } from './pages/currencies/currencies.component';
import { SettingsComponent } from './pages/settings/settings.component';
import { TransactionsComponent } from './pages/transactions/transactions.component';
import { UsersComponent } from './pages/users/users.component';

const routes: Routes = [
  { path: 'onboarding', component: OnboardingComponent, canActivate: [setupGuard] },
  { path: 'login', component: LoginComponent },
  { path: 'change-password', component: ChangePasswordComponent, canActivate: [changePasswordGuard] },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardComponent },
      { path: 'printers', component: PrintersComponent },
      { path: 'materials', component: MaterialsComponent },
      { path: 'currencies', component: CurrenciesComponent },
      { path: 'settings', component: SettingsComponent },
      { path: 'transactions', component: TransactionsComponent },
      { path: 'users', component: UsersComponent, canActivate: [adminGuard] },
    ],
  },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
