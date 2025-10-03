using GoogleCalendarApi.Models;

namespace GoogleCalendarApi.Services
{
    public interface IFreeSlotService
    {
        List<TimeSlot> FindFreeSlots(List<CalendarEvent> events, DateTime from, DateTime to);
    }
}
