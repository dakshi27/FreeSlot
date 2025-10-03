using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleCalendarApi.Models;


namespace GoogleCalendarApi.Services
{
    public class GoogleCalendarService : ICalendarService
    {
        private readonly CalendarService _svc;

        public GoogleCalendarService(IWebHostEnvironment env)
        {
            // Locate credentials.json in the content root (project root at dev time)
                string contentRoot = env.ContentRootPath;
                string credentialsPath = Path.Combine(contentRoot, "credentials.json");
                string tokenDir = Path.Combine(contentRoot, "GoogleOAuth");

            if (!File.Exists(credentialsPath))
                throw new FileNotFoundException("credentials.json not found at project root.", credentialsPath);

            Directory.CreateDirectory(tokenDir);

            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            var cred = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { CalendarService.Scope.CalendarReadonly },
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

            // choose calendars
            var calendars = new List<CalendarListEntry>();
            if (includeAllCalendars)
            {
                var cl = await _svc.CalendarList.List().ExecuteAsync();
                if (cl?.Items != null) calendars.AddRange(cl.Items);
            }
            else
            {
                calendars.Add(new CalendarListEntry { Id = "primary", Summary = "Primary" });
            }

            foreach (var cal in calendars)
            {
                var req = _svc.Events.List(cal.Id);
                req.TimeMin = from;
                req.TimeMax = to;
                req.ShowDeleted = false;
                req.SingleEvents = true; // expand recurring
                req.MaxResults = 2500;   // Google max per page
                req.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                var resp = await req.ExecuteAsync();
                if (resp?.Items == null) continue;

                foreach (var e in resp.Items)
                {
                    var start = e.Start.DateTime ?? DateTime.Parse(e.Start.Date);
                    var end = e.End.DateTime ?? DateTime.Parse(e.End.Date);

                    // Use attendee DisplayName when available, otherwise email
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
                        Attendees = attendees
                    });
                }
            }

            return results.OrderBy(r => r.StartTime).ToList();
        }
    }
}
    

