using AutoInterfaceAttributes;

namespace Core.Factories;

[AutoInterface]
public class Factory<T>(Func<T> initFunc) : IFactory<T>
{
    public T Create() => initFunc();
}
