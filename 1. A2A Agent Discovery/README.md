# 1. A2A Agent Discovery
## How do we know what an agent can do?
![A2A discovery](discovery.png)

We will create a simple application to discover the capabilities of a weather agent. The URL of the agent is [https://a2a-weather.azurewebsites.net/](https://a2a-weather.azurewebsites.net/swagger).

Let's first create a simple .NET console app with the following terminal window command:

## Setup

``` bash
dotnet new console -n '1. A2A Agent Discovery'
cd '1. A2A Agent Discovery'
dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1
```
## Program.cs
Open your application in VS code. Replace the content in `Program.cs` with the following C# code in sequence:

### Connect to the web A2A Agent and get agent card:

``` C#
using System.Text.Json;
using A2A;
using Microsoft.Extensions.AI;

// Get agent card
A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("https://a2a-weather.azurewebsites.net/"));
AgentCard agentCard = await agentCardResolver.GetAgentCardAsync();

JsonSerializerOptions s_indentedOptions = new(A2AJsonUtilities.DefaultOptions){ WriteIndented = true};
Console.WriteLine("\nAgent card details:");
Console.WriteLine(JsonSerializer.Serialize(agentCard, s_indentedOptions));
```

### Create chat client and send message to the agent
``` C#
// Create a chat client
A2AClient chatClient = new(new Uri(agentCard.Url));
var message = "What is the weather like in Vancouver?";

// Send message and get the response
var response = await chatClient.AsAIAgent().RunAsync(message);
Console.WriteLine($"\nReceived complete response from agent: {response.Text}\n");
```

## Run app
In terminal window:

```bash
dotnet run
```

<details>

<summary>Here's an example of the output</summary>

```json
Agent card details:
{
  "name": "Weather Agent",
  "description": "This is a weather agent.",
  "url": "https://netbc-weather-agent.azurewebsites.net",
  "version": "1.0",
  "protocolVersion": "0.3.0",
  "capabilities": {
    "streaming": false,
    "pushNotifications": false,
    "stateTransitionHistory": false,
    "extensions": []
  },
  "defaultInputModes": [
    "text"
  ],
  "defaultOutputModes": [
    "text"
  ],
  "skills": [
    {
      "id": "get_weather",
      "name": "Weather Agent",
      "description": "An agent that provides weather information.",
      "tags": [
        "weather",
        "forecast"
      ],
      "examples": [
        "What is the weather like in Vancouver today?"
      ]
    }
  ],
  "supportsAuthenticatedExtendedCard": false,
  "additionalInterfaces": [],
  "preferredTransport": "JSONRPC"
}

Received complete response from agent:
{
    "current": {
        "time": "2026-04-12T09:00",
        "temperature": "10.4°C",
        "feelsLike": "9.5°C",
        "windSpeed": "5.6 km/h",
        "condition": "Overcast"
    },
    "today": {
        "hourly": [
            {
                "time": "2026-04-12T00:00",
                "temperature": "8.8°C",
                "precipitation": "0 mm",
                "condition": "Partly cloudy"
            },
            {
                "time": "2026-04-12T01:00",
                "temperature": "8.5°C",
                "precipitation": "0 mm",
                "condition": "Partly cloudy"
            }
        ]
    }
}
```

</details>

<br />
Optionally, you can also stream the response from the agent.  
<br><br>
Comment out this code.

```C#
// Send message and get the response
// var response = await chatClient.AsAIAgent().RunAsync(message);
// Console.WriteLine($"Received complete response from agent: {response.Text}\n");
```

Add the following code to get the response faster since it is streamed in real-time.

``` C#
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
```

## Next: [2. Client Agent A2A Integration](https://github.com/jasmin-software/dotnet_a2a_workshop/blob/master/2.%20Client%20Agent%20A2A%20Integration/README.md)