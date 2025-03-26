using Next.Hazel;
using Next.Hazel.Abstractions;

namespace Impostor.Api.Extension.Messages;

public class MessageWriterProvider : IMessageWriterProvider
{
    public IMessageWriter Get(MessageType sendOption = MessageType.Unreliable)
    {
        return MessageWriter.Get(sendOption);
    }
}
