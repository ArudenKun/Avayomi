using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ServiceScan.SourceGenerator;

namespace Avayomi.Messaging;

public static partial class MessengerExtensions
{
    [ScanForTypes(AssignableTo = typeof(IRequest<>), Handler = nameof(RegisterReceiverHandler))]
    public static partial void RegisterAllReceivers(this IMessenger messenger, object instance);

    private static void RegisterReceiverHandler<TReceiver, TMessage>(
        IMessenger messenger,
        object instance
    )
        where TReceiver : class, IRequest<TMessage>
        where TMessage : class
    {
        if (instance is not TReceiver receiver)
            return;

        messenger.Register<TReceiver, RequestMessage<TMessage>>(
            receiver,
            (r, m) =>
            {
                var response = r.Handle();
                m.Reply(response);
            }
        );
    }
}
