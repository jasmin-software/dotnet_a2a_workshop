using OpenAI;
using Microsoft.Extensions.AI;
using System.ClientModel;
using A2A;
using A2A.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

string githubToken = builder.Configuration["GitHub:Token"] ?? throw new InvalidOperationException("GitHub:Token is not set.");
string endpoint = builder.Configuration["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string model = builder.Configuration["GitHub:Model"] ?? "openai/gpt-4o-mini";

// Create AI agent
var chatClient = new OpenAIClient(
    new ApiKeyCredential(githubToken),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint),
    })
    .GetChatClient(model).AsIChatClient();

var agent = chatClient.AsAIAgent();
builder.Services.AddSingleton(chatClient);

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

AgentCard weatherAgentCard = new AgentCard
{
    Name = "Weather Agent",
    Description = "This is a weather agent.",
    Version = "1.0",
    Skills = [
        new AgentSkill {
            Id = "get_weather",
            Name = "Weather Agent",
            Description = "An agent that provides weather information.",
            Tags = ["weather", "forecast"],
            Examples = ["What is the weather like in Vancouver today?"]
        }
    ]
};

// Expose the agent via A2A protocol.
app.MapA2A(
    agent,
    path: "/",
    agentCard: weatherAgentCard,
    taskManager => app.MapWellKnownAgentCard(taskManager, "/")
    );

app.Run();