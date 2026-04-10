using A2A;
using A2A.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

string githubToken = builder.Configuration["GitHub:Token"]
    ?? throw new InvalidOperationException("GitHub:Token is not set.");
string endpoint = builder.Configuration["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string model = builder.Configuration["GitHub:Model"] ?? "openai/gpt-4o-mini";

AITool[] tools =
[
    AIFunctionFactory.Create(CalendarTool.GetEventsOnDate),
    AIFunctionFactory.Create(CalendarTool.CreateEvent)
];

// Create chat client
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
    Help users view and create calendar events.

    Rules:
    - When the user asks what is on a day, use the calendar tools.
    - If a user wants to create an event, gather title, start time, and end time if missing.
    - Keep responses concise and helpful.
    - Always confirm created events with the exact time.
    """,
    tools: tools
);
builder.Services.AddSingleton(chatClient);

builder.Services.AddSingleton<ICalendarStore, InMemoryCalendarStore>();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

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

// Expose the agent over A2A
app.MapA2A(
    calendarAgent, 
    path: "/", 
    agentCard: calendarAgentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/"));

app.Run();