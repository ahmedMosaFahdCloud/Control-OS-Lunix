using Control_OS_Lunix.Composition;
using Control_OS_Lunix.UI.Controllers;
using Control_OS_Lunix.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Control_OS_Lunix;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using ServiceProvider serviceProvider = ServiceRegistration.BuildServiceProvider();
        var mainView = serviceProvider.GetRequiredService<MainDashboardView>();
        var mainController = serviceProvider.GetRequiredService<IMainDashboardController>();
        mainController.AttachView(mainView);

        Application.Run(mainView);
    }
}
