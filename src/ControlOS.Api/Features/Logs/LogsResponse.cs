namespace ControlOS.Api.Features.Logs;

public sealed record LogsResponse(IReadOnlyList<string> Lines);
