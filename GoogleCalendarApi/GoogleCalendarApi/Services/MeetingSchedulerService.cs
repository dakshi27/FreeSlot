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

        public List<CalendarEvent> SuggestReschedules(
            List<CalendarEvent> allEvents,
            List<CalendarEvent> conflictingEvents,
            DateTime startOfDay,
            DateTime endOfDay,
            int bufferMinutes = 15)
        {
            // 1. Separate fixed events
            var fixedEvents = allEvents
                .Where(e => !conflictingEvents.Any(c => c.GoogleEventId == e.GoogleEventId))
                .OrderBy(e => e.StartTime)
                .ToList();

            var toReschedule = conflictingEvents.OrderBy(e => e.StartTime).ToList();
            
            // Determine Smart Start Time: Earliest of (9 AM, First Conflicting Event Start)
            // But we must respect working hours, so Max(9AM, EarliestStart)
            var workingStart = startOfDay.AddHours(9);
            var earliestConflict = toReschedule.Min(e => e.StartTime);
            
            // If earliest conflict is before 9 AM, stick to 9 AM (unless we want to support early meetings?)
            // Let's assume strict 9 AM start for now, but if the user scheduled at 9:15, we start at 9:15.
            var smartStart = earliestConflict < workingStart ? workingStart : earliestConflict;

            // Attempt 1: With Buffer, starting at Smart Start
            var schedule = TrySchedule(fixedEvents, toReschedule, smartStart, endOfDay, bufferMinutes);
            if (schedule != null) return schedule;

            // Attempt 2: No Buffer, starting at Smart Start
            schedule = TrySchedule(fixedEvents, toReschedule, smartStart, endOfDay, 0);
            if (schedule != null) return schedule;

            // Attempt 3: No Buffer, starting at 9 AM (Desperation)
            if (smartStart > workingStart)
            {
                schedule = TrySchedule(fixedEvents, toReschedule, workingStart, endOfDay, 0);
                if (schedule != null) return schedule;
            }

            // If all fails, return Attempt 2's result (even if incomplete/overtime) or just force it
            return ForceSchedule(fixedEvents, toReschedule, workingStart, bufferMinutes);
        }

        private List<CalendarEvent> TrySchedule(
            List<CalendarEvent> fixedEvents, 
            List<CalendarEvent> toReschedule, 
            DateTime startPoint, 
            DateTime endOfDay, 
            int bufferMinutes)
        {
            var proposed = new List<CalendarEvent>();
            var workingEnd = endOfDay.AddHours(17 - 24); // 5 PM (endOfDay is next midnight)
            // Fix: endOfDay passed from controller is next day midnight. 
            // Let's recalculate 5 PM correctly.
            workingEnd = startPoint.Date.AddHours(17);

            foreach (var meeting in toReschedule)
            {
                var duration = meeting.EndTime - meeting.StartTime;
                bool placed = false;

                var currentObstacles = fixedEvents.Concat(proposed).OrderBy(e => e.StartTime).ToList();
                DateTime current = startPoint;

                // Check gap before first event
                if (currentObstacles.Any())
                {
                    if (currentObstacles[0].StartTime - current >= duration)
                    {
                        proposed.Add(CreateEvent(meeting, current, duration));
                        placed = true;
                    }
                    else
                    {
                        for (int i = 0; i < currentObstacles.Count - 1; i++)
                        {
                            var gapStart = currentObstacles[i].EndTime.AddMinutes(bufferMinutes);
                            var gapEnd = currentObstacles[i + 1].StartTime;

                            if (gapStart < startPoint) gapStart = startPoint;

                            if (gapEnd - gapStart >= duration)
                            {
                                proposed.Add(CreateEvent(meeting, gapStart, duration));
                                placed = true;
                                break;
                            }
                        }
                    }

                    if (!placed)
                    {
                        var lastEnd = currentObstacles.Last().EndTime.AddMinutes(bufferMinutes);
                        if (lastEnd < startPoint) lastEnd = startPoint;

                        if (workingEnd - lastEnd >= duration)
                        {
                            proposed.Add(CreateEvent(meeting, lastEnd, duration));
                            placed = true;
                        }
                    }
                }
                else
                {
                    if (workingEnd - current >= duration)
                    {
                        proposed.Add(CreateEvent(meeting, current, duration));
                        placed = true;
                    }
                }

                if (!placed) return null; // Failed this attempt
            }

            return proposed;
        }

        private List<CalendarEvent> ForceSchedule(
            List<CalendarEvent> fixedEvents,
            List<CalendarEvent> toReschedule,
            DateTime startPoint,
            int bufferMinutes)
        {
            // Just stack them at the end, ignoring 5 PM limit
            var proposed = new List<CalendarEvent>();
            
            foreach (var meeting in toReschedule)
            {
                var duration = meeting.EndTime - meeting.StartTime;
                var currentObstacles = fixedEvents.Concat(proposed).OrderBy(e => e.StartTime).ToList();
                
                var lastTime = currentObstacles.Any() ? currentObstacles.Max(e => e.EndTime).AddMinutes(bufferMinutes) : startPoint;
                if (lastTime < startPoint) lastTime = startPoint;

                proposed.Add(CreateEvent(meeting, lastTime, duration));
            }
            return proposed;
        }

        private CalendarEvent CreateEvent(CalendarEvent original, DateTime start, TimeSpan duration)
        {
            return new CalendarEvent
            {
                Title = original.Title,
                StartTime = start,
                EndTime = start.Add(duration),
                Description = original.Description,
                GoogleEventId = original.GoogleEventId
            };
        }
    }
}
