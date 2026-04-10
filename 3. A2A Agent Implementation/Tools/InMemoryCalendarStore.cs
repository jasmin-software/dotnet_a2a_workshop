public sealed class InMemoryCalendarStore : ICalendarStore
{
    private readonly List<CalendarEvent> _events =
    [
        new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Work",
            Start = new DateTime(2026, 4, 21, 9, 0, 0),
            End = new DateTime(2026, 4, 21, 17, 0, 0),
            Location = "Office"
        },
        new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Commute to BCIT Downtown Campus",
            Start = new DateTime(2026, 4, 21, 17, 0, 0),
            End = new DateTime(2026, 4, 21, 18, 0, 0),
            Location = "Train"
        },
        new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Workshop: Agent-to-Agent (A2A) with Microsoft Agent Framework",
            Start = new DateTime(2026, 4, 21, 18, 0, 0),
            End = new DateTime(2026, 4, 21, 20, 0, 0),
            Location = "BCIT Downtown Campus, Room 645"
        },
        new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Sleep",
            Start = new DateTime(2026, 4, 21, 23, 0, 0),
            End = new DateTime(2026, 4, 22, 7, 0, 0),
            Location = "Bedroom"
        },
        new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Work",
            Start = new DateTime(2026, 4, 22, 9, 0, 0),
            End = new DateTime(2026, 4, 22, 17, 0, 0),
            Location = "Office"
        },
        new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Create my own A2A agent!",
            Start = new DateTime(2026, 4, 22, 19, 0, 0),
            End = new DateTime(2026, 4, 22, 20, 0, 0),
            Location = "Home"
        },
        new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Sleep",
            Start = new DateTime(2026, 4, 22, 23, 0, 0),
            End = new DateTime(2026, 4, 23, 7, 0, 0),
            Location = "Bedroom"
        }
    ];

    public IReadOnlyList<CalendarEvent> GetEvents(DateOnly date)
    {
        return _events
            .Where(e => DateOnly.FromDateTime(e.Start) == date)
            .ToList();
    }

    public void AddEvent(CalendarEvent calendarEvent)
    {
        _events.Add(calendarEvent);
    }
}