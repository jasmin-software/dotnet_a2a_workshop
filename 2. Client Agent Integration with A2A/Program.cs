using System.ClientModel;
using A2A;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// dotnet add package Azure.AI.OpenAI --version 2.9.0-beta.1
// dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1
// dotnet add package Microsoft.Agents.AI.OpenAI --version 1.0.0
// dotnet add package Microsoft.Extensions.AI --version 10.4.1

A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AgentCard agentCard = await agentCardResolver.GetAgentCardAsync(); // TODO: Host the calendar agent and call it here as a tool


AIAgent remoteAgent = await agentCardResolver.GetAIAgentAsync();

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

string? token = config["GitHub:Token"];
string? endpoint = config["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string? model = config["GitHub:Model"] ?? "openai/gpt-4o-mini";

var chatClient = new OpenAIClient(
    new ApiKeyCredential(token!),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint)
    })
    .GetChatClient(model).AsIChatClient();

var agent = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You are a personal assistant. You are concise with your answers.", 
        tools: [remoteAgent.AsAIFunction()]);

var asToolResponse = await agent.RunAsync("What is the weather like in Vancouver?");
Console.WriteLine($"\n\n{asToolResponse.Text}");