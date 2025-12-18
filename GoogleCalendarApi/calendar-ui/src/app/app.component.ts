import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CalendarComponent } from './calendar/calendar.component';

@Component({
  selector: 'app-root',
  standalone:true,
  imports: [CalendarComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  template: `<app-calendar></app-calendar>`
})
export class AppComponent {
  title = 'calendar-ui';
}
