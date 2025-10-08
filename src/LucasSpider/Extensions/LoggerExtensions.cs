using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace LucasSpider.Extensions;

internal static class LoggerExtensions
{
	internal static void LogWithProperties(this ILogger logger, Action<ILogger> log, params (string Key, object Value)[] properties)
	{
		var props = properties.ToDictionary(kv => kv.Key, kv => kv.Value);

		using (logger.BeginScope(props))
		{
			log(logger);
		}
	}
}
