using BepInEx.Logging;

namespace AzuEPI.Core.Utilities.Extensions;

public static class LoggerExtensions
{
    public static void LogInfoDebug(this ManualLogSource logger, string message)
    {
#if DEBUG
        logger.LogInfo(message);
#endif
    }

    public static void LogWarningDebug(this ManualLogSource logger, string message)
    {
#if DEBUG
        logger.LogWarning(message);
#endif
    }

    public static void LogErrorDebug(this ManualLogSource logger, string message)
    {
#if DEBUG
        logger.LogError(message);
#endif
    }

    public static void LogDebugDebug(this ManualLogSource logger, string message)
    {
#if DEBUG
        logger.LogDebug(message);
#endif
    }
}