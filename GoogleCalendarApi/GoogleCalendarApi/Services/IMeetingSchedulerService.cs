using GoogleCalendarApi.Models;
using GoogleCalendarApi.Models;

namespace GoogleCalendarApi.Services
{
    public interface IMeetingSchedulerService
    {
        List<CalendarEvent> AutoResolveOverlaps(
            List<CalendarEvent> existingEvents,
            CalendarEvent newEvent,
            List<(DateTime Start, DateTime End)> freeSlots);

        List<CalendarEvent> SuggestReschedules(
            List<CalendarEvent> allEvents,
            List<CalendarEvent> conflictingEvents,
            DateTime startOfDay,
            DateTime endOfDay,
            int bufferMinutes = 15);

    }
}
