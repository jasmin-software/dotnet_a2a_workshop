We will create a simple application that is able to discover the capabilities of an agent that is already deployed online. The URL of the agent is https://netbc-weather-agent.azurewebsites.net/.

Let's first create a simple .NET console app with the following terminal window command:

# Setup

``` bash
dotnet new console -n '1. A2A Agent Discovery'
cd '1. A2A Agent Discovery'
dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1
```
# Program.cs
Open your application in VS code. Replace the content in `Program.cs` with the following C# code in sequence:

Connect to A2A Cloud Agent and get agent card:

``` C#
using System.Text.Json;
using A2A;
using Microsoft.Extensions.AI;

// Get agent card
A2ACardResolver agentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AgentCard agentCard = await agentCardResolver.GetAgentCardAsync();

JsonSerializerOptions s_indentedOptions = new(A2AJsonUtilities.DefaultOptions){ WriteIndented = true};
Console.WriteLine("\nAgent card details:");
Console.WriteLine(JsonSerializer.Serialize(agentCard, s_indentedOptions));
```

Create chat client
``` C#
// Create a chat client
A2AClient chatClient = new(new Uri(agentCard.Url));
var message = "What is the weather like in Vancouver?";
```

Send message and get the response
```C#
// Send message and get the response
var response = await chatClient.AsAIAgent().RunAsync(message);
Console.WriteLine($"Received complete response from agent: {response.Text}\n");
```

# Run app
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
Received complete response from agent: {"current":{"time":"2026-04-12T08:45","temperature":"10.5°C","feelsLike":"9.8°C","windSpeed":"4.3 km/h","condition":"Overcast"},"today":{"hourly":[{"time":"2026-04-12T00:00","temperature":"8.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T01:00","temperature":"8.5°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T02:00","temperature":"8.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T03:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T04:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T05:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T06:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T07:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T08:00","temperature":"9.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T09:00","temperature":"10.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T10:00","temperature":"10.7°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T11:00","temperature":"11.9°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T12:00","temperature":"12.9°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T13:00","temperature":"13.5°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T14:00","temperature":"13.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T15:00","temperature":"13.5°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T16:00","temperature":"13.7°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T17:00","temperature":"13.9°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T18:00","temperature":"13.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T19:00","temperature":"12.2°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T20:00","temperature":"11.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T21:00","temperature":"10.1°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T22:00","temperature":"9.5°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T23:00","temperature":"9.1°C","precipitation":"0 mm","condition":"Clear sky"}]}}
```

</details>

<br />
Optionally, you can also stream the response from the agent.  
<br><br>
Comment out this code.

```C#
// Send message and get the response
var response = await chatClient.AsAIAgent().RunAsync(message);
Console.WriteLine($"Received complete response from agent: {response.Text}\n");
```

Add the following code instead of the above commented out code.

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

# Run app
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
Streaming response from agent:
{"current":{"time":"2026-04-12T09:00","temperature":"10.4°C","feelsLike":"9.5°C","windSpeed":"5.6 km/h","condition":"Overcast"},"today":{"hourly":[{"time":"2026-04-12T00:00","temperature":"8.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T01:00","temperature":"8.5°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T02:00","temperature":"8.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T03:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T04:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T05:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T06:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T07:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T08:00","temperature":"9.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T09:00","temperature":"10.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T10:00","temperature":"10.7°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T11:00","temperature":"11.9°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T12:00","temperature":"12.9°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T13:00","temperature":"13.5°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T14:00","temperature":"13.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T15:00","temperature":"13.5°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T16:00","temperature":"13.7°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T17:00","temperature":"13.9°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T18:00","temperature":"13.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T19:00","temperature":"12.2°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T20:00","temperature":"11.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T21:00","temperature":"10.1°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T22:00","temperature":"9.5°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T23:00","temperature":"9.1°C","precipitation":"0 mm","condition":"Clear sky"}]}}
```

</details>
