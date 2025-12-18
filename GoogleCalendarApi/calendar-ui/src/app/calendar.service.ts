// src/app/calendar.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private baseUrl = 'https://localhost:7265/api/Calendar';

  constructor(private http: HttpClient) {}

  getEvents(days: number, allCalendars: boolean): Observable<any> {
    return this.http.get(`${this.baseUrl}/events?days=${days}&allCalendars=${allCalendars}`);
  }

  getEventsInRange(from: string, to: string, allCalendars: boolean): Observable<any> {
    return this.http.get(`${this.baseUrl}/events/range?from=${from}&to=${to}&allCalendars=${allCalendars}`);
  }

  getOverlaps(days: number, allCalendars: boolean): Observable<any> {
    return this.http.get(`${this.baseUrl}/overlaps?days=${days}&allCalendars=${allCalendars}`);
  }

  getFreeSlots(days: number, allCalendars: boolean): Observable<any> {
    return this.http.get(`${this.baseUrl}/findFreeSlots?days=${days}&allCalendars=${allCalendars}`);
  }
}
