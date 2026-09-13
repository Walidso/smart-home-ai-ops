using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SmartHome.McpGateway;

// The gateway doesn't reimplement any business logic — every tool here just translates an MCP
// tool call into a plain HTTP call against the Sensors or Actions API and hands the JSON
// response straight back. All the actual logic (validation, the approval workflow, the
// current-status query) already lives in those services from Phases 5-7.
[McpServerToolType]
public class HouseTools(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(Name = "get_house_status")]
    [Description("Gets the current status of every sensor in the house: each sensor's latest reading and whether anything is running hot.")]
    public async Task<string> GetHouseStatus()
    {
        var client = httpClientFactory.CreateClient("SensorsApi");
        var response = await client.GetAsync("/status");
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool(Name = "propose_action")]
    [Description("Proposes an action for a human to approve or reject, such as turning something off. This does NOT execute the action immediately — it only creates a pending approval.")]
    public async Task<string> ProposeAction(
        [Description("Plain-language description of the action being proposed, e.g. 'Turn off the office heater'.")]
        string description,
        [Description("The room this action targets, if applicable.")]
        string? targetRoom = null)
    {
        var client = httpClientFactory.CreateClient("ActionsApi");
        var body = JsonSerializer.Serialize(new { description, targetRoom });
        var response = await client.PostAsync("/actions", new StringContent(body, Encoding.UTF8, "application/json"));
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool(Name = "list_pending_approvals")]
    [Description("Lists all proposed actions that are still awaiting human approval or rejection.")]
    public async Task<string> ListPendingApprovals()
    {
        var client = httpClientFactory.CreateClient("ActionsApi");
        var response = await client.GetAsync("/actions/pending");
        return await response.Content.ReadAsStringAsync();
    }
}
