namespace Generator;

public record DiagnosticDetail
{
    public DiagnosticDetail() { }

    public DiagnosticDetail(string id, string category)
    {
        Id = id;
        Category = category;
    }

    public string Id { get; init; } = "";
    public string Category { get; init; } = "";
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
}
