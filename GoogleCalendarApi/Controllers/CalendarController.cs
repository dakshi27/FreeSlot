using GoogleCalendarApi.Models;
using GoogleCalendarApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace GoogleCalendarApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendar;
        private readonly IOverlapService _overlapService;
        private readonly IFreeSlotService _freeSlotService;
        private readonly IMeetingSchedulerService _meetingSchedulerService;

        public CalendarController(
            ICalendarService calendar, 
            IOverlapService overlapService, 
            IFreeSlotService freeSlotService, 
            IMeetingSchedulerService meetingSchedulerService)
        {
            _calendar = calendar;
            _overlapService = overlapService;
            _freeSlotService = freeSlotService;
            _meetingSchedulerService = meetingSchedulerService;
        }

        // GET /api/calendar/events?days=5&allCalendars=true
        [HttpGet("events")]
        public async Task<IActionResult> GetEvents([FromQuery] int days = 5, [FromQuery] bool allCalendars = true)
        {
            // window from midnight today to midnight N days later (captures all-day and timed events)
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(days);

            var events = await _calendar.GetEventsAsync(start, end, allCalendars);
            return Ok(events);
        }

        // GET /api/calendar/events/range?from=2025-08-25&to=2025-08-28&allCalendars=true
        [HttpGet("events/range")]
        public async Task<IActionResult> GetEventsInRange([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] bool allCalendars = true)
        {
            if (to <= from) return BadRequest("`to` must be after `from`.");
            var events = await _calendar.GetEventsAsync(from, to, allCalendars);
            return Ok(events);
        }

        [HttpGet("overlaps")]
        public async Task<IActionResult> GetOverlappingEvents(
           [FromQuery] int days = 5,
           [FromQuery] bool allCalendars = true)
        {
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(days);

            var events = await _calendar.GetEventsAsync(start, end, allCalendars);
            var overlaps = _overlapService.FindOverlaps(events);

            return Ok(overlaps.Select(o => new
            {
                Event1 = o.Item1.Title,
                Start1 = o.Item1.StartTime,
                End1 = o.Item1.EndTime,
                Event2 = o.Item2.Title,
                Start2 = o.Item2.StartTime,
                End2 = o.Item2.EndTime
            }));
        }

        [HttpGet("findFreeSlots")]
        public async Task<IActionResult> GetFreeSlotsAfterOverlaps(
             [FromQuery] int days = 5,
             [FromQuery] bool allCalendars = true)
        {
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(days);

            var events = await _calendar.GetEventsAsync(start, end, allCalendars);
            var overlaps = _overlapService.FindOverlaps(events);

            // Merge conflicting events
            var overlappingEvents = overlaps
                .SelectMany(o => new[] { o.Item1, o.Item2 })
                .Distinct()
                .ToList();

            var freeSlots = _freeSlotService.FindFreeSlots(overlappingEvents, start, end);

            return Ok(freeSlots.Select(f => new
            {
                Start = f.Start,
                End = f.End
            }));
        }

        // ✅ 2. Clean free slot detection for a single date (new endpoint)
        [HttpGet("freeSlots")]
        public async Task<IActionResult> GetFreeSlots([FromQuery] DateTime date, [FromQuery] bool allCalendars = true)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            // Use your existing GetEventsAsync to get all events for that date
            var events = await _calendar.GetEventsAsync(startOfDay, endOfDay, allCalendars);

            // Find textual free slot strings
            var freeSlotStrings = _freeSlotService.FindFreeSlotStrings(events, date);

            return Ok(freeSlotStrings);
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleMeeting(
    [FromBody] CreateMeetingRequest request,
    [FromQuery] bool allCalendars = true)
        {
            // 1️⃣ Get existing events for that same day
            var startOfDay = request.StartTime.Date;
            var endOfDay = startOfDay.AddDays(1);

            var existingEvents = await _calendar.GetEventsAsync(startOfDay, endOfDay, allCalendars);

            // 2️⃣ Create the new meeting
            var newEvent = new CalendarEvent
            {
                Id = Guid.NewGuid().ToString(),
                Title = request.Title,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Description = request.Description
            };

            // 3️⃣ Compute free slots for that day
            var freeSlots = _freeSlotService.FindFreeSlots(existingEvents, startOfDay, endOfDay);

            // 🔁 Convert TimeSlot to tuple format
            var slotTuples = freeSlots
                .Select(slot => (slot.Start, slot.End))
                .ToList();

            // 4️⃣ Auto-resolve overlaps (using your MeetingSchedulerService)
            var updatedEvents = _meetingSchedulerService.AutoResolveOverlaps(existingEvents, newEvent, slotTuples);

            // 5️⃣ Return a clean response
            return Ok(new
            {
                Message = "Meeting scheduled successfully. Overlaps resolved automatically.",
                Events = updatedEvents.Select(e => new
                {
                    e.Title,
                    e.StartTime,
                    e.EndTime
                })
            });
        }

    }
}

       
