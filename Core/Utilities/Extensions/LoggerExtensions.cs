using BepInEx.Logging;

namespace AzuEPI.Core.Utilities.Extensions;

public static class LoggerExtensions
{
    extension(ManualLogSource logger)
	{
		public void LogInfoDebug(string message)
		{
#if DEBUG
        logger.LogInfo(message);
#endif
		}

		public void LogWarningDebug(string message)
		{
#if DEBUG
        logger.LogWarning(message);
#endif
		}

		public void LogErrorDebug(string message)
		{
#if DEBUG
        logger.LogError(message);
#endif
		}

		public void LogDebugDebug(string message)
		{
#if DEBUG
        logger.LogDebug(message);
#endif
		}
	}
}