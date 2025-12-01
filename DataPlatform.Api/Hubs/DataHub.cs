using Microsoft.AspNetCore.SignalR;

namespace DataPlatform.Api.Hubs;

public class DataHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}