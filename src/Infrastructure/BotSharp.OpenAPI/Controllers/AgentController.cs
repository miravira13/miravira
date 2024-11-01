using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IUserIdentity _user;
    private readonly IServiceProvider _services;

    public AgentController(IAgentService agentService, IUserIdentity user, IServiceProvider services)
    {
        _agentService = agentService;
        _user = user;
        _services = services;
    }

    [HttpGet("/agent/settings")]
    public AgentSettings GetSettings()
    {
        var settings = _services.GetRequiredService<AgentSettings>();
        return settings;
    }

    [HttpGet("/agent/{id}")]
    public async Task<AgentViewModel?> GetAgent([FromRoute] string id)
    {
        var pagedAgents = await _agentService.GetAgents(new AgentFilter
        {
            AgentIds = new List<string> { id }
        });

        var foundAgent = pagedAgents.Items.FirstOrDefault();
        if (foundAgent == null) return null;

        await _agentService.InheritAgent(foundAgent);
        var targetAgent = AgentViewModel.FromAgent(foundAgent);
        var agentSetting = _services.GetRequiredService<AgentSettings>();
        targetAgent.IsHost = targetAgent.Id == agentSetting.HostAgentId;

        var redirectAgentIds = targetAgent.RoutingRules
                                          .Where(x => !string.IsNullOrEmpty(x.RedirectTo))
                                          .Select(x => x.RedirectTo)
                                          .ToList();

        var redirectAgents = await _agentService.GetAgents(new AgentFilter
        {
            AgentIds = redirectAgentIds
        });
        foreach (var rule in targetAgent.RoutingRules)
        {
            var found = redirectAgents.Items.FirstOrDefault(x => x.Id == rule.RedirectTo);
            if (found == null) continue;
            
            rule.RedirectToAgentName = found.Name;
        }

        var editable = true;
        var chatable = true;
        var userService = _services.GetRequiredService<IUserService>();
        var user = await userService.GetUser(_user.Id);
        if (!UserConstant.AdminRoles.Contains(user?.Role))
        {
            var userAgents = await _agentService.GetUserAgents(user?.Id);
            var actions = userAgents?.FirstOrDefault(x => x.AgentId == targetAgent.Id)?.Actions ?? [];
            editable = actions.Contains(UserAction.Edit);
            chatable = actions.Contains(UserAction.Chat);
        }

        targetAgent.Editable = editable;
        targetAgent.Chatable = chatable;
        return targetAgent;
    }

    [HttpGet("/agents")]
    public async Task<PagedItems<AgentViewModel>> GetAgents([FromQuery] AgentFilter filter, [FromQuery] bool checkAuth = false)
    {
        var agentSetting = _services.GetRequiredService<AgentSettings>();
        var userService = _services.GetRequiredService<IUserService>();

        List<AgentViewModel> agents;
        var pagedAgents = await _agentService.GetAgents(filter);

        if (!checkAuth)
        {
            agents = pagedAgents?.Items?.Select(x => AgentViewModel.FromAgent(x))?.ToList() ?? [];
            return new PagedItems<AgentViewModel>
            {
                Items = agents,
                Count = pagedAgents?.Count ?? 0
            };
        }

        var userAgents = new List<UserAgent>();
        var user = await userService.GetUser(_user.Id);
        if (!UserConstant.AdminRoles.Contains(user.Role))
        {
            userAgents = await _agentService.GetUserAgents(user.Id);
        }

        agents = pagedAgents?.Items?.Select(x =>
        {
            var chatable = true;
            var editable = true;
            if (!UserConstant.AdminRoles.Contains(user.Role))
            {
                var actions = userAgents.FirstOrDefault(a => a.AgentId == x.Id)?.Actions ?? [];
                chatable = actions.Contains(UserAction.Chat);
                editable = actions.Contains(UserAction.Edit);
            }

            var model = AgentViewModel.FromAgent(x);
            model.Editable = editable;
            model.Chatable = chatable;
            return model;
        })?.ToList() ?? [];

        return new PagedItems<AgentViewModel>
        {
            Items = agents,
            Count = pagedAgents?.Count ?? 0
        };
    }

    [HttpPost("/agent")]
    public async Task<AgentViewModel> CreateAgent(AgentCreationModel agent)
    {
        var createdAgent = await _agentService.CreateAgent(agent.ToAgent());
        return AgentViewModel.FromAgent(createdAgent);
    }

    [HttpPost("/refresh-agents")]
    public async Task<string> RefreshAgents()
    {
        return await _agentService.RefreshAgents();
    }

    [HttpPut("/agent/file/{agentId}")]
    public async Task<string> UpdateAgentFromFile([FromRoute] string agentId)
    {
        return await _agentService.UpdateAgentFromFile(agentId);
    }

    [HttpPut("/agent/{agentId}")]
    public async Task UpdateAgent([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.All);
    }

    [HttpPatch("/agent/{agentId}/{field}")]
    public async Task PatchAgentByField([FromRoute] string agentId, AgentField field, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, field);
    }

    [HttpPatch("/agent/{agentId}/templates")]
    public async Task<string> PatchAgentTemplates([FromRoute] string agentId, [FromBody] AgentTemplatePatchModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        return await _agentService.PatchAgentTemplate(model);
    }

    [HttpDelete("/agent/{agentId}")]
    public async Task<bool> DeleteAgent([FromRoute] string agentId)
    {
        return await _agentService.DeleteAgent(agentId);
    }

    [HttpGet("/agent/utilities")]
    public IEnumerable<string> GetAgentUtilities()
    {
        return _agentService.GetAgentUtilities();
    }
}