using GoogleCalendarApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCalendarApi.Services
{
    public class FreeSlotService : IFreeSlotService
    {
        // ✅ Implementation 1 - Find structured free slots
        public List<TimeSlot> FindFreeSlots(List<CalendarEvent> events, DateTime from, DateTime to)
        {
            var freeSlots = new List<TimeSlot>();
            var workingStart = from.Date.AddHours(9);   // 9 AM fixed working hours
            var workingEnd = from.Date.AddHours(17);    // 5 PM

            // Filter only events for that day
            var dayEvents = events
                .Where(e => e.StartTime.Date == from.Date)
                .OrderBy(e => e.StartTime)
                .ToList();

            DateTime current = workingStart;

            foreach (var ev in dayEvents)
            {
                if (ev.StartTime > current)
                {
                    freeSlots.Add(new TimeSlot
                    {
                        Start = current,
                        End = ev.StartTime
                    });
                }

                if (ev.EndTime > current)
                    current = ev.EndTime;
            }

            // Last slot until 5 PM
            if (current < workingEnd)
            {
                freeSlots.Add(new TimeSlot
                {
                    Start = current,
                    End = workingEnd
                });
            }

            return freeSlots;
        }

        // ✅ Implementation 2 - String format version
        public List<string> FindFreeSlotStrings(List<CalendarEvent> events, DateTime date)
        {
            var structured = FindFreeSlots(events, date, date);
            return structured.Select(s => $"Free: {s.Start:HH:mm} - {s.End:HH:mm}").ToList();
        }
    }
}






