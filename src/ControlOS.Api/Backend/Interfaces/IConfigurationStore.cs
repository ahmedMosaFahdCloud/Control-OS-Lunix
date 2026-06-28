using ControlOS.Api.Backend.Models;
using ControlOS.Api.Backend.Results;

namespace ControlOS.Api.Backend.Interfaces;

public interface IConfigurationStore
{
    Result<AppConfiguration> Load();

    Result Save(AppConfiguration configuration);
}
