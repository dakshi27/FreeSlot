using GoogleCalendarApi.Models;

namespace GoogleCalendarApi.Services
{
    public interface IMeetingSchedulerService
    {
        List<CalendarEvent> AutoResolveOverlaps(
    List<CalendarEvent> events,
    CalendarEvent newEvent,
    List<(DateTime Start, DateTime End)> freeSlots);

    }
}
