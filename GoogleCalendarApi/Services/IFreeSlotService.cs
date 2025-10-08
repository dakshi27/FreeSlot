using GoogleCalendarApi.Models;
using System;
using System.Collections.Generic;

namespace GoogleCalendarApi.Services
{
    public interface IFreeSlotService
    {
        // Existing method (you already have this)
        List<string> FindFreeSlotStrings(List<CalendarEvent> events, DateTime date);

        // 🔹 Add this overload for controller compatibility
        List<TimeSlot> FindFreeSlots(List<CalendarEvent> events, DateTime from, DateTime to);
    }
}


