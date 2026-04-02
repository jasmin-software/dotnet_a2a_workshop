using A2A;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text.Json;
using A2AJsonUtilities = A2A.A2AJsonUtilities;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

string? token = config["GitHub:Token"];
string? endpoint = config["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string? model = config["GitHub:Model"] ?? "openai/gpt-4o-mini";

A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AgentCard agentCard = await agentCardResolver.GetAgentCardAsync(); // like a business card 

Console.WriteLine("\nAgent card details:");
JsonSerializerOptions s_indentedOptions = new(A2AJsonUtilities.DefaultOptions)
    {
        WriteIndented = true
    };
Console.WriteLine(JsonSerializer.Serialize(agentCard, s_indentedOptions)); // THIS IS GOOD // KEEP


A2AClient a2aChatClient = new(new Uri(agentCard.Url));


// Send the message and get the response
Console.WriteLine("\nNon-Streaming Message Communication");
var directSend = await a2aChatClient.AsAIAgent().RunAsync("What is the weather like in Vancouver?");
Console.WriteLine($" Received complete response from agent: {directSend.Text}");

var directStreamingSend = a2aChatClient.AsAIAgent().RunStreamingAsync("What is the weather like in Vancouver?");
Console.WriteLine("\nStreaming Message Communication"); 
await foreach (var update in directStreamingSend)
{
    foreach (var content in update.Contents)
    {
        if (content is TextContent textContent)
        {
            Console.Write(textContent.Text);
        }
    }
}


// A2A as tool
AIAgent remoteAgent = await agentCardResolver.GetAIAgentAsync();

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

var asTool = await agent.RunAsync("What is the weather like in Vancouver?");
Console.WriteLine($"\n\n{asTool.Text}");


// List<ChatMessage> messages = [];
// AgentSession session = await agent.CreateSessionAsync();
// Console.Write("\nEnter your message or :q to quit.\n");
// string prompt = "\n> ";
// try
// {
//     while (true)
//     {
//         // Get and validate user input
//         Console.Write(prompt);
//         string? message = Console.ReadLine();

//         if (string.IsNullOrWhiteSpace(message))
//         {
//             Console.WriteLine("Request cannot be empty.");
//             continue;
//         }
//         if (message.ToLowerInvariant() is ":q" or "quit")
//         {
//             break;
//         }

//         messages.Add(new ChatMessage(ChatRole.User, message));

//         // Stream and print the response
//         await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(messages, session))
//         {
//             foreach (AIContent content in update.Contents)
//             {
//                 if (content is TextContent textContent)
//                 {
//                     Console.Write(textContent.Text);
//                 }
//                 // else if (content is FunctionCallContent functionCallContent)
//                 // {                    
//                 //     var argsJson = JsonSerializer.Serialize(
//                 //         functionCallContent.Arguments,
//                 //         new JsonSerializerOptions { WriteIndented = true }
//                 //     );
//                 //     Console.ForegroundColor = ConsoleColor.DarkGray;
//                 //     Console.WriteLine($"\n[Function Call: {functionCallContent.Name}]\nArguments:\n{argsJson}");
//                 //     Console.ResetColor();
//                 // }
//                 // else if (content is FunctionResultContent functionResultContent)
//                 // {
//                 //     Console.ForegroundColor = ConsoleColor.DarkGray;
//                 //     Console.WriteLine($"\n[Function Result: {functionResultContent.Result}]");
//                 //     Console.ResetColor();
//                 // }
//             }
//         }
//     }
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"\nAn error occurred: {ex.Message}");
// }
Console.WriteLine("\n\nDemo complete. Press any key to exit.");