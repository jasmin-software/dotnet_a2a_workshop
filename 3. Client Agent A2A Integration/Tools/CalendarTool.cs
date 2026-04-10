using System.ComponentModel;

internal static class CalendarTool
{
    private static ICalendarStore _calendarStore = new InMemoryCalendarStore();

    public static void Initialize(ICalendarStore calendarStore)
    {
        _calendarStore = calendarStore;
    }

    [Description("Get calendar events for a given date in yyyy-MM-dd format.")]
    public static string GetEventsOnDate(
        [Description("Date in yyyy-MM-dd format")] string date)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return "Invalid date. Please provide the date in yyyy-MM-dd format.";
        }

        var events = _calendarStore.GetEvents(parsedDate);

        if (events.Count == 0)
        {
            return $"No events found on {parsedDate:yyyy-MM-dd}.";
        }

        var lines = events
            .OrderBy(e => e.Start)
            .Select(e => $"- {e.Title}: {e.Start:yyyy-MM-dd HH:mm} to {e.End:yyyy-MM-dd HH:mm}");

        return string.Join(Environment.NewLine, lines);
    }

    [Description("Create a calendar event.")]
    public static string CreateEvent(
        [Description("Event title")] string title,
        [Description("Start time in ISO format, for example 2026-04-10T14:00:00")] string start,
        [Description("End time in ISO format, for example 2026-04-10T15:00:00")] string end,
        [Description("Optional event location")] string? location = null,
        [Description("Optional event description")] string? description = null)
    {
        if (!DateTime.TryParse(start, out var startTime))
        {
            return "Invalid start time. Use ISO format like 2026-04-10T14:00:00.";
        }

        if (!DateTime.TryParse(end, out var endTime))
        {
            return "Invalid end time. Use ISO format like 2026-04-10T15:00:00.";
        }

        if (endTime <= startTime)
        {
            return "End time must be after start time.";
        }

        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Start = startTime,
            End = endTime,
            Location = location,
            Description = description
        };

        _calendarStore.AddEvent(calendarEvent);

        return
            $"Created event '{calendarEvent.Title}' from " +
            $"{calendarEvent.Start:yyyy-MM-dd HH:mm} to {calendarEvent.End:yyyy-MM-dd HH:mm}.";
    }
}