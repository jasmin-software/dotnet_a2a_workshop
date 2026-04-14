This walkthrough involves developing a client application that sequentially communcates with multiple agents in order to solve this challenge:

```
I need to schedule an activity as soon as possible. The activity requires good weather.

Schedule the activity for me after determining the most suitable time depending weather conditions.
```

This means two agents will get engaged to fulfill the task:
1. The weather agent
2. The calendar agent
3. The activity summary agent

Workflow concepts are introduced in the walkthrough since more than one agent is involved in the solution. In this scenario, we will setup a sequential workflow.

### Setup

Create a simple .NET web application with the following terminal window commands:

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

### Set up chat client configuration

Delete any existing code in `Program.cs`.

Add the following code in sequence.

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

### Initialize chat client

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

### Connect to the A2A weather agent

``` C#
// Connect to the A2A weather agent
A2ACardResolver weatherAgentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AIAgent weatherAgent = await weatherAgentCardResolver.GetAIAgentAsync();
```

### Connect to the A2A calendar agent

We will use the calendar agent that we built in the third workshop `3. A2A Agent Implementation`. 

``` C#
// Connect to the A2A calendar agent
A2ACardResolver calendarAgentCardResolver = new A2ACardResolver(new Uri("http://localhost:5098/"));
AIAgent calendarAgent = await calendarAgentCardResolver.GetAIAgentAsync();
```

### Create a client agent to summarize the event created

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

// setup a sequential workflow agent that makes sequential
// requests to weather, calendar, and summary agents 
AIAgent workflowAgent = AgentWorkflowBuilder.BuildSequential(
    weatherAgent, calendarAgent, activitySummaryAgent
).AsAIAgent();
```

### Send message to agents and stream response

``` C#
// Send message to agents and stream response
bool isDebug = true; // Toggle this to print messages from A2A agents
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
                        if (lastAuthor != update.AuthorName) {
                            lastAuthor = update.AuthorName;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n[{update.AuthorName}]");
                            Console.ResetColor();
                        }

                        if (update.AuthorName == activitySummaryAgent.Name) {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write(textContent.Text);
                        }
                    } else {
                        if (isDebug && update.RawRepresentation is AgentMessage agentMessage) {
                            if (update.Role == ChatRole.Assistant) {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                if (update.AgentId == weatherAgent.Id) {
                                    Console.WriteLine($"\n[A2A Agent: Weather Agent] \n{textContent.Text}");
                                } else if (update.AgentId == calendarAgent.Id) {
                                    Console.WriteLine($"\n[A2A Agent: Calendar Agent] \n{textContent.Text}");
                                }
                            }
                        }
                    }
                } else if (content is ErrorContent errorContent) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[Error: {update.RawRepresentation}]");
                }
            }
            Console.ResetColor();
        }
    }
} catch (Exception ex) {
    Console.WriteLine($"\nAn error occurred: {ex.Message}");
}
```

### Run solution

In the terminal window of the third walkthrough `3. A2A Agent Implementation`, run the following command so that the web app listens on `port 5098`:

```bash
dotnet run --urls=http://localhost:5098
```

In another terminal window of this walkthrough `4. Multi-Agent Coordination`, run the following command:

```bash
dotnet run
```

You are prompted with this:

```
Enter the outdoor activity you'd like to plan or :q to quit.
```

I entered the following:

```
I want to hike along the Knox Mountain - Apex Trail in Kelowna, BC, Canada.
```

This is the response I got from the workflow agent:

```
[A2A Agent: Weather Agent] 
{"current":{"time":"2026-04-13T10:15","temperature":"11.1°C","feelsLike":"10.4°C","windSpeed":"3.2 km/h","condition":"Overcast"},"today":{"hourly":[{"time":"2026-04-13T00:00","temperature":"8.9°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-13T01:00","temperature":"8.7°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-13T02:00","temperature":"7.9°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-13T03:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T04:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T05:00","temperature":"8.8°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T06:00","temperature":"9°C","precipitation":"0.1 mm","condition":"Drizzle"},{"time":"2026-04-13T07:00","temperature":"8.9°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T08:00","temperature":"9.1°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T09:00","temperature":"9.7°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T10:00","temperature":"10.6°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T11:00","temperature":"12.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T12:00","temperature":"11.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T13:00","temperature":"11.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T14:00","temperature":"11.9°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T15:00","temperature":"13.6°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T16:00","temperature":"14.1°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-13T17:00","temperature":"13.8°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T18:00","temperature":"13°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T19:00","temperature":"11.3°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T20:00","temperature":"9.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-13T21:00","temperature":"9.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-13T22:00","temperature":"8.7°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-13T23:00","temperature":"7.8°C","precipitation":"0 mm","condition":"Clear sky"}]}}

[A2A Agent: Calendar Agent] 
It looks like you're planning a hike along the Knox Mountain - Apex Trail in Kelowna. The weather forecast for April 13 shows an overcast condition with temperatures ranging from 10.6°C at 10:00 AM increasing steadily to 14.1°C by 4:00 PM. It remains overcast most of the day with lighter conditions turning partly cloudy later in the afternoon.

Would you like me to create an event for this hike? If yes, please provide the start and end times.

[Assistant]
Your hike along the Knox Mountain - Apex Trail is scheduled from 1:00 PM to 4:00 PM. This time was chosen for the mild temperature of around 11-14°C and the partly cloudy conditions expected by the afternoon, making it comfortable for outdoor activity. Bring layers and water, as temperatures may be cooler earlier on.
```
