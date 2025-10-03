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

        public CalendarController(ICalendarService calendar, IOverlapService overlapService, IFreeSlotService freeSlotService)
        {
            _calendar = calendar;
            _overlapService = overlapService;
            _freeSlotService = freeSlotService;
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
        public async Task<IActionResult> GetFreeSlotsAfterOverlaps([FromQuery] int days = 5, [FromQuery] bool allCalendars = true)
        {
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(days);

            var events = await _calendar.GetEventsAsync(start, end, allCalendars);
            var overlaps = _overlapService.FindOverlaps(events);

            // Extract only overlapping events (merge conflicting ones)
            var overlappingEvents = overlaps.SelectMany(o => new[] { o.Item1, o.Item2 }).ToList();

            // Find free slots after overlaps
            var freeSlots = _freeSlotService.FindFreeSlots(overlappingEvents, start, end);

            return Ok(freeSlots.Select(f => new
            {
                Start = f.Start,
                End = f.End
            }));
        }
    }
}

       
