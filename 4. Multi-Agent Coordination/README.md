# 4. Multi-Agent Coordination

## How do we connect the agents?
In this walkthrough, you'll develop a client application that talks to multiple agents (i.e., weather agent, calendar agent, summary agent). 

We would be able to ask the client to schedule an outdoor activity for us. It will:
1. Determine a time with good weather (using the **weather agent**) 
2. Figure out when we are free (using the **calendar agent**)
3. Provide a description of the activity scheduled (using the **summary agent**)

![workflow](workflow.png)

We will be using a sequential workflow since more than one agent is involved.

## Setup

Create a .NET web application with the following terminal window commands:

``` bash
dotnet new web -n '4. Multi-Agent Coordination'

cd '4. Multi-Agent Coordination'
dotnet new gitignore

dotnet add package Azure.AI.OpenAI --version 2.9.0-beta.1
dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1
dotnet add package Microsoft.Agents.AI.OpenAI --version 1.0.0
dotnet add package Microsoft.Agents.AI.Workflows --version 1.1.0
```

Replace `appsettings.Development.json` with this JSON code:

```json
{
    "GitHub": {
        "Token": "put-your-github-personal-access-token-here",
        "ApiEndpoint": "https://models.github.ai/inference",
        "Model": "openai/gpt-4o-mini"
    }
}
```

> [!NOTE]
>
> Replace `put-your-github-personal-access-token-here` with your GitHub Personal Access Token. 

Edit the `.gitignore` file and add to it `appsettings.Development.json` so that your secrets do not find their way into source control by mistake.

## Program.cs

Replace `Program.cs` with the following code in sequence.

### Read configuration settings
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

### Connect to A2A agents from previous steps
``` C#
// Connect to the A2A weather agent
A2ACardResolver weatherAgentCardResolver = new A2ACardResolver(new Uri("https://a2a-weather.azurewebsites.net/"));
AIAgent weatherAgent = await weatherAgentCardResolver.GetAIAgentAsync();

// Connect to the A2A calendar agent
A2ACardResolver calendarAgentCardResolver = new A2ACardResolver(new Uri("http://localhost:5098/"));
AIAgent calendarAgent = await calendarAgentCardResolver.GetAIAgentAsync();
```

### Create a summary agent and set up the workflow
``` C#
// Initialize chat client
var chatClient = new OpenAIClient(
    new ApiKeyCredential(token!),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint)
    })
    .GetChatClient(model).AsIChatClient();

// Create a client-side agent to summarize the event created
var summaryAgent = chatClient.AsAIAgent(
        name: "Summary Agent",
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

// Set up sequential workflow agent
AIAgent workflowAgent = AgentWorkflowBuilder.BuildSequential(
    weatherAgent, 
    calendarAgent,
    summaryAgent).AsAIAgent();
```

### Send message to agents

``` C#
// Send message to agents and stream response
bool isDebug = false;
AgentSession session = await workflowAgent.CreateSessionAsync();
List<ChatMessage> messages = [];
Console.Write("\nEnter the outdoor activity you'd like to plan or :q to quit.\n");

try {
    while (true) {
        // Get and validate user input
        Console.Write("\n> ");
        string? message = Console.ReadLine();
        string? lastAuthor = null;

        if (string.IsNullOrWhiteSpace(message)) {
            Console.WriteLine("\nRequest cannot be empty.");
            continue;
        }

        if (message.ToLowerInvariant() is ":q" or "quit") {
            break;
        }

        messages.Add(new ChatMessage(ChatRole.User, message));

        // Stream and print the response
        await foreach (AgentResponseUpdate update in workflowAgent.RunStreamingAsync(messages, session)) {
            foreach (AIContent content in update.Contents) {
                if (content is TextContent textContent) {
                    if (update.AuthorName != null) {
                        if (update.AuthorName == summaryAgent.Name) {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(textContent.Text);
                        }
                    } 
                }
            }
        }
    }
} 
catch (Exception ex) {
    Console.WriteLine($"\nAn error occurred: {ex.Message}");
}
```

## Run app

> [!IMPORTANT]
> Before running this app, ensure the calendar agent in Step 3 is running on `http://localhost:5098`
>
> You can do it by navigating to the `3. A2A Agent Implementation` folder in terminal and run the following:
> ```bash
> dotnet run --urls=http://localhost:5098
> ```

