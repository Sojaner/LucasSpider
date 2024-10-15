using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace LucasSpider.Infrastructure
{
	public class ObjectUtilities
	{
		public static void DisposeSafely(params IDisposable[] objs)
		{
			foreach (var obj in objs)
			{
				try
				{
					obj?.Dispose();
				}
				catch (Exception)
				{
					// ignored
				}
			}
		}

		public static void DisposeSafely(IDisposable obj)
		{
			try
			{
				obj?.Dispose();
			}
			catch (Exception)
			{
				// ignored
			}
		}

		public static void DisposeSafelyAndLog(ILogger logger, params IDisposable[] objs)
		{
			DisposeSafelyAndLog(logger, objs.AsEnumerable());
		}

		public static void DisposeSafelyAndLog(ILogger logger, IEnumerable<IDisposable> objs)
		{
			foreach (var obj in objs)
			{
				try
				{
					obj?.Dispose();
				}
				catch (Exception e)
				{
					logger.LogWarning("Dispose {obj} failed: {e}", obj, e);
				}
			}
		}
	}
}
