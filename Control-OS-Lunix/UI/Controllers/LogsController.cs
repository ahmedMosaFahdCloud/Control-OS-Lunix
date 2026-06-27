using Control_OS_Lunix.Backend.Interfaces;
using Control_OS_Lunix.Backend.Results;
using Control_OS_Lunix.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Control_OS_Lunix.UI.Controllers;

public sealed class LogsController : ILogsController
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogService _logService;

    public LogsController(IServiceProvider serviceProvider, ILogService logService)
    {
        _serviceProvider = serviceProvider;
        _logService = logService;
    }

    public async Task<Result> ShowAsync(IWin32Window owner)
    {
        Result<IReadOnlyList<string>> linesResult = await _logService.ReadRecentLinesAsync(500);
        if (!linesResult.IsSuccess)
        {
            return Result.Failure(linesResult.ErrorCode, linesResult.Message);
        }

        LogsView view = _serviceProvider.GetRequiredService<LogsView>();
        view.SetLines(linesResult.Value ?? []);
        view.ShowDialogView(owner);
        return Result.Success();
    }
}
