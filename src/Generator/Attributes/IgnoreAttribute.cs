using System;

namespace Generator.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreAttribute : Attribute;