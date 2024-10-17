﻿using System;
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
		/// The target location of the redirect
		/// </summary>
		public Uri TargetUri { get; set; }

		/// <summary>
		/// The status code of the response
		/// </summary>
		public HttpStatusCode StatusCode { get; set; }

		/// <summary>
		/// The time it took to get the response
		/// </summary>
		public TimeSpan TimeToHeaders { get; set; }

		/// <summary>
		/// The order of the redirect in the chain
		/// </summary>
		public int Order { get; set; }

		public object Clone()
		{
			return new RedirectResponse
			{
				TimeToHeaders = TimeToHeaders, RequestUri = RequestUri, TargetUri = TargetUri, StatusCode = StatusCode, Order = Order
			};
		}
	}
}
