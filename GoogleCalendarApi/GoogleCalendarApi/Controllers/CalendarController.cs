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



        //  Clean free slot detection for a single date (new endpoint)


        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleMeeting(
     [FromBody] CreateMeetingRequest request,
     [FromQuery] bool allCalendars = true)
        {
            // 1) Load existing events for the day
            var startOfDay = request.StartTime.Date;
            var endOfDay = startOfDay.AddDays(1);
            var existingEvents = await _calendar.GetEventsAsync(startOfDay, endOfDay, allCalendars);

            // 2) Build the local new event
            var newEvent = new CalendarEvent
            {
                Title = request.Title,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Description = request.Description
            };

            // 3) Compute free slots and prepare tuple list
            var freeSlots = _freeSlotService.FindFreeSlots(existingEvents, startOfDay, endOfDay);
            var slotTuples = freeSlots.Select(s => (s.Start, s.End)).ToList();

            // 4) Resolve overlaps locally
            var updatedEvents = _meetingSchedulerService.AutoResolveOverlaps(existingEvents, newEvent, slotTuples);

            // 5) Persist changes

            // 5a) Save the newly created event
            var createdEvent = await _calendar.CreateEventAsync(
                updatedEvents.First(e =>
                    e.Title == newEvent.Title &&
                    e.StartTime == newEvent.StartTime &&
                    e.EndTime == newEvent.EndTime)
            );

            // 5b) For existing events that moved, update them on Google safely

            // ✅ FIX: Safely handle duplicate GoogleEventIds
            var originalByGoogleId = existingEvents
                .Where(e => !string.IsNullOrEmpty(e.GoogleEventId))
                .GroupBy(e => e.GoogleEventId)
                .ToDictionary(g => g.Key, g => g.First());

            var updates = new List<CalendarEvent>();

            foreach (var ue in updatedEvents)
            {
                if (string.IsNullOrEmpty(ue.GoogleEventId))
                    continue; // newly created event already handled

                if (originalByGoogleId.TryGetValue(ue.GoogleEventId, out var original))
                {
                    // detect if event changed
                    bool changed = original.StartTime != ue.StartTime ||
                                   original.EndTime != ue.EndTime ||
                                   original.Title != ue.Title;

                    if (changed)
                    {
                        // ensure correct CalendarId before updating
                        ue.CalendarId = original.CalendarId;

                        try
                        {
                            var updatedOnGoogle = await _calendar.UpdateEventAsync(ue);
                            updates.Add(updatedOnGoogle);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Failed to update event {ue.GoogleEventId}: {ex.Message}");
                        }
                    }
                }
            }

            // 6) Build final response
            return Ok(new
            {
                Message = "Meeting scheduled successfully. Overlaps resolved and changes saved to Google Calendar.",
                Created = new
                {
                    createdEvent.Title,
                    createdEvent.StartTime,
                    createdEvent.EndTime,
                    createdEvent.GoogleEventId
                },
                Updated = updates.Select(u => new
                {
                    u.Title,
                    u.StartTime,
                    u.EndTime,
                    u.GoogleEventId
                }),
                All = updatedEvents.Select(e => new
                {
                    e.Title,
                    e.StartTime,
                    e.EndTime,
                    e.GoogleEventId,
                    e.CalendarId
                })
            });
        }


        [HttpGet("suggest-reschedules")]
        public async Task<IActionResult> SuggestReschedules(
            [FromQuery] DateTime date,
            [FromQuery] bool allCalendars = true,
            [FromQuery] int bufferMinutes = 15)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            // 1. Get all events for the day
            var allEvents = await _calendar.GetEventsAsync(startOfDay, endOfDay, allCalendars);

            // 2. Identify conflicting events
            var overlaps = _overlapService.FindOverlaps(allEvents);
            var conflictingEvents = overlaps
                .SelectMany(o => new[] { o.Item1, o.Item2 })
                .GroupBy(e => e.GoogleEventId) // Deduplicate by ID
                .Select(g => g.First())
                .ToList();

            if (!conflictingEvents.Any())
            {
                return Ok(new { Message = "No conflicts found." });
            }

            // 3. Generate proposed schedule
            var proposedSchedule = _meetingSchedulerService.SuggestReschedules(
                allEvents, 
                conflictingEvents, 
                startOfDay, 
                endOfDay,
                bufferMinutes);

            return Ok(proposedSchedule.Select(e => new
            {
                Title = e.Title,
                NewStart = e.StartTime,
                NewEnd = e.EndTime,
                OriginalId = e.GoogleEventId
            }));
        }
    }
}

       
