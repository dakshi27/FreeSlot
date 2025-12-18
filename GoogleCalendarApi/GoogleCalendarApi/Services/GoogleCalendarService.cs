using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleCalendarApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCalendarApi.Services
{
    public class GoogleCalendarService : ICalendarService
    {
        private readonly CalendarService _svc;

        public GoogleCalendarService(IWebHostEnvironment env)
        {
            string contentRoot = env.ContentRootPath;
            string credentialsPath = Path.Combine(contentRoot, "credentials.json");
            string tokenDir = Path.Combine(contentRoot, "GoogleOAuth");

            if (!File.Exists(credentialsPath))
                throw new FileNotFoundException("credentials.json not found at project root.", credentialsPath);

            Directory.CreateDirectory(tokenDir);

            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            var cred = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { CalendarService.Scope.Calendar }, // <-- write access included
                "user",
                CancellationToken.None,
                new FileDataStore(tokenDir, true)
            ).Result;

            _svc = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = "GoogleCalendarAPI",
            });
        }

        public async Task<List<CalendarEvent>> GetEventsAsync(DateTime from, DateTime to, bool includeAllCalendars)
        {
            var results = new List<CalendarEvent>();
            var calendars = new List<CalendarListEntry>();

            if (includeAllCalendars)
            {
                var cl = await _svc.CalendarList.List().ExecuteAsync();
                if (cl?.Items != null)
                {
                    // ✅ Prevent duplicate calendar IDs (root cause of your crash)
                    calendars.AddRange(
                        cl.Items
                          .Where(c => !string.IsNullOrWhiteSpace(c.Id))
                          .GroupBy(c => c.Id)
                          .Select(g => g.First())
                    );
                }
            }
            else
            {
                calendars.Add(new CalendarListEntry { Id = "primary", Summary = "Primary" });
            }

            foreach (var cal in calendars)
            {
                try
                {
                    var req = _svc.Events.List(cal.Id);
                    req.TimeMin = from;
                    req.TimeMax = to;
                    req.ShowDeleted = false;
                    req.SingleEvents = true;
                    req.MaxResults = 2500;
                    req.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                    var resp = await req.ExecuteAsync();
                    if (resp?.Items == null) continue;

                    foreach (var e in resp.Items)
                    {
                        // Handle both full-day and timed events
                        var start = e.Start.DateTime ?? DateTime.Parse(e.Start.Date);
                        var end = e.End.DateTime ?? DateTime.Parse(e.End.Date);

                        var attendees = e.Attendees?
                            .Select(a => !string.IsNullOrWhiteSpace(a.DisplayName) ? a.DisplayName : a.Email)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList() ?? new List<string>();

                        results.Add(new CalendarEvent
                        {
                            Title = string.IsNullOrWhiteSpace(e.Summary) ? "(No title)" : e.Summary,
                            StartTime = start.ToLocalTime(),
                            EndTime = end.ToLocalTime(),
                            Description = e.Description,
                            Attendees = attendees,
                            GoogleEventId = e.Id,
                            CalendarId = cal.Id
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching events for calendar '{cal.Summary}': {ex.Message}");
                }
            }

            // ✅ Remove duplicate events (some shared events appear in multiple calendars)
            return results
                .GroupBy(r => r.GoogleEventId)
                .Select(g => g.First())
                .OrderBy(r => r.StartTime)
                .ToList();
        }

        public async Task<CalendarEvent> CreateEventAsync(CalendarEvent newEvent)
        {
            var googleEvent = new Event
            {
                Summary = newEvent.Title,
                Description = newEvent.Description,
                Start = new EventDateTime
                {
                    DateTime = newEvent.StartTime,
                    TimeZone = "Asia/Kolkata"
                },
                End = new EventDateTime
                {
                    DateTime = newEvent.EndTime,
                    TimeZone = "Asia/Kolkata"
                },
                Attendees = newEvent.Attendees?.Select(a => new EventAttendee { Email = a }).ToList()
            };

            // ✅ Always insert to "primary" by default
            var created = await _svc.Events.Insert(googleEvent, "primary").ExecuteAsync();

            return new CalendarEvent
            {
                Title = created.Summary,
                StartTime = created.Start?.DateTime ?? DateTime.MinValue,
                EndTime = created.End?.DateTime ?? DateTime.MinValue,
                Description = created.Description,
                Attendees = created.Attendees?.Select(a => a.Email).ToList() ?? new List<string>(),
                GoogleEventId = created.Id,
                CalendarId = "primary"
            };
        }

        public async Task<CalendarEvent> UpdateEventAsync(CalendarEvent updatedEvent)
        {
            if (string.IsNullOrWhiteSpace(updatedEvent.GoogleEventId) || string.IsNullOrWhiteSpace(updatedEvent.CalendarId))
                throw new ArgumentException("Event must contain GoogleEventId and CalendarId to be updated.");

            var existing = await _svc.Events.Get(updatedEvent.CalendarId, updatedEvent.GoogleEventId).ExecuteAsync();

            existing.Summary = updatedEvent.Title;
            existing.Description = updatedEvent.Description;
            existing.Start = new EventDateTime
            {
                DateTime = updatedEvent.StartTime,
                TimeZone = "Asia/Kolkata"
            };
            existing.End = new EventDateTime
            {
                DateTime = updatedEvent.EndTime,
                TimeZone = "Asia/Kolkata"
            };
            existing.Attendees = updatedEvent.Attendees?.Select(a => new EventAttendee { Email = a }).ToList();

            var result = await _svc.Events.Update(existing, updatedEvent.CalendarId, updatedEvent.GoogleEventId).ExecuteAsync();

            return new CalendarEvent
            {
                Title = result.Summary,
                StartTime = result.Start?.DateTime ?? DateTime.MinValue,
                EndTime = result.End?.DateTime ?? DateTime.MinValue,
                Description = result.Description,
                Attendees = result.Attendees?.Select(a => a.Email).ToList() ?? new List<string>(),
                GoogleEventId = result.Id,
                CalendarId = updatedEvent.CalendarId
            };
        }
    }
}
