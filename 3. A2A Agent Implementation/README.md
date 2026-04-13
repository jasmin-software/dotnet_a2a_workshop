``` bash
dotnet new web -n '3. A2A Agent Implementation'

cd '3. A2A Agent Implementation'
dotnet new gitignore

dotnet add package Microsoft.Agents.AI.Hosting.A2A.AspNetCore --version 1.0.0-preview.260402.1
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.5
dotnet add package Microsoft.Extensions.AI.OpenAI --version 10.4.1
dotnet add package Swashbuckle.AspNetCore --version 10.1.7
mkdir Tools
```

Replace `appsettings.Development.json` with this JSON code:

```json
{
    "GitHub": {
        "Token": "put-your-github-personal-access-token-here",
        "ApiEndpoint": "https://models.github.ai/inference",
        "Model": "openai/gpt-4o-mini"
    }
}
```

> [!NOTE]
>
> Replace `put-your-github-personal-access-token-here` with your GitHub Personal Access Token. 

Edit the `.gitignore` file and add to it `appsettings.Development.json` so that your secrets do not find their way into source control by mistake.

## Tools

Add the following C# class files into the `Tools` folder:

### CalendarEvents.cs

```C#
namespace A2AAgent.Tools;

public sealed class CalendarEvent {
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required DateTime Start { get; set; }
    public required DateTime End { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
}
```

### ICalendarStore.cs

```c#
namespace A2AAgent.Tools;

public interface ICalendarStore {
    IReadOnlyList<CalendarEvent> GetEvents(DateOnly date);
    void AddEvent(CalendarEvent calendarEvent);
}
```

### InMemoryCalendarStore

```C#
namespace A2AAgent.Tools;

public sealed class InMemoryCalendarStore : ICalendarStore {
    private readonly List<CalendarEvent> _events =
    [
        new CalendarEvent {
            Id = Guid.NewGuid().ToString(),
            Title = "Work",
            Start = new DateTime(2026, 4, 21, 9, 0, 0),
            End = new DateTime(2026, 4, 21, 17, 0, 0),
            Location = "Office"
        },
        new CalendarEvent {
            Id = Guid.NewGuid().ToString(),
            Title = "Commute to BCIT Downtown Campus",
            Start = new DateTime(2026, 4, 21, 17, 0, 0),
            End = new DateTime(2026, 4, 21, 18, 0, 0),
            Location = "Train"
        },
        new CalendarEvent {
            Id = Guid.NewGuid().ToString(),
            Title = "Workshop: Agent-to-Agent (A2A) with Microsoft Agent Framework",
            Start = new DateTime(2026, 4, 21, 18, 0, 0),
            End = new DateTime(2026, 4, 21, 20, 0, 0),
            Location = "BCIT Downtown Campus, Room 645"
        },
        new CalendarEvent {
            Id = Guid.NewGuid().ToString(),
            Title = "Sleep",
            Start = new DateTime(2026, 4, 21, 23, 0, 0),
            End = new DateTime(2026, 4, 22, 7, 0, 0),
            Location = "Bedroom"
        },
        new CalendarEvent {
            Id = Guid.NewGuid().ToString(),
            Title = "Work",
            Start = new DateTime(2026, 4, 22, 9, 0, 0),
            End = new DateTime(2026, 4, 22, 17, 0, 0),
            Location = "Office"
        },
        new CalendarEvent {
            Id = Guid.NewGuid().ToString(),
            Title = "Create my own A2A agent!",
            Start = new DateTime(2026, 4, 22, 19, 0, 0),
            End = new DateTime(2026, 4, 22, 20, 0, 0),
            Location = "Home"
        },
        new CalendarEvent {
            Id = Guid.NewGuid().ToString(),
            Title = "Sleep",
            Start = new DateTime(2026, 4, 22, 23, 0, 0),
            End = new DateTime(2026, 4, 23, 7, 0, 0),
            Location = "Bedroom"
        }
    ];

    public IReadOnlyList<CalendarEvent> GetEvents(DateOnly date) {
        return _events
            .Where(e => DateOnly.FromDateTime(e.Start) == date)
            .ToList();
    }

    public void AddEvent(CalendarEvent calendarEvent) {
        _events.Add(calendarEvent);
    }
}
```

### CalendarTool.cs

The `CalendarTool` has the smarts to carry out two related tasks. It can:
- Get all events that fall on a specific date
- Create an event given: the event title, start time, end time, event location (optional), and event description (optionsl)
```C#
using System.ComponentModel;

namespace A2AAgent.Tools;

internal static class CalendarTool {
    private static ICalendarStore _calendarStore = new InMemoryCalendarStore();

    public static void Initialize(ICalendarStore calendarStore) {
        _calendarStore = calendarStore;
    }

    [Description("Get calendar events for a given date in yyyy-MM-dd format.")]
    public static string GetEventsOnDate(
        [Description("Date in yyyy-MM-dd format")] string date) {
        if (!DateOnly.TryParse(date, out var parsedDate)) {
            return "Invalid date. Please provide the date in yyyy-MM-dd format.";
        }

        var events = _calendarStore.GetEvents(parsedDate);

        if (events.Count == 0) {
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
        if (!DateTime.TryParse(start, out var startTime)) {
            return "Invalid start time. Use ISO format like 2026-04-10T14:00:00.";
        }

        if (!DateTime.TryParse(end, out var endTime)) {
            return "Invalid end time. Use ISO format like 2026-04-10T15:00:00.";
        }

        if (endTime <= startTime) {
            return "End time must be after start time.";
        }

        var calendarEvent = new CalendarEvent {
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
```

