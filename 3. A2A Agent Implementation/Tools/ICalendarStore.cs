public interface ICalendarStore
{
    IReadOnlyList<CalendarEvent> GetEvents(DateOnly date);
    void AddEvent(CalendarEvent calendarEvent);
}