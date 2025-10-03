/*using GoogleCalendarApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCalendarApi.Services
{
    public class FreeSlotService : IFreeSlotService
    {
        public List<TimeSlot> FindFreeSlots(List<CalendarEvent> events, DateTime from, DateTime to)
        {
            var freeSlots = new List<TimeSlot>();

            // Filter events within the range and sort by start time
            var sortedEvents = events
                .Where(e => e.EndTime > from && e.StartTime < to)
                .OrderBy(e => e.StartTime)
                .ToList();

            // Merge overlapping events
            var mergedEvents = new List<CalendarEvent>();
            foreach (var e in sortedEvents)
            {
                if (!mergedEvents.Any())
                {
                    mergedEvents.Add(new CalendarEvent
                    {
                        StartTime = e.StartTime,
                        EndTime = e.EndTime
                    });
                }
                else
                {
                    var last = mergedEvents.Last();
                    if (e.StartTime <= last.EndTime) // Overlap or touch
                    {
                        last.EndTime = last.EndTime > e.EndTime ? last.EndTime : e.EndTime;
                    }
                    else
                    {
                        mergedEvents.Add(new CalendarEvent
                        {
                            StartTime = e.StartTime,
                            EndTime = e.EndTime
                        });
                    }
                }
            }

            // Find free slots between merged events
            DateTime lastEndTime = from;
            foreach (var e in mergedEvents)
            {
                if (e.StartTime > lastEndTime)
                {
                    freeSlots.Add(new TimeSlot
                    {
                        Start = lastEndTime,
                        End = e.StartTime
                    });
                }

                lastEndTime = e.EndTime > lastEndTime ? e.EndTime : lastEndTime;
            }

            // Final slot after last event
            if (lastEndTime < to)
            {
                freeSlots.Add(new TimeSlot
                {
                    Start = lastEndTime,
                    End = to
                });
            }

            return freeSlots;
        }
    }
}*/

using GoogleCalendarApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCalendarApi.Services
{
    public class FreeSlotService : IFreeSlotService
    {
        public List<TimeSlot> FindFreeSlots(List<CalendarEvent> events, DateTime from, DateTime to)
        {
            var freeSlots = new List<TimeSlot>();

            TimeSpan workStart = new TimeSpan(9, 0, 0);  // 9:00 AM
            TimeSpan workEnd = new TimeSpan(18, 0, 0);   // 6:00 PM
            TimeSpan minSlotDuration = TimeSpan.FromMinutes(30);

            // Filter only Dakshitha's events within range
            var userEvents = events
                .Where(e => e.Attendees.Contains("Dakshitha"))
                .Where(e => e.EndTime > from && e.StartTime < to)
                .OrderBy(e => e.StartTime)
                .ToList();

            // Merge overlapping events
            var mergedEvents = new List<CalendarEvent>();
            foreach (var e in userEvents)
            {
                if (!mergedEvents.Any())
                {
                    mergedEvents.Add(new CalendarEvent { StartTime = e.StartTime, EndTime = e.EndTime });
                }
                else
                {
                    var last = mergedEvents.Last();
                    if (e.StartTime <= last.EndTime)
                    {
                        last.EndTime = last.EndTime > e.EndTime ? last.EndTime : e.EndTime;
                    }
                    else
                    {
                        mergedEvents.Add(new CalendarEvent { StartTime = e.StartTime, EndTime = e.EndTime });
                    }
                }
            }

            // Loop through each day
            for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
            {
                if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                var dayStart = day + workStart;
                var dayEnd = day + workEnd;

                var dayEvents = mergedEvents
                    .Where(e => e.StartTime.Date == day.Date)
                    .OrderBy(e => e.StartTime)
                    .ToList();

                DateTime currentTime = dayStart;

                foreach (var ev in dayEvents)
                {
                    if (ev.StartTime > currentTime && ev.StartTime - currentTime >= minSlotDuration)
                    {
                        freeSlots.Add(new TimeSlot
                        {
                            Start = currentTime,
                            End = ev.StartTime
                        });
                    }

                    currentTime = ev.EndTime > currentTime ? ev.EndTime : currentTime;
                }

                if (dayEnd - currentTime >= minSlotDuration)
                {
                    freeSlots.Add(new TimeSlot
                    {
                        Start = currentTime,
                        End = dayEnd
                    });
                }
            }

            return freeSlots;
        }
    }
}