Replace `Program.cs` with the following code:

``` C#
using A2A;
using A2A.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using A2AAgent.Tools;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ICalendarStore, InMemoryCalendarStore>();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

string githubToken = builder.Configuration["GitHub:Token"]
    ?? throw new InvalidOperationException("GitHub:Token is not set.");
string endpoint = builder.Configuration["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string model = builder.Configuration["GitHub:Model"] ?? "openai/gpt-4o-mini";
```

Create chat client and agent.
``` C#
IChatClient chatClient = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(githubToken),
    new OpenAIClientOptions
    {
        Endpoint = new Uri(endpoint),
    })
    .GetChatClient(model).AsIChatClient();

var calendarAgent = chatClient.AsAIAgent(
    name: "calendar",
    instructions:
    """
    You are a calendar assistant.
    You list calendar events given a date, and you create new events with 
    a title, start time, end time, and optional location and description.

    Rules:
    - When the user asks what is on a day, use the GetEventsOnDate tools.
    - If a user wants to create an event, gather title, start time, and end time if missing, 
      and use the CreateEvent tool.
    - Do not create the event if there is already an event that overlaps with the requested time.
    - Keep responses concise and helpful.
    - Always confirm created events with the exact time.

    - Today is 2026-04-21.
    """,
    tools: [
        AIFunctionFactory.Create(CalendarTool.GetEventsOnDate),
        AIFunctionFactory.Create(CalendarTool.CreateEvent)
    ]
);
builder.Services.AddSingleton(chatClient);

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();
```

Customize agent card.
``` C#
// customize agent card
AgentCard calendarAgentCard = new AgentCard {
    Name = "Calendar Agent",
    Description = "A calendar assistant that can list and create events for a particular date.",
    Version = "1.0.0",
    Skills = [
        new AgentSkill {
            Id = "get-events",
            Description = "Get calendar events for a given date in yyyy-MM-dd format.",
            Tags = ["calendar", "events", "date"],
            Examples = [
                "What events do I have on 2026-04-21?",
                "Do I have any meetings on April 21, 2026?"
            ]
        },
        new AgentSkill {
            Id = "create-event",
            Description = "Create a calendar event with title, start time, end time, and optional location and description.",
            Tags = ["calendar", "create", "event"],
            Examples = [
                "Create a calendar event titled 'Team Meeting' on April 21, 2026 from 2 PM to 3 PM.",
                "I have a meeting from 10 AM to 11 AM on April 21, 2026. Can you add it to my calendar?"
            ]
        }
    ]
};
```

Expose agent via A2A protocol
``` C#
// expose agent via A2A protocol
app.MapA2A(
    calendarAgent, 
    path: "/", 
    agentCard: calendarAgentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/"));

app.Run();
```
### Making Swagger UI the default landing page

Open `Properties/launchSettings.json` in your editor and make the following changes:

Inside the `http` and `https` blocks, add the following:

```json
"launchUrl": "swagger"
```

### Run app

In the terminal window:

```bash
dotnet watch
```

The web app will automatically open in your btowser at address [http://localhost:????/swagger/index.html] with the interface that looks like this:

![Swagger Interface](swagger.png)

<details>

<summary>Here's an example of the interaction:</summary>

```json
[Function Call: Weather_Agent]
Arguments:
{
  "query": "current weather in Vancouver"
}

[Function Result: {"current":{"time":"2026-04-12T08:15","temperature":"10.4°C","feelsLike":"9.4°C","windSpeed":"6.2 km/h","condition":"Overcast"},"today":{"hourly":[{"time":"2026-04-12T00:00","temperature":"8.5°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T01:00","temperature":"8.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T02:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T03:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T04:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T05:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T06:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T07:00","temperature":"9.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T08:00","temperature":"10.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T09:00","temperature":"10.7°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T10:00","temperature":"11.9°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T11:00","temperature":"12.9°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T12:00","temperature":"13.5°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T13:00","temperature":"13.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T14:00","temperature":"13.5°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T15:00","temperature":"13.7°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T16:00","temperature":"13.9°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T17:00","temperature":"13.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T18:00","temperature":"12.2°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T19:00","temperature":"11.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T20:00","temperature":"10.1°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T21:00","temperature":"9.5°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T22:00","temperature":"9.1°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T23:00","temperature":"9.4°C","precipitation":"0 mm","condition":"Overcast"}]}}]
Currently in Vancouver, it is 10.4°C and feels like 9.4°C. The sky is overcast with a light wind blowing at 6.2 km/h.

For the next few hours:
- At 9:00 AM, the temperature will rise slightly to 10.7°C with partly cloudy skies.
- By 10:00 AM, it will further increase to 11.9°C, still with partly cloudy conditions.
- At 11:00 AM, the temperature will reach 12.9°C and the sky will be mainly clear.

No precipitation is expected, and the weather will gradually become sunnier and warmer over the next few hours.
```

</details>