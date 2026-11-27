namespace SAOTRPG.UI.Helpers;

// Builds one line of text into a caller's buffer, usually stack memory, without allocating. A
// line longer than the buffer is cut off at its end; a HUD line is never wider than the screen,
// and a buffer is sized well past that.
public ref struct LineWriter
{
    private readonly Span<char> _buf;
    private int _len;

    public LineWriter(Span<char> buffer)
    {
        _buf = buffer;
        _len = 0;
    }

    public readonly ReadOnlySpan<char> Text => _buf[.._len];
    public readonly int Length => _len;

    // Drops everything written after `length`.
    public void Truncate(int length) => _len = Math.Clamp(length, 0, _len);

    public void Append(char c)
    {
        if (_len < _buf.Length) _buf[_len++] = c;
    }

    public void Append(ReadOnlySpan<char> text)
    {
        int n = Math.Min(text.Length, _buf.Length - _len);
        text[..n].CopyTo(_buf[_len..]);
        _len += n;
    }

    public void Append(string? text) => Append(text.AsSpan());

    // Formats as interpolation would (current culture), e.g. format "00" for a two-digit field.
    public void Append(int value, ReadOnlySpan<char> format = default)
    {
        if (value.TryFormat(_buf[_len..], out int n, format)) _len += n;
    }

    public void Append(char c, int count)
    {
        for (int i = 0; i < count; i++) Append(c);
    }
}
