
``` bash
dotnet new web -n '2. Client Agent A2A Integration'

cd '2. Client Agent A2A Integration'
dotnet new gitignore

dotnet add package Azure.AI.OpenAI --version 2.9.0-beta.1
dotnet add package Microsoft.Agents.AI.A2A --version 1.0.0-preview.260402.1
dotnet add package Microsoft.Agents.AI.OpenAI --version 1.0.0
```

// Set up chat client configuration
``` C#
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .Build();

string? token = config["GitHub:Token"];
string? endpoint = config["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string? model = config["GitHub:Model"] ?? "openai/gpt-4o-mini";
```

Initialize chat client
``` C#
var chatClient = new OpenAIClient(
    new ApiKeyCredential(token!),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint)
    })
    .GetChatClient(model).AsIChatClient();
```

Connect to the A2A weather agent
``` C#
A2ACardResolver weatherAgentCardResolver = new A2ACardResolver(new Uri("https://netbc-weather-agent.azurewebsites.net/"));
AIAgent weatherAgent = await weatherAgentCardResolver.GetAIAgentAsync();
```

Create a client agent that uses the weather agent as a tool 
``` C#
var agent = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You are a personal weather assistant. 
        You summarize the current weather and the forecast for the next few hours.
        Highlight any significant changes in the weather.
        ", 
        tools: [weatherAgent.AsAIFunction()]);
```

Send message to agent
``` C#
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