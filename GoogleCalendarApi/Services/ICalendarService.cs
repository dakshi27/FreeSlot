using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoogleCalendarApi.Models;

namespace GoogleCalendarApi.Services
{
    public interface ICalendarService
    {
        Task<List<CalendarEvent>> GetEventsAsync(DateTime from, DateTime to, bool includeAllCalendars);

        Task<CalendarEvent> CreateEventAsync(CalendarEvent newEvent);
        Task<CalendarEvent> UpdateEventAsync(CalendarEvent updatedEvent);

    }
}

