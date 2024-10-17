using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LucasSpider.Http
{
	[Serializable]
	public class Response : IDisposable, ICloneable
	{
		[JsonInclude]
		private Version _version;
		[JsonInclude]
		private ResponseHeaders _headers;
		[JsonInclude]
		private ResponseHeaders _trailingHeaders;
		private bool _disposed;

		public ResponseHeaders Headers => _headers ??= new ResponseHeaders();

		public ResponseHeaders TrailingHeaders => _trailingHeaders ??= new ResponseHeaders();

		public Encoding Encoding => IsSuccessStatusCode ? Content.GetEncoding() : null;

		public string Agent { get; set; }

		/// <summary>
		/// Request
		/// </summary>
		public string RequestHash { get; set; }

		public Version Version
		{
			get => _version;
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(nameof(value));
				}

				_version = value;
			}
		}

		/// <summary>
		/// Response status code
		/// </summary>
		/// <remarks>The <b>StatusCode</b> will be <b>4</b> in case of a <b>Connection Timeout</b>, and <b>0</b> in case of <b>No Response</b></remarks>
		public HttpStatusCode StatusCode { get; set; }

		/// <summary>
		/// Response reason phrase
		/// </summary>
		public string ReasonPhrase { get; set; }

		/// <summary>
		/// Download content
		/// </summary>
		public ByteArrayContent Content { get; set; }

		/// <summary>
		/// Download time
		/// </summary>
		public TimeSpan Elapsed { get; set; }

		/// <summary>
		/// Final address
		/// </summary>
		public Uri TargetUrl { get; set; }

		/// <summary>
		/// Redirects before the final address
		/// </summary>
		public List<RedirectResponse> Redirects { get; set; }

		/// <summary>
		/// Time to download headers
		/// </summary>
		public TimeSpan TimeToHeaders { get; set; }

		public bool IsSuccessStatusCode =>
			StatusCode is >= HttpStatusCode.OK and <= (HttpStatusCode)299;

		public Response EnsureSuccessStatusCode()
		{
			if (!IsSuccessStatusCode)
			{
				throw new HttpRequestException("net_http_message_not_success_statuscode");
			}

			return this;
		}

		public string ReadAsString()
		{
			return Content.GetEncoding().GetString(Content.Bytes);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || _disposed)
			{
				return;
			}

			_disposed = true;

			_headers?.Clear();
			_headers = null;

			_trailingHeaders?.Clear();
			_trailingHeaders = null;

			Content?.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append("StatusCode: ");
			sb.Append((int)StatusCode);
			sb.Append(", ReasonPhrase: '");
			sb.Append(ReasonPhrase ?? "<null>");
			sb.Append("', Version: ");
			sb.Append(_version);
			sb.Append(", Content: ");
			sb.Append(Content == null ? "<null>" : Content.GetType().ToString());
			sb.AppendLine(", Headers:");
			HeaderUtilities.DumpHeaders(sb, _headers, Content?.Headers);

			if (_trailingHeaders == null)
			{
				return sb.ToString();
			}

			sb.AppendLine(", Trailing Headers:");
			HeaderUtilities.DumpHeaders(sb, _trailingHeaders);

			return sb.ToString();
		}

		private static readonly Regex _redirectsRegex = new (@"(?:Load cannot follow more than \d+ redirections|net::ERR_TOO_MANY_REDIRECTS|NS_ERROR_REDIRECT_LOOP)");

		public static Response CreateFailedResponse(Exception exception, string requestHash)
		{
			var isTimeout = exception is TaskCanceledException or TimeoutException;
			var isHttpRequestException = exception is HttpRequestException;
			var message = exception.Message;
			var isRedirects = _redirectsRegex.IsMatch(message);
			return CreateFailedResponse(isTimeout ? ResponseReasonPhraseConstants.ConnectionTimedOut : isRedirects ?
				ResponseReasonPhraseConstants.TooManyRedirects : isHttpRequestException ? ResponseReasonPhraseConstants.NoResponse : message, requestHash);
		}

		public static Response CreateFailedResponse(string message, string requestHash)
		{
			var isTimeout = message == ResponseReasonPhraseConstants.ConnectionTimedOut;
			var isRedirects = message == ResponseReasonPhraseConstants.TooManyRedirects;

			return new Response
			{
				RequestHash = requestHash,
				StatusCode = (HttpStatusCode)(isTimeout ? ExtraHttpStatusCode.Timeout : isRedirects ? ExtraHttpStatusCode.TooManyRedirects : ExtraHttpStatusCode.ConnectionError),
				ReasonPhrase = message,
				Version = HttpVersion.Version11
			};
		}

		public object Clone()
		{
			var response = new Response
			{
				Agent = Agent,
				RequestHash = RequestHash,
				Version = Version,
				StatusCode = StatusCode,
				ReasonPhrase = ReasonPhrase,
				Content = (ByteArrayContent)((Content as IHttpContent)?.Clone()),
				Elapsed = Elapsed,
				TargetUrl = TargetUrl,
				Redirects = [],
				TimeToHeaders = TimeToHeaders
			};

			foreach (var kv in Headers)
			{
				response.Headers.Add(kv.Key, kv.Value);
			}

			foreach (var kv in TrailingHeaders)
			{
				response.TrailingHeaders.Add(kv.Key, kv.Value);
			}

			foreach (var redirect in Redirects)
			{
				response.Redirects.Add((RedirectResponse)redirect.Clone());
			}

			return response;
		}
	}
}
