using GoogleCalendarApi.Models;

namespace GoogleCalendarApi.Services
{
    public interface IOverlapService
    {
        List<(CalendarEvent, CalendarEvent)> FindOverlaps(List<CalendarEvent> events);
    }
}
