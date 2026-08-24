using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Application.Abstractions.Realtime.Presence;

namespace GameHub.Infrastructure.Hubs;

public interface ChatClientAdapter : IChatClient, IPresenceClient {}
