using System;
using AutoInterfaceAttributes;

namespace Desktop.Services.Factories;

[AutoInterface]
public class Factory<T>(Func<T> initFunc) : IFactory<T>
{
    public T Create()
    {
        return initFunc();
    }
}
