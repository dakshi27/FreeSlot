using GoogleCalendarApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCalendarApi.Services
{
    public class OverlapService : IOverlapService
    {
        public List<(CalendarEvent, CalendarEvent)> FindOverlaps(List<CalendarEvent> events)
        {
            var overlaps = new List<(CalendarEvent, CalendarEvent)>();

           
            var sorted = events.OrderBy(e => e.StartTime).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    if (sorted[j].StartTime < sorted[i].EndTime)
                    {
                        
                        if (IsSameEvent(sorted[i], sorted[j])) continue;

                        overlaps.Add((sorted[i], sorted[j]));
                    }
                    else
                    {
                        break; 
                    }
                }
            }

            
            var unique = overlaps
                .Select(pair => new
                {
                    Key = GetEventKey(pair.Item1) + "|" + GetEventKey(pair.Item2),
                    Pair = pair
                })
                .GroupBy(x => x.Key)
                .Select(g => g.First().Pair)
                .ToList();

            return unique;
        }

        private bool IsSameEvent(CalendarEvent e1, CalendarEvent e2)
        {
            return e1.Title == e2.Title &&
                   e1.StartTime == e2.StartTime &&
                   e1.EndTime == e2.EndTime;
        }

        private string GetEventKey(CalendarEvent e)
        {
            return $"{e.Title}_{e.StartTime:O}_{e.EndTime:O}";
        }
    }
}
