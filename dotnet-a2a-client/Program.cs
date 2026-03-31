using A2A;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text.Json;



string githubToken = builder.Configuration["GitHub:Token"] ?? throw new InvalidOperationException("GitHub:Token is not set.");
string endpoint = builder.Configuration["GitHub:ApiEndpoint"] ?? "https://models.github.ai/inference";
string model = builder.Configuration["GitHub:Model"] ?? "openai/gpt-4o-mini";

A2ACardResolver cardResolver = new A2ACardResolver(new Uri("http://localhost:5024/"));
AIAgent remoteAgent = await cardResolver.GetAIAgentAsync();

// Create AI agent
var chatClient = new OpenAIClient(
    new ApiKeyCredential(githubToken),
    new OpenAIClientOptions()
    {
        Endpoint = new Uri(endpoint)
    })
    .GetChatClient(model).AsIChatClient();

var agent = chatClient.AsAIAgent(
        name: "Assistant",
        instructions: @"You are a personal assistant", 
        tools: [remoteAgent.AsAIFunction()]);

List<ChatMessage> messages = [];
AgentSession session = await agent.CreateSessionAsync();
Console.Write("\nEnter your message or :q to quit.\n");
string prompt = "\n> ";
try
{
    while (true)
    {
        // Get and validate user input
        Console.Write(prompt);
        string? message = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("Request cannot be empty.");
            continue;
        }
        if (message.ToLowerInvariant() is ":q" or "quit")
        {
            break;
        }

        messages.Add(new ChatMessage(ChatRole.User, message));

        // Stream and print the response
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(messages, session))
        {
            foreach (AIContent content in update.Contents)
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
                    Console.ResetColor();
                }
                else if (content is FunctionResultContent functionResultContent)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"\n[Function Result: {functionResultContent.Result}]");
                    Console.ResetColor();
                }
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nAn error occurred: {ex.Message}");
}