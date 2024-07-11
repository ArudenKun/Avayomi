using System;
using Generator.Metadata.CopyCode;

namespace Generator.Attributes;

[Copy]
[AttributeUsage(AttributeTargets.Class)]
public sealed class StaticViewLocatorAttribute : Attribute;
