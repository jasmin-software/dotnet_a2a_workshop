using A2A;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Identity;
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

A2ACardResolver cardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AgentCard agentCard = await cardResolver.GetAgentCardAsync();

Console.WriteLine("\nAgent card details:");
JsonSerializerOptions s_indentedOptions = new(A2AJsonUtilities.DefaultOptions)
    {
        WriteIndented = true
    };
// Console.WriteLine(JsonSerializer.Serialize(agentCard, s_indentedOptions)); // THIS IS GOOD // KEEP


// A2AClient agentClient = new(new Uri(agentCard.SupportedInterfaces[0].Url));
A2AClient a2aChatClient = new(new Uri(agentCard.Url));
AgentMessage userMessage1 = new()
        {
            Role = MessageRole.User,
            MessageId = Guid.NewGuid().ToString(),
            Parts = new Part[]
            {
                new TextPart { Text = "What is the weather like in Vancouver?" }
            }.ToList()
        };

// 4. Send the message using non-streaming API
Console.WriteLine("\nNon-Streaming Message Communication");
Console.WriteLine($" Sending message via non-streaming API: {userMessage1.Parts[0].AsTextPart().Text}");

// Send the message and get the response
var directSend = await a2aChatClient.AsAIAgent().RunAsync("What is the weather like in Vancouver?");



// var lll = response.Message!;
// Display the response
// Console.WriteLine($" Received complete response from agent: {response.Parts[0].Text}");

// 5. Send the message using streaming API
// await SendStreamingMessageAsync(agentClient, userMessage1);



AIAgent remoteAgent = await cardResolver.GetAIAgentAsync();

// Create AI agent
var chatClient = new OpenAIClient(
    new ApiKeyCredential(token!),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint)
    })
    .GetChatClient(model).AsIChatClient();

var agent2 = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You are a personal assistant", 
        tools: [remoteAgent.AsAIFunction()]);

var asTool = await agent2.RunAsync("What is the weather like in Vancouver?");


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