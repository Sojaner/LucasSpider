using System;
using System.Net;

namespace LucasSpider.Http
{
	[Serializable]
	public class RedirectResponse: ICloneable
	{
		/// <summary>
		/// The request location that was redirected
		/// </summary>
		public Uri RequestUri { get; set; }

		/// <summary>
		/// The status code of the response
		/// </summary>
		public HttpStatusCode StatusCode { get; set; }

		/// <summary>
		/// The time it took to get the response
		/// </summary>
		public TimeSpan TimeToHeaders { get; set; }

		public object Clone()
		{
			return new RedirectResponse
			{
				TimeToHeaders = TimeToHeaders, RequestUri = RequestUri, StatusCode = StatusCode
			};
		}
	}
}
