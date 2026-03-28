import { APP_INITIALIZER, NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';

import { AuthService } from './services/auth.service';
import { AuthInterceptor } from './interceptors/auth.interceptor';

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

function initApp(auth: AuthService): () => Promise<void> {
  return () => auth.checkSetupStatus();
}

@NgModule({
  declarations: [
    App,
    LoginComponent,
    OnboardingComponent,
    ChangePasswordComponent,
    DashboardComponent,
    PrintersComponent,
    MaterialsComponent,
    CurrenciesComponent,
    SettingsComponent,
    TransactionsComponent,
    UsersComponent,
    RolesComponent,
    ProfileComponent,
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: APP_INITIALIZER, useFactory: initApp, deps: [AuthService], multi: true },
  ],
  bootstrap: [App],
})
export class AppModule {}
