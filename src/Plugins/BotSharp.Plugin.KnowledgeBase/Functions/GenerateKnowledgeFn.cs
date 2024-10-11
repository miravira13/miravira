using BotSharp.Abstraction.Templating;
using BotSharp.Core.Infrastructures;

namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class GenerateKnowledgeFn : IFunctionCallback
{
    public string Name => "generate_knowledge";

    public string Indication => "generating knowledge";

    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;

    public GenerateKnowledgeFn(IServiceProvider services, KnowledgeBaseSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExtractedKnowledge>(message.FunctionArgs ?? "{}");
        var agentService = _services.GetRequiredService<IAgentService>();
        var llmAgent = await agentService.GetAgent(BuiltInAgentId.Planner);
        var generateKnowledgePrompt = await GetGenerateKnowledgePrompt(args.Question, args.Answer);
        var agent = new Agent
        {
            Id = message.CurrentAgentId ?? string.Empty,
            Name = "sqlDriver_DictionarySearch",
            Instruction = generateKnowledgePrompt,
            LlmConfig = llmAgent.LlmConfig
        };
        var response = await GetAiResponse(agent);
        message.Data = response.Content.JsonArrayContent<ExtractedKnowledge>();
        message.Content = response.Content;
        return true;
    }

    private async Task<string> GetGenerateKnowledgePrompt(string userQuestions, string sqlAnswer)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var render = _services.GetRequiredService<ITemplateRender>();

        var agent = await agentService.GetAgent(BuiltInAgentId.Learner);
        var template = agent.Templates.FirstOrDefault(x => x.Name == "knowledge.generation")?.Content ?? string.Empty;

        return render.Render(template, new Dictionary<string, object>
        {
            { "user_questions", userQuestions },
            { "sql_answer", sqlAnswer },
        });
    }
    private async Task<RoleDialogModel> GetAiResponse(Agent agent)
    {
        var text = "Generate question and answer pair";
        var message = new RoleDialogModel(AgentRole.User, text);

        var completion = CompletionProvider.GetChatCompletion(_services,
            provider: agent.LlmConfig.Provider,
            model: agent.LlmConfig.Model);

        return await completion.GetChatCompletions(agent, new List<RoleDialogModel> { message });
    }
}
