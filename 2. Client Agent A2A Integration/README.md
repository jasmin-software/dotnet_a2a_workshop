In the previous example, we were able to discover the capabilities of a weather agent at https://netbc-weather-agent.azurewebsites.net/. In this example, we will interact with that agent by asking it for weather conditions in a particular city.

### Setup

Create a simple .NET web application with the following terminal window commands:

``` bash
dotnet new web -n '2. Client Agent A2A Integration'

cd '2. Client Agent A2A Integration'
dotnet new gitignore

dotnet add package Azure.AI.OpenAI --version 2.9.0-beta.1
dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1
dotnet add package Microsoft.Agents.AI.OpenAI --version 1.0.0
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
using Microsoft.Extensions.AI;
using OpenAI;

// Read configuration settings
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

### Create a client agent that uses the weather agent as a tool

``` C#
// Create a client agent that uses the weather agent as a tool
var agent = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You are a personal weather assistant. 
        You summarize the current weather and the forecast for the next few hours.
        Highlight any significant changes in the weather.
        ", 
        tools: [weatherAgent.AsAIFunction()]);
```

### Send message to agent and stream response

``` C#
// Send message to agent and stream response
var response = agent.RunStreamingAsync("What is the weather like in Vancouver?");
await foreach (var update in response)
{
    foreach (var content in update.Contents)
    {
        if (content is TextContent textContent)
        {
            Console.Write(textContent.Text);
        }
        else if (content is FunctionCallContent functionCallContent)
        {                    
            var argsJson = JsonSerializer.Serialize(
                functionCallContent.Arguments,
                new JsonSerializerOptions { WriteIndented = true }
            );
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n[Function Call: {functionCallContent.Name}]\nArguments:\n{argsJson}");
        }
        else if (content is FunctionResultContent functionResultContent)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n[Function Result: {functionResultContent.Result}]");
        }

    }
}
```

### Run app

In the terminal window:

```bash
dotnet run
```
<details>

<summary>Here's an example of the interaction:</summary>

```json
[Function Call: Weather_Agent]
Arguments:
{
  "query": "current weather in Vancouver"
}

[Function Result: {"current":{"time":"2026-04-12T08:15","temperature":"10.4°C","feelsLike":"9.4°C","windSpeed":"6.2 km/h","condition":"Overcast"},"today":{"hourly":[{"time":"2026-04-12T00:00","temperature":"8.5°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T01:00","temperature":"8.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T02:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T03:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T04:00","temperature":"8.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T05:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T06:00","temperature":"8.2°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T07:00","temperature":"9.8°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T08:00","temperature":"10.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T09:00","temperature":"10.7°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T10:00","temperature":"11.9°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T11:00","temperature":"12.9°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T12:00","temperature":"13.5°C","precipitation":"0 mm","condition":"Partly cloudy"},{"time":"2026-04-12T13:00","temperature":"13.4°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T14:00","temperature":"13.5°C","precipitation":"0 mm","condition":"Overcast"},{"time":"2026-04-12T15:00","temperature":"13.7°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T16:00","temperature":"13.9°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T17:00","temperature":"13.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T18:00","temperature":"12.2°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T19:00","temperature":"11.1°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T20:00","temperature":"10.1°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T21:00","temperature":"9.5°C","precipitation":"0 mm","condition":"Mainly clear"},{"time":"2026-04-12T22:00","temperature":"9.1°C","precipitation":"0 mm","condition":"Clear sky"},{"time":"2026-04-12T23:00","temperature":"9.4°C","precipitation":"0 mm","condition":"Overcast"}]}}]
Currently in Vancouver, it is 10.4°C and feels like 9.4°C. The sky is overcast with a light wind blowing at 6.2 km/h.

For the next few hours:
- At 9:00 AM, the temperature will rise slightly to 10.7°C with partly cloudy skies.
- By 10:00 AM, it will further increase to 11.9°C, still with partly cloudy conditions.
- At 11:00 AM, the temperature will reach 12.9°C and the sky will be mainly clear.

No precipitation is expected, and the weather will gradually become sunnier and warmer over the next few hours.
```

</details>
