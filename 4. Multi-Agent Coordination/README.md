This is code is can be built on top of Step 2. or for simplicity, we can also spin another project for this step.

``` C#
dotnet add package Azure.AI.OpenAI --version 2.9.0-beta.1
dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1
dotnet add package Microsoft.Agents.AI.OpenAI --version 1.0.0
dotnet add package Microsoft.Agents.AI.Workflows --version 1.1.0
```

``` C#
using System.ClientModel;
using System.Text.Json;
using A2A;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
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
```

``` C#
// Initialize chat client
var chatClient = new OpenAIClient(
    new ApiKeyCredential(token!),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint)
    })
    .GetChatClient(model).AsIChatClient();
```

``` C#
// Connect to the A2A weather agent
A2ACardResolver weatherAgentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
// A2ACardResolver weatherAgentCardResolver = new A2ACardResolver(new Uri("http://localhost:5000/"));
AIAgent weatherAgent = await weatherAgentCardResolver.GetAIAgentAsync();
```

``` C#
// Connect to the A2A calendar agent
A2ACardResolver calendarAgentCardResolver = new A2ACardResolver(new Uri("http://localhost:5098/"));
AIAgent calendarAgent = await calendarAgentCardResolver.GetAIAgentAsync();
```

``` C#
// Create a client agent to summarize the event created
var activitySummaryAgent = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You are a calendar event summary assistant.
        You are the final step in a workflow. Earlier agents selected a time based on weather and created the event.

        Your job:
        - State when the activity is scheduled (start and end time).
        - Briefly explain why that time was chosen based on the weather.
        - If relevant, suggest specific items to bring based on the forecast.

        Guidelines:
        - Be concise and natural (2–3 sentences).
        - Do not mention the workflow or other agents.
        - Do not include unnecessary details.
        - Only suggest items if needed.");

AIAgent workflowAgent = AgentWorkflowBuilder.BuildSequential(weatherAgent, calendarAgent, activitySummaryAgent).AsAIAgent();
```


// Send message to agent
``` C#
bool isDebug = true;
AgentSession session = await workflowAgent.CreateSessionAsync();
List<ChatMessage> messages = [];
Console.Write("\nEnter the outdoor activity you'd like to plan or :q to quit.\n");

try
{
    while (true)
    {
        // Get and validate user input
        Console.Write("\n> ");
        string? message = Console.ReadLine();
        string? lastAuthor = null;

        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("\nRequest cannot be empty.");
            continue;
        }
        if (message.ToLowerInvariant() is ":q" or "quit")
        {
            break;
        }

        messages.Add(new ChatMessage(ChatRole.User, message));

        // Stream and print the response
        await foreach (AgentResponseUpdate update in workflowAgent.RunStreamingAsync(messages, session))
        {
            foreach (AIContent content in update.Contents)
            {
                if (content is TextContent textContent)
                {
                    if (update.AuthorName != null)
                    {
                        if (lastAuthor != update.AuthorName)
                        {
                            lastAuthor = update.AuthorName;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n** {update.AuthorName} **");
                            Console.ResetColor();
                        }
                        if (update.AuthorName == activitySummaryAgent.Name)
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(textContent.Text);
                        }
                    }
                    else
                    {
                        if (isDebug && update.RawRepresentation is AgentMessage agentMessage)
                        {
                            if (update.Role == ChatRole.Assistant)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                if (update.AgentId == weatherAgent.Id)
                                {
                                    Console.WriteLine($"\n[A2A Agent: Weather Agent] \n{textContent.Text}");
                                }
                                else if (update.AgentId == calendarAgent.Id)
                                {
                                    Console.WriteLine($"\n[A2A Agent: Calendar Agent] \n{textContent.Text}");
                                }
                            }
                        }
                    }
                }
                else if (content is ErrorContent errorContent)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[Error: {update.RawRepresentation}]");
                }
            }
            Console.ResetColor();
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nAn error occurred: {ex.Message}");
}
```