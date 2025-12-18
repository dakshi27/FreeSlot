import { Component, signal, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CalendarService } from '../calendar.service';

@Component({
    selector: 'app-recommender',
    standalone: true,
    imports: [CommonModule, FormsModule, DatePipe],
    templateUrl: './recommender.component.html',
    styleUrl: './recommender.component.css'
})
export class RecommenderComponent {
    private calendarService = inject(CalendarService);

    selectedDate = signal(new Date().toISOString().split('T')[0]);
    bufferMinutes = signal(15);
    isLoading = signal(false);
    error = signal<string | null>(null);

    suggestions = signal<any[]>([]);

    // We'll keep conflicts for now if the API returns them or if we want to mock them, 
    // but the user specific request focused on suggestions. 
    // The current API response shown by user only has suggestions.
    // We will clear mock data initially.
    conflicts = signal<any[]>([]);

    analyzeSchedule() {
        this.isLoading.set(true);
        this.error.set(null);
        this.suggestions.set([]);

        this.calendarService.getSuggestedReschedules(
            this.selectedDate(),
            true, // allCalendars defaults to true
            this.bufferMinutes()
        ).subscribe({
            next: (data) => {
                this.suggestions.set(data);
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error('Error fetching suggestions', err);
                this.error.set('Failed to analyze schedule. Please try again.');
                this.isLoading.set(false);
            }
        });
    }

    clearResults() {
        this.suggestions.set([]);
        this.error.set(null);
    }
}
