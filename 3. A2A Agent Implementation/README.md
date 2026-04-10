``` bash
dotnet new web -n '3. A2A Agent Implementation'

cd '3. A2A Agent Implementation'
dotnet new gitignore

dotnet add package Microsoft.Agents.AI.Hosting.A2A.AspNetCore --version 1.0.0-preview.260402.1
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.5
dotnet add package Microsoft.Extensions.AI.OpenAI --version 10.4.1
dotnet add package Swashbuckle.AspNetCore --version 10.1.7
```

# TODO: 
## Add instruction to add the Tools folder and its content
# OR Suggestion: 
## Maybe people can just clone this project with the Tools folder completed and they can just focus on the content in Program.cs

Set up chat client configuration
``` C#
using A2A;
using A2A.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ICalendarStore, InMemoryCalendarStore>();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

string githubToken = builder.Configuration["GitHub:Token"]
    ?? throw new InvalidOperationException("GitHub:Token is not set.");
string endpoint = builder.Configuration["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string model = builder.Configuration["GitHub:Model"] ?? "openai/gpt-4o-mini";
```

Create chat client and agent
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

Customize agent card
``` C#
AgentCard calendarAgentCard = new AgentCard
{
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
app.MapA2A(
    calendarAgent, 
    path: "/", 
    agentCard: calendarAgentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/"));

app.Run();
```