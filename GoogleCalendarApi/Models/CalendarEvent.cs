using System;
using System.Collections.Generic;

namespace GoogleCalendarApi.Models
{
    public class CalendarEvent
    {
        // local id (optional)
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Google-specific information (populated when fetching / saved)
        public string GoogleEventId { get; set; } = null;
        public string CalendarId { get; set; } = "primary";

        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Description { get; set; }
        public List<string> Attendees { get; set; } = new();

        public override string ToString()
        {
            string time = $"{StartTime:dd MMM yyyy HH:mm}-{EndTime:HH:mm}";
            string people = Attendees.Count > 0 ? string.Join(", ", Attendees) : "None";
            return $"{Title} at {time}\nAttendees: {people}\nDescription: {Description ?? "None"}";
        }
    }
}


