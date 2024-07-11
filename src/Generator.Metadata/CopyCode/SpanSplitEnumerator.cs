using System;

namespace Generator.Metadata.CopyCode;

public static class MemoryExtensions
{
    public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, char separator) =>
        new(span, separator);
}

public ref struct SpanSplitEnumerator<T>
    where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> _toSplit;
    private readonly T _separator;
    private int _offset;
    private int _index;

    public readonly SpanSplitEnumerator<T> GetEnumerator() => this;

    internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
    {
        _toSplit = span;
        _separator = separator;
        _index = 0;
        _offset = 0;
    }

    public readonly ReadOnlySpan<T> Current => _toSplit.Slice(_offset, _index - 1);

    public bool MoveNext()
    {
        if (_toSplit.Length - _offset < _index)
        {
            return false;
        }
        var slice = _toSplit[(_offset += _index)..];

        var nextIndex = slice.IndexOf(_separator);
        _index = (nextIndex != -1 ? nextIndex : slice.Length) + 1;
        return true;
    }
}
