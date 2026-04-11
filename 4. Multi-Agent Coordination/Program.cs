using System.ClientModel;
using System.Text.Json;
using A2A;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

/*
dotnet add package Azure.AI.OpenAI --version 2.9.0-beta.1
dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1
dotnet add package Microsoft.Agents.AI.OpenAI --version 1.0.0
dotnet add package Microsoft.Agents.AI.Workflows --version 1.1.0
*/

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

// Connect to the A2A calendar agent
A2ACardResolver calendarAgentCardResolver = new A2ACardResolver(new Uri("http://localhost:5098/"));
AIAgent calendarAgent = await calendarAgentCardResolver.GetAIAgentAsync();

// Create a client agent that uses the weather agent as a tool 
var plannerAgent = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You are a outdoor activity planner who speaks concisely.
        Given user's schedule and the weather forecast, you suggest the best time for outdoor activities.
        You can only suggest a time the weather is good, and there should be nothing scheduled on the calendar at the same time.
        You make an estimate on how long each calendar even will last based on the activity.
        Use the tool to create the outdoor activity event in the user's calendar.", 
        tools: [calendarAgent.AsAIFunction()]);

// var availabilityAgent = AgentWorkflowBuilder.BuildConcurrent([weatherAgent, calendarAgent]);
// AIAgent workflowAgent = AgentWorkflowBuilder.BuildSequential(availabilityAgent, plannerAgent).AsAIAgent();
var workflowAgent = AgentWorkflowBuilder.BuildSequential(weatherAgent, calendarAgent, plannerAgent);

// var x = await InProcessExecution.RunStreamingAsync(workflowAgent, new List<ChatMessage> { new(ChatRole.User, "best time to run tomorrow?")});
// await x.TrySendMessageAsync(new TurnToken(emitEvents: true));
// var result = new List<AgentResponseUpdate>();
// await foreach (WorkflowEvent evt in x.WatchStreamAsync().ConfigureAwait(false))
// {
//     if (evt is WorkflowOutputEvent completed)
//     {
//         if (completed.Data is AgentResponseUpdate update)
//         {
//             result.Add(update);
//             break;

//         }
//     }
// }

// foreach (var msg in result.Where(x => x.Role !=ChatRole.User))
// {
//     Console.WriteLine("------------------------------");
//     Console.WriteLine($"{msg.Text}");
//     break;
// }
// // Send message to agent
// AgentSession session = await workflowAgent.CreateSessionAsync();
// List<ChatMessage> messages = [];

// Console.Write("\nEnter the outdoor activity you'd like to plan or :q to quit.\n");

// try
// {
//     while (true)
//     {
//         // Get and validate user input
//         Console.Write("\n> ");
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
//         await foreach (AgentResponseUpdate update in workflowAgent.RunStreamingAsync(messages, session))
//         {
//             Console.ForegroundColor = ConsoleColor.Blue;
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
//                 // }
//                 // else if (content is FunctionResultContent functionResultContent)
//                 // {
//                 //     Console.ForegroundColor = ConsoleColor.DarkGray;
//                 //     Console.WriteLine($"\n[Function Result: {functionResultContent.Result}]");
//                 // }
//             }
//             Console.ResetColor();
//         }
//     }
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"\nAn error occurred: {ex.Message}");
// }