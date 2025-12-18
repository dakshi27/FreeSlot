// --- GUARANTEED FIX: Manually define the Message interface ---
// This ensures TypeScript knows the type, resolving the TS2305 error.
interface Message {
  severity?: 'success' | 'info' | 'warn' | 'error';
  summary?: string;
  detail?: string;
  closable?: boolean;
}
// --- END FIX ---

import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CalendarService } from '../calendar.service';
import { DatePipe } from '@angular/common';

// --- Official FullCalendar Imports ---
import { FullCalendarModule } from '@fullcalendar/angular';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';

// --- PrimeNG Imports ---
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { PanelModule } from 'primeng/panel';
import { MessagesModule } from 'primeng/messages';
import { SidebarModule } from 'primeng/sidebar';

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    FullCalendarModule,
    ButtonModule,
    CardModule,
    PanelModule,
    MessagesModule,
    SidebarModule
  ],
  providers: [DatePipe],
  templateUrl: './calendar.component.html',
  styleUrl: './calendar.component.css'
})
export class CalendarComponent implements OnInit {
  events: any;
  overlaps: any;
  suggestedReschedules: any[] = [];
  selectedDate: string = new Date().toISOString().split('T')[0];
  bufferMinutes: number = 15;
  loading = false;
  sidebarVisible: boolean = false;

  // Properties for FullCalendar
  options: any;
  allCalendarEvents: any[] = [];

  // Property uses the manually defined Message interface
  overlapMessages: Message[] = [];

  constructor(private calendarService: CalendarService, private datePipe: DatePipe, private router: Router) {
    this.options = {
      plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
      initialView: 'timeGridWeek',
      headerToolbar: {
        left: 'prev,next today',
        center: 'title',
        right: 'dayGridMonth,timeGridWeek,timeGridDay'
      },
      editable: false,
      selectable: false, // Disabling selection for a pure viewing app
      slotMinTime: '08:00:00',
      slotMaxTime: '18:00:00',
      height: 'auto',
      eventClick: (info: any) => this.handleEventClick(info)
    };
  }

  ngOnInit() {
    this.loadAllData();
  }

  handleEventClick(info: any) {
    const event = {
      title: info.event.title,
      start: info.event.start,
      end: info.event.end,
      description: info.event.extendedProps.description,
      attendees: info.event.extendedProps.attendees
    };
    this.router.navigate(['/event-details'], { state: { event: event } });
  }

  formatDate(date: string): string {
    return this.datePipe.transform(date, 'dd/MM/yyyy – hh:mm a') || '';
  }

  mapToCalendarEvents(data: any[], type: 'event' | 'overlap' | 'free', color: string): any[] {
    return data.map(item => {
      let title, start, end;

      if (type === 'event') {
        title = item.title;
        start = item.startTime;
        end = item.endTime;
      } else if (type === 'overlap') {
        title = `CONFLICT: ${item.event1} vs ${item.event2}`;
        start = item.start1;
        end = item.end2;
        color = '#EF4444';
      } else { // type === 'free'
        title = 'FREE SLOT';
        start = item.start;
        end = item.end;
        color = '#D1FAE5';
        return { title, start, end, display: 'background', color };
      }

      return {
        title: title,
        start: start,
        end: end,
        color: color,
        allDay: false,
        extendedProps: {
          description: item.description,
          attendees: item.attendees
        }
      };
    });
  }

  loadAllData() {
    this.loading = true;
    this.allCalendarEvents = [];
    this.overlapMessages = [];

    // 1. Load Events
    this.calendarService.getEvents(5, true).subscribe((res: any[]) => {
      this.events = this.removeDuplicates(res);
      const mappedEvents = this.mapToCalendarEvents(this.events, 'event', '#10B981');
      this.allCalendarEvents = [...this.allCalendarEvents, ...mappedEvents];
    });

    // 2. Load Overlaps
    this.calendarService.getOverlaps(5, true).subscribe(res => {
      this.overlaps = res;

      // Message array construction
      if (res.length > 0) {
        this.overlapMessages = [{
          severity: 'warn',
          summary: 'Conflicts Detected',
          detail: `You have ${res.length} schedule conflicts.`
        }];
      } else {
        this.overlapMessages = [{
          severity: 'success',
          summary: 'No Conflicts',
          detail: 'Schedule is clear!'
        }];
      }

      const mappedOverlaps = this.mapToCalendarEvents(res, 'overlap', '#EF4444');
      this.allCalendarEvents = [...this.allCalendarEvents, ...mappedOverlaps];
      this.loading = false;
    });

    // 3. Load Suggested Reschedules
    this.calendarService.getSuggestedReschedules(this.selectedDate, true, this.bufferMinutes).subscribe(res => {
      this.suggestedReschedules = res;
    });
  }

  removeDuplicates(events: any[]): any[] {
    const seen = new Set();
    return events.filter(event => {
      const key = `${event.title}-${event.startTime}-${event.endTime}`;
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });
  }
}