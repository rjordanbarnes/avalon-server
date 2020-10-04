using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Avalon.Server.Hubs
{
    public class MainHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}