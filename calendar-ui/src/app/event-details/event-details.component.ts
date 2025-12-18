import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { AvatarModule } from 'primeng/avatar';
import { AvatarGroupModule } from 'primeng/avatargroup';
import { TooltipModule } from 'primeng/tooltip';
import { CalendarService } from '../calendar.service';

@Component({
    selector: 'app-event-details',
    standalone: true,
    imports: [CommonModule, ButtonModule, CardModule, AvatarModule, AvatarGroupModule, TooltipModule],
    templateUrl: './event-details.component.html',
    styleUrls: ['./event-details.component.css']
})
export class EventDetailsComponent implements OnInit {
    private router = inject(Router);
    private location = inject(Location);

    event: any;
    participants: any[] = [];
    description: string = '';

    // UI Helpers
    formattedDate: string = '';
    formattedTimeRange: string = '';
    isOrganizer: boolean = false;
    guestCount: number = 0;
    yesCount: number = 0;
    awaitingCount: number = 0;

    // User Response
    userResponse: 'yes' | 'no' | 'maybe' | null = null;

    ngOnInit() {
        // Get event data from router state
        const state = this.location.getState() as any;
        if (state && state.event) {
            this.event = state.event;
            this.loadRealData();
        } else {
            // If no state (e.g., direct refresh), go back to calendar
            this.router.navigate(['/calendar']);
        }
    }

    loadRealData() {
        if (!this.event) return;

        // Date & Time Formatting
        const start = new Date(this.event.start);
        const end = new Date(this.event.end);

        // Example: Tuesday, December 9
        this.formattedDate = start.toLocaleDateString('en-US', {
            weekday: 'long',
            month: 'long',
            day: 'numeric'
        });

        // Example: 10:30 - 11:30am
        const timeOptions: Intl.DateTimeFormatOptions = { hour: 'numeric', minute: '2-digit' };
        this.formattedTimeRange = `${start.toLocaleTimeString([], timeOptions)} – ${end.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit', timeZoneName: 'short' })}`;

        // Real Description
        this.description = this.event.description || 'No description provided.';

        // Real Participants
        if (this.event.attendees && this.event.attendees.length > 0) {
            this.guestCount = this.event.attendees.length;
            // Mocking status counts for UI demo
            this.yesCount = Math.ceil(this.guestCount / 2);
            this.awaitingCount = this.guestCount - this.yesCount;

            this.participants = this.event.attendees.map((attendee: string, index: number) => {
                let name = attendee;
                let isOrganizer = false;

                // Specific mapping as requested
                if (attendee === 'big44nhce@gmail.com') {
                    name = 'Dakshitha';
                    isOrganizer = true;
                    this.isOrganizer = true; // Current user is organizer for demo purposes if mapped
                } else if (index === 0) {
                    // Assume first person is organizer if not matched above for demo visual
                    isOrganizer = true;
                }

                return {
                    name: name,
                    email: attendee,
                    image: `https://ui-avatars.com/api/?name=${name}&background=random&color=fff`,
                    isOrganizer: isOrganizer,
                    status: isOrganizer ? 'yes' : 'awaiting' // simple mock logic
                };
            });
        } else {
            this.participants = [];
            this.guestCount = 0;
        }
    }

    goBack() {
        this.router.navigate(['/calendar']);
    }

    joinMeeting() {
        window.open('https://meet.google.com', '_blank');
    }

    deleteEvent() {
        // Todo: Implement delete
        console.log('Delete requested');
        this.goBack();
    }

    editEvent() {
        // Todo: Implement edit
        console.log('Edit requested');
    }

    setResponse(response: 'yes' | 'no' | 'maybe') {
        this.userResponse = response;
    }
}
