using System;
using AutoInterfaceAttributes;

namespace Avayomi.Factories;

[AutoInterface]
public class Factory<T>(Func<T> initFunc) : IFactory<T>
{
    public T Create() => initFunc();
}
