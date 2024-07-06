namespace Generator;

internal record DiagnosticDetail(
    string Id,
    string Message,
    string Title = "",
    string Category = ""
);
