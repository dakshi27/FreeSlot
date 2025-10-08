using System;
using System.Collections.Generic;
using System.Linq;
using GoogleCalendarApi.Models;

namespace GoogleCalendarApi.Services
{
    public class MeetingSchedulerService : IMeetingSchedulerService
    {
        public List<CalendarEvent> AutoResolveOverlaps(
            List<CalendarEvent> existingEvents,
            CalendarEvent newEvent,
            List<(DateTime Start, DateTime End)> freeSlots)
        {
            var updatedEvents = new List<CalendarEvent>(existingEvents);

            // 1️⃣ Check if the new event overlaps
            bool hasOverlap = existingEvents.Any(e =>
                (newEvent.StartTime < e.EndTime && newEvent.EndTime > e.StartTime));

            if (!hasOverlap)
            {
                updatedEvents.Add(newEvent);
                return updatedEvents;
            }

            // 2️⃣ Try to find a free slot for the new event
            var duration = newEvent.EndTime - newEvent.StartTime;

            foreach (var slot in freeSlots)
            {
                var slotDuration = slot.End - slot.Start;
                if (slotDuration >= duration)
                {
                    // Move the meeting here
                    newEvent.StartTime = slot.Start;
                    newEvent.EndTime = slot.Start.Add(duration);
                    updatedEvents.Add(newEvent);
                    return updatedEvents;
                }
            }

            // 3️⃣ If no free slot found, just append after last event
            var lastEventEnd = existingEvents.Max(e => e.EndTime);
            newEvent.StartTime = lastEventEnd.AddMinutes(5);
            newEvent.EndTime = newEvent.StartTime.Add(duration);
            updatedEvents.Add(newEvent);

            return updatedEvents;
        }
    }
}
