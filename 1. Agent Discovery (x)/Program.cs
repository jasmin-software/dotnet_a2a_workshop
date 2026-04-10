// A2A as agent
using System.Text.Json;
using A2A;
using Microsoft.Extensions.AI;

// dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1

// Get agent card
A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AgentCard agentCard = await agentCardResolver.GetAgentCardAsync();

JsonSerializerOptions s_indentedOptions = new(A2AJsonUtilities.DefaultOptions){ WriteIndented = true};
Console.WriteLine("\nAgent card details:");
Console.WriteLine(JsonSerializer.Serialize(agentCard, s_indentedOptions));

// Create a chat client
A2AClient chatClient = new(new Uri(agentCard.Url));
var message = "What is the weather like in Vancouver?";

// Send message and get the response
var response = await chatClient.AsAIAgent().RunAsync(message);
Console.WriteLine($"Received complete response from agent: {response.Text}\n");

// Send message and stream the response
var streamingResponse = chatClient.AsAIAgent().RunStreamingAsync(message);
Console.WriteLine("Streaming response from agent:");
await foreach (var update in streamingResponse)
{
    foreach (var content in update.Contents)
    {
        if (content is TextContent textContent)
        {
            Console.Write(textContent.Text);
        }
    }
}