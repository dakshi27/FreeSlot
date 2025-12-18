import { Component } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { filter } from 'rxjs/operators';

import { CommonModule } from '@angular/common';
import { SplashScreenComponent } from './splash-screen/splash-screen.component';

import { DashboardComponent } from './dashboard/dashboard.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ButtonModule, CommonModule, SplashScreenComponent, DashboardComponent],
  styleUrl: './app.component.css',
  templateUrl: './app.component.html'
})
export class AppComponent {
  title = 'calendar-ui';
  showSplash = true;
  isSidebarCollapsed = false;
  isDarkMode = false;
  isFullWidthPage = false;

  constructor(private router: Router) {
    // Check for dark mode preference
    this.isDarkMode = localStorage.getItem('theme') === 'dark';
    this.applyTheme();

    // Listen to route changes to toggle layout
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.isFullWidthPage = event.url.includes('contributors') || event.url.includes('splash');
    });
  }

  onSplashComplete() {
    this.showSplash = false;
  }

  toggleTheme() {
    this.isDarkMode = !this.isDarkMode;
    localStorage.setItem('theme', this.isDarkMode ? 'dark' : 'light');
    this.applyTheme();
  }

  private applyTheme() {
    const element = document.querySelector('html');
    if (this.isDarkMode) {
      element?.classList.add('my-app-dark');
    } else {
      element?.classList.remove('my-app-dark');
    }
  }

  onSidebarToggle(collapsed: boolean) {
    this.isSidebarCollapsed = collapsed;
  }
}
