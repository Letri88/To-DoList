using Microsoft.AspNetCore.SignalR;

namespace To_doList.Hubs
{
    public class TaskHub : Hub
    {
        public async Task JoinTaskRoom(int taskId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Task_{taskId}");
        }

        public async Task LeaveTaskRoom(int taskId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Task_{taskId}");
        }
    }
}
