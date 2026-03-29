namespace Avayomi.Messaging;

public interface IRequest<out TMessage>
{
    TMessage Handle();
}
