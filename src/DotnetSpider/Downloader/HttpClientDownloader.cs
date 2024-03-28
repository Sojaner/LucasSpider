using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DotnetSpider.Http;
using DotnetSpider.Infrastructure;
using DotnetSpider.Proxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetSpider.Downloader
{
	public class HttpClientDownloader : IDownloader
	{
		private readonly IProxyService _proxyService;
		private readonly int _allowedRedirects;
		protected IHttpClientFactory HttpClientFactory { get; }
		protected ILogger Logger { get; }
		protected bool UseProxy { get; }

		public HttpClientDownloader(IHttpClientFactory httpClientFactory,
			IProxyService proxyService,
			ILogger<HttpClientDownloader> logger,
			IOptions<DownloaderOptions> options)
		{
			HttpClientFactory = httpClientFactory;
			Logger = logger;
			_proxyService = proxyService;
			_allowedRedirects = options.Value.MaximumAllowedRedirects;
			UseProxy = !(_proxyService is EmptyProxyService);
		}

		public async Task<Response> DownloadAsync(Request request)
		{
			var httpResponseMessages = new List<HttpResponseMessage>();
			var httpRequestMessages = new List<HttpRequestMessage>();
			try
			{
				var httpRequestMessage = request.ToHttpRequestMessage();

				httpRequestMessages.Add(httpRequestMessage);

				var httpClient = await CreateClientAsync(request);

				var redirects = 0;

				var stopwatch = new Stopwatch();

				HttpStatusCode statusCode;

				long headersTime;

				HttpResponseMessage httpResponseMessage;

				var redirectResponses = new List<RedirectResponse>();

				Uri targetUrl;

				do
				{
					stopwatch.Restart();
					httpResponseMessage = await SendAsync(httpClient, httpRequestMessage);
					headersTime = stopwatch.ElapsedMilliseconds;

					httpResponseMessages.Add(httpResponseMessage);
					redirects++;
					statusCode = httpResponseMessage.StatusCode;
					targetUrl = httpResponseMessage.RequestMessage.RequestUri;

					if (statusCode is HttpStatusCode.Moved or HttpStatusCode.MovedPermanently && redirects <= _allowedRedirects)
					{
						var location = httpResponseMessage.Headers.Location;
						httpRequestMessage = request.Clone().ToHttpRequestMessage();
						httpRequestMessage.RequestUri = location;
						httpRequestMessages.Add(httpRequestMessage);

						redirectResponses.Add(new RedirectResponse
						{
							StatusCode = statusCode,
							ResponseTime = TimeSpan.FromMilliseconds(headersTime),
							RequestUri = targetUrl
						});
					}

				} while (statusCode is HttpStatusCode.Moved or HttpStatusCode.MovedPermanently && redirects <= _allowedRedirects);

				var response = await HandleAsync(request, httpResponseMessage);
				if (response != null)
				{
					response.Version = response.Version == null ? HttpVersion.Version11 : response.Version;
					return response;
				}

				response = await httpResponseMessage.ToResponseAsync();
				response.Elapsed = stopwatch.Elapsed;
				response.RequestHash = request.Hash;
				response.Version = httpResponseMessage.Version;
				response.Redirects = redirectResponses;
				response.TimeToHeaders = TimeSpan.FromMilliseconds(headersTime);
				response.TargetUrl = targetUrl;
				stopwatch.Stop();

				return response;
			}
			catch (Exception e)
			{
				return Response.CreateFailedResponse(e, request.Hash);
			}
			finally
			{
				ObjectUtilities.DisposeSafely(Logger, httpRequestMessages);
				ObjectUtilities.DisposeSafely(Logger, httpResponseMessages);
			}
		}

		protected virtual async Task<HttpResponseMessage> SendAsync(HttpClient httpClient,
			HttpRequestMessage httpRequestMessage)
		{
			return await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
		}

		protected virtual async Task<HttpClient> CreateClientAsync(Request request)
		{
			string name;
			if (UseProxy)
			{
				var proxy = await _proxyService.GetAsync(request.Timeout);
				if (proxy == null)
				{
					throw new SpiderException("Failed to get proxy");
				}

				name = $"{Const.ProxyPrefix}{proxy}";
			}
			else
			{
				name = request.RequestUri.Host;
			}

			var client = HttpClientFactory.CreateClient(name);

			client.Timeout = TimeSpan.FromMilliseconds(request.Timeout);

			if (!string.IsNullOrEmpty(request.Headers.UserAgent))
			{
				client.DefaultRequestHeaders.UserAgent.ParseAdd(request.Headers.UserAgent);
			}
			

			return client;
		}

		protected virtual Task<Response> HandleAsync(Request request, HttpResponseMessage responseMessage)
		{
			return Task.FromResult((Response)null);
		}

		public virtual string Name => UseProxy ? Downloaders.ProxyHttpClient : Downloaders.HttpClient;
	}
}
