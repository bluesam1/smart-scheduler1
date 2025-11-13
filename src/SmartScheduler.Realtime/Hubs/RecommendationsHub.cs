using Microsoft.AspNetCore.SignalR;

namespace SmartScheduler.Realtime.Hubs;

/// <summary>
/// SignalR hub for real-time recommendations and job assignment notifications.
/// Supports groups for dispatchers (/dispatch/{region}) and contractors (/contractor/{id}).
/// </summary>
public class RecommendationsHub : Hub
{
    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// Automatically removes the client from all groups.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Adds a dispatcher to a region group.
    /// Group name format: dispatch/{region}
    /// </summary>
    /// <param name="region">The region identifier</param>
    public async Task JoinDispatchGroup(string region)
    {
        var groupName = $"dispatch/{region}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Removes a dispatcher from a region group.
    /// </summary>
    /// <param name="region">The region identifier</param>
    public async Task LeaveDispatchGroup(string region)
    {
        var groupName = $"dispatch/{region}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Adds a contractor to their personal group.
    /// Group name format: contractor/{contractorId}
    /// </summary>
    /// <param name="contractorId">The contractor identifier</param>
    public async Task JoinContractorGroup(string contractorId)
    {
        var groupName = $"contractor/{contractorId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Removes a contractor from their personal group.
    /// </summary>
    /// <param name="contractorId">The contractor identifier</param>
    public async Task LeaveContractorGroup(string contractorId)
    {
        var groupName = $"contractor/{contractorId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

