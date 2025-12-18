import { Component, signal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-dashboard',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './dashboard.component.html',
    styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
    isProjectsOpen = signal(true);
    isCollapsed = signal(false);

    // Notify parent component about state change for layout adjustment
    toggleChange = output<boolean>();

    toggleProjects() {
        if (!this.isCollapsed()) {
            this.isProjectsOpen.update(v => !v);
        }
    }

    toggleSidebar() {
        this.isCollapsed.update(v => !v);
        this.toggleChange.emit(this.isCollapsed());
    }
}
