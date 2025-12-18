using System;

namespace GoogleCalendarApi.Models
{
    public class CreateMeetingRequest
    {
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Description { get; set; }
    }
}

