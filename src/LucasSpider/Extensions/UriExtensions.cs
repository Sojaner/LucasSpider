using System;

namespace LucasSpider.Extensions
{
	internal static class UriExtensions
	{
		public static Uri ToAbsoluteLocation(this Uri location, Uri requestUri)
		{
			return location.IsAbsoluteUri ? location : new Uri(requestUri, location);
		}
	}
}
