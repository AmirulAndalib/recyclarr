using Recyclarr.Cli.Console.Commands;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Console.Setup;

public class AppDataDirSetupTask(IAppDataSetup appDataSetup) : IGlobalSetupTask
{
    public void OnStart(BaseCommandSettings cmd)
    {
        appDataSetup.SetAppDataDirectoryOverride(cmd.AppData ?? "");
    }

    public void OnFinish()
    {
    }
}