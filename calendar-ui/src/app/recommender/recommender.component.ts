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

    applySuggestion(item: any) {
        // Helper to format date as local ISO string (YYYY-MM-DDTHH:mm:ss.sss)
        // This ensures the backend receives the exact time shown on the UI, avoiding UTC conversion issues (e.g. +5:30 vs +5:00)
        const toLocalISO = (dateStr: string) => {
            const date = new Date(dateStr);
            const pad = (n: number) => n.toString().padStart(2, '0');
            const year = date.getFullYear();
            const month = pad(date.getMonth() + 1);
            const day = pad(date.getDate());
            const hours = pad(date.getHours());
            const minutes = pad(date.getMinutes());
            const seconds = pad(date.getSeconds());
            const ms = date.getMilliseconds().toString().padStart(3, '0');
            return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}.${ms}`;
        };

        // Map suggestion item to API payload
        const payload = {
            title: item.title,
            startTime: toLocalISO(item.newStart),
            endTime: toLocalISO(item.newEnd),
            description: item.description || 'Rescheduled via FreeSlot' // Default description if missing
        };

        this.calendarService.scheduleMeeting(payload).subscribe({
            next: (response) => {
                console.log('Meeting scheduled successfully', response);
                // Ideally, remove the item from the list or show a success message
                // For now, we can just remove it from the local suggestions list to indicate it's done
                this.suggestions.update(current => current.filter(s => s !== item));
                alert('Meeting rescheduled successfully!');
            },
            error: (err) => {
                console.error('Error scheduling meeting', err);
                alert('Failed to apply schedule. Please try again.');
            }
        });
    }
}
