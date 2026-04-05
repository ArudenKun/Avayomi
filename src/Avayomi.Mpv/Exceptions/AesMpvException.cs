using Avayomi.Mpv.Native;

namespace Avayomi.Mpv.Exceptions;

public sealed class AesMpvException : Exception
{
    public AesMpvException(MpvError error, string message = "")
        : base(message)
    {
        Error = error;
    }

    public MpvError Error { get; }
}
