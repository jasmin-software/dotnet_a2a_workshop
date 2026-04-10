using System.ClientModel;
using System.Text.Json;
using A2A;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// Set up chat client configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .Build();

string? token = config["GitHub:Token"];
string? endpoint = config["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string? model = config["GitHub:Model"] ?? "openai/gpt-4o-mini";

// Initialize chat client
var chatClient = new OpenAIClient(
    new ApiKeyCredential(token!),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint)
    })
    .GetChatClient(model).AsIChatClient();

// Connect to the A2A weather agent
A2ACardResolver weatherAgentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AIAgent weatherAgent = await weatherAgentCardResolver.GetAIAgentAsync();

// Create a client agent that uses the weather agent as a tool 
var agent = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You are a personal weather assistant who speaks concisely. 
        When asked for the weather, summarize the current weather and the forecast for the next few hours.
        Highlight any significant changes in the weather.
        When asked for the weather in the future, summarize the forecast of that day.", 
        tools: [weatherAgent.AsAIFunction()]);

// Send message to agent
var response = agent.RunStreamingAsync("What is the weather like in Vancouver?");
await foreach (var update in response)
{
    foreach (var content in update.Contents)
    {
        if (content is TextContent textContent)
        {
            Console.Write(textContent.Text);
        }
        else if (content is FunctionCallContent functionCallContent)
        {                    
            var argsJson = JsonSerializer.Serialize(
                functionCallContent.Arguments,
                new JsonSerializerOptions { WriteIndented = true }
            );
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n[Function Call: {functionCallContent.Name}]\nArguments:\n{argsJson}");
        }
        else if (content is FunctionResultContent functionResultContent)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n[Function Result: {functionResultContent.Result}]");
        }

    }
}