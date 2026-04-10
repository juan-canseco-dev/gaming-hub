using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameHub.Application.UnitTests.Shared.Helpers.ReflectionTestHelper;

namespace GameHub.Application.UnitTests.Shared.Factories;

public class ChatTestFactory
{
    public static Chat CreateNew(Guid chatId, int channelId, DateTimeOffset createdAt)
    {
        var chat = (Chat)Activator.CreateInstance(typeof(Chat), nonPublic: true)!;

        SetProperty(chat, nameof(Chat.Id), chatId);
        SetProperty(chat, nameof(Chat.ChannelId), channelId);
        SetProperty(chat, nameof(Chat.CreatedAt), createdAt);
        SetProperty(chat, nameof(Chat.Channel), Channel.FromValue(channelId)!);

        return chat;
    }

}
