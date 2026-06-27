using Control_OS_Lunix.Backend.Models;
using Control_OS_Lunix.Backend.Results;

namespace Control_OS_Lunix.Backend.Interfaces;

public interface IConfigurationStore
{
    Result<AppConfiguration> Load();

    Result Save(AppConfiguration configuration);
}
