using AutoInterfaceAttributes;

namespace Core;

[AutoInterface]
public class Factory<T>(Func<T> initFunc) : IFactory<T>
{
    public T Create()
    {
        return initFunc();
    }
}