In a new terminal window, navigate to the `4. Multi-Agent Coordination` folder, run the following:
```bash
dotnet run
```

You'd be prompted with this:

```
Enter the outdoor activity you'd like to plan or :q to quit.
```

Go ahead and ask the agent to schedule a run, hike, or picnic, e.g.,

```
I want to hike along the Knox Mountain - Apex Trail in Kelowna, BC, Canada tomorrow.
```

<details>

<summary>Here's an example of the output</summary>

```
[Client Agent: Summary Agent]
Your hike along the Knox Mountain - Apex Trail is scheduled from 1:00 PM to 4:00 PM.

This time was chosen for the mild temperature of around 11-14°C and the partly cloudy conditions expected by the afternoon, making it comfortable for outdoor activity.

Bring layers and water, as temperatures may be cooler earlier on.
```
</details>

<details>

<summary>Debug logs</summary>
<br>

If you'd like to see debug logs, you can toggle the `isDebug` flag to true and replace the try-catch block with the following:

``` C#
try {
    while (true) {
        // Get and validate user input
        Console.Write("\n> ");
        string? message = Console.ReadLine();
        string? lastAuthor = null;

        if (string.IsNullOrWhiteSpace(message)) {
            Console.WriteLine("\nRequest cannot be empty.");
            continue;
        }

        if (message.ToLowerInvariant() is ":q" or "quit") {
            break;
        }

        messages.Add(new ChatMessage(ChatRole.User, message));

        // Stream and print the response
        await foreach (AgentResponseUpdate update in workflowAgent.RunStreamingAsync(messages, session)) {
            foreach (AIContent content in update.Contents) {
                if (content is TextContent textContent) {
                    if (update.AuthorName != null) {
                        if (isDebug && lastAuthor != update.AuthorName) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            lastAuthor = update.AuthorName;
                            Console.WriteLine($"\n[Client Agent: {update.AuthorName}]");
                        }
                        if (update.AuthorName == summaryAgent.Name) {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(textContent.Text);
                        }
                    } 
                    else {
                        if (isDebug && update.RawRepresentation is AgentMessage agentMessage) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            if (update.Role == ChatRole.Assistant) {
                                if (update.AgentId == weatherAgent.Id) {
                                    Console.WriteLine($"\n[A2A Agent: Weather Agent]");
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.WriteLine($"{textContent.Text}");
                                } 
                                else if (update.AgentId == calendarAgent.Id) {
                                    Console.WriteLine($"\n[A2A Agent: Calendar Agent]");
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.WriteLine($"{textContent.Text}");
                                }
                            }
                        }
                    }
                }
                else if (content is ErrorContent errorContent) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[Error: {update.RawRepresentation}]");
                }
            }
            Console.ResetColor();
        }
    }
}
catch (Exception ex) {
    Console.WriteLine($"\nAn error occurred: {ex.Message}");
}
```

 You'll see exactly what the weather and calendar agents are returning:
``` json
[A2A Agent: Weather Agent] 
{
    "current": {
        "time": "2026-04-21T09:00",
        "temperature": "10.4°C",
        "feelsLike": "9.5°C",
        "windSpeed": "5.6 km/h",
        "condition": "Overcast"
    },
    "today": {
        "hourly": [
            {
                "time": "2026-04-22T00:00",
                "temperature": "8.8°C",
                "precipitation": "0 mm",
                "condition": "Partly cloudy"
            },
            {
                "time": "2026-04-22T01:00",
                "temperature": "8.5°C",
                "precipitation": "0 mm",
                "condition": "Partly cloudy"
            }, ...
        ]
    }
}
```
```
[A2A Agent: Calendar Agent] 
It looks like you're planning a hike along the Knox Mountain - Apex Trail in Kelowna. 

The weather forecast for April 22 shows an overcast condition with temperatures ranging from 10.6°C at 10:00 AM increasing steadily to 14.1°C by 4:00 PM. 

It remains overcast most of the day with lighter conditions turning partly cloudy later in the afternoon.
```


</details>

## That's all! 
Now, you have the tools to build your own A2A agent 🙂‍
