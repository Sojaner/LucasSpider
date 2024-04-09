using System;
using Newtonsoft.Json.Linq;

namespace LucasSpider.Extensions
{
	internal static class StringExtensions
	{
		public static bool TryParseJToken(this string jsonString, out JToken token)
		{
			try
			{
				token = JToken.Parse(jsonString);
				return true;
			}
			catch (Exception)
			{
				token = null;
				return false;
			}
		}
	}
}
