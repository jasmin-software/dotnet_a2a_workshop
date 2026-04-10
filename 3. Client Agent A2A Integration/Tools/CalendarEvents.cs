public sealed class CalendarEvent
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required DateTime Start { get; set; }
    public required DateTime End { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
}