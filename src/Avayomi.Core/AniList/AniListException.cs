using System.Net;

namespace Avayomi.Core.AniList;

public class AniListException : Exception
{
    public string ActualRequestBody { get; }
    public string ActualResponseBody { get; }
    public HttpStatusCode StatusCode { get; }

    internal AniListException(
        string message,
        string actualRequestBody,
        string actualResponseBody,
        HttpStatusCode statusCode
    )
        : base(message)
    {
        ActualRequestBody = actualRequestBody;
        ActualResponseBody = actualResponseBody;
        StatusCode = statusCode;
    }
}
