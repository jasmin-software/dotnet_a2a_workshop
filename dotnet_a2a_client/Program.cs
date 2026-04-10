using A2A;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text.Json;

// A2A as agent
A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AgentCard agentCard = await agentCardResolver.GetAgentCardAsync();

JsonSerializerOptions s_indentedOptions = new(A2A.A2AJsonUtilities.DefaultOptions){ WriteIndented = true};
// Console.WriteLine("\nAgent card details:");
// Console.WriteLine(JsonSerializer.Serialize(agentCard, s_indentedOptions));


A2AClient a2aChatClient = new(new Uri(agentCard.Url));

// Send the message and get the response
// Console.WriteLine("\nNon-Streaming Message Communication");
var weatherAgent = a2aChatClient.AsAIAgent(name: "WeatherAgent");
// var response = await a2aChatClient.AsAIAgent().RunAsync("What is the weather like in Vancouver?");
// Console.WriteLine($" Received complete response from agent: {response.Text}");

// var streamingResponse = a2aChatClient.AsAIAgent().RunStreamingAsync("What is the weather like in Vancouver?");
// Console.WriteLine("\nStreaming Message Communication"); 
// await foreach (var update in streamingResponse)
// {
//     foreach (var content in update.Contents)
//     {
//         if (content is TextContent textContent)
//         {
//             Console.Write(textContent.Text);
//         }
//     }
// }

// Console.WriteLine("\n\n");

// A2A as tool ========================================================
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

// var asToolResponse = await agent.RunAsync("What is the weather like in Vancouver?");
// Console.WriteLine($"\n\n{asToolResponse.Text}");

// Create their server agent // DONE 


A2ACardResolver agentCardResolver1 = new A2ACardResolver(new Uri("http://localhost:5098/"));
AgentCard agentCard2 = await agentCardResolver1.GetAgentCardAsync();

A2AClient a2aChatClient2 = new(new Uri(agentCard2.Url));

// Send the message and get the response
var calendarAgent = a2aChatClient2.AsAIAgent(name: "CalendarAgent");

// consolidation agent
var consolidateAgent = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You use the weather data and existing calendar data to schedule outdoor activities for user.
        Activity should only be scheduled when the weather is good, and there's an open slot in the user's calendar.
        You are concise with your answers. You do not ask the user for approval. You make the decisions and create the calendar event using the calendar agent tool.",
        tools: [calendarAgent.AsAIFunction()]);


// Call the server agent here and put into a workflow. // TODO



AIAgent workflowAgent = AgentWorkflowBuilder.BuildSequential(weatherAgent, calendarAgent, consolidateAgent).AsAIAgent();

// Console.WriteLine(await workflowAgent.RunAsync("I want to do a run today or tomorrow, find a time for me."));


string? lastAuthor = null;
await foreach (var update in workflowAgent.RunStreamingAsync("I want to do a 20km run today or tomorrow, when should I do it?"))
{
    // Skip WorkflowEvent-only updates
    if ((update.Contents == null || update.Contents.Count == 0) && update.RawRepresentation is WorkflowEvent)
    {
        continue;
    }

    if (lastAuthor != update.AuthorName)
    {
        lastAuthor = update.AuthorName;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n\n** {update.AuthorName} **");
        Console.ResetColor();
    }

    Console.Write(update.Text);
}