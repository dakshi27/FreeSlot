import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarService } from '../calendar.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-calendar',
  standalone:true,
  imports: [CommonModule],
  providers: [DatePipe],
  templateUrl: './calendar.component.html',
  styleUrl: './calendar.component.css'
})
export class CalendarComponent {
  events: any;
  overlaps: any;
  freeSlots: any;

  constructor(private calendarService: CalendarService , private datePipe: DatePipe) {}
  loading=false;
  
  formatDate(date: string): string {
  return this.datePipe.transform(date, 'dd/MM/yyyy – hh:mm a') || '';
}

loadEvents() {
  this.loading = true;
  this.calendarService.getEvents(5, true).subscribe((res: any[]) => {
    const cleaned = res.map((event: any) => ({
      ...event,
      attendees: event.attendees.map((a: string) =>
        a === 'big44nhce@gmail.com' ? 'Dakshitha' : a
      )
    }));
    this.events = this.removeDuplicates(cleaned);
    this.loading = false;
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


  loadOverlaps() {
    this.calendarService.getOverlaps(5, true).subscribe(res => this.overlaps = res);
  }

  loadFreeSlots() {
    this.calendarService.getFreeSlots(5, true).subscribe(res => this.freeSlots = res);
  }
}
