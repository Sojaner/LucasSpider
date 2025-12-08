using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using LucasSpider.Extensions;
using LucasSpider.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace LucasSpider.Downloader
{
	public class PlaywrightDownloader(IBrowser browser, IOptions<DownloaderOptions> options, ILogger<PlaywrightDownloader> logger) : IDownloader
	{
		public async Task<Response> DownloadAsync(Request request)
		{
			var trackingId = Guid.NewGuid();
			var url = request.RequestUri.AbsoluteUri;
			try
			{
				logger.LogWithProperties(l => l.LogDebug("{Downloader} for {Owner} requesting {RequestUri}",
						nameof(PlaywrightDownloader), request.Owner, url),
					("Url", url),
					("ServiceEventName", "Downloader"),
					("ServiceEventType", "RequestSent"),
					("TrackingId", trackingId));

				await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
				{
					UserAgent = request.Headers.UserAgent
				});
				context.SetDefaultNavigationTimeout(request.Timeout);

				var page = await context.NewPageAsync();
				var requests = new List<IRequest>();
				page.Request += onRequest;
				var stopwatch = Stopwatch.StartNew();
				await page.GotoAsync(request.RequestUri.AbsoluteUri);
				stopwatch.Stop();

				await page.WaitForLoadStateAsync();
				page.Request -= onRequest;
				var document = await (requests
					.SingleOrDefault(r => r.Frame == page.MainFrame && r.IsNavigationRequest &&
					                      r.RedirectedTo == null && r.ResourceType == "document")?
					.ResponseAsync() ?? Task.FromResult<IResponse>(null));

				if (document == null)
				{
					var failedResponse = Response.CreateFailedResponse(ResponseReasonPhraseConstants.NoResponse, request.Hash);
					logger.LogWithProperties(l => l.LogWarning("{Downloader} for {Owner} got no response for {RequestUri} with {StatusCode}",
							nameof(PlaywrightDownloader), request.Owner, url, failedResponse.StatusCode),
						("Url", url),
						("ServiceEventName", "Downloader"),
						("ServiceEventType", "NoResponse"),
						("TrackingId", trackingId));
					return failedResponse;
				}

				var redirects = (await GetRedirectsAsync(document)).ToList();
				var httpResponse = await ConvertIResponseToHttpResponse(document, page);

				var response = await httpResponse.ToResponseAsync();
				response.Elapsed = TimeSpan.FromMilliseconds(document.Request.Timing.ResponseEnd);
				response.RequestHash = request.Hash;
				response.Version = httpResponse.Version;
				response.Redirects = options.Value.TrackRedirects ? redirects : [];
				response.TimeToHeaders = TimeSpan.FromMilliseconds(document.Request.Timing.ResponseStart);
				response.TargetUrl = new Uri(document.Url);

				await context.CloseAsync();

				logger.LogWithProperties(l => l.LogInformation("{Downloader} for {Owner} received {RequestUri} with {StatusCode}",
						nameof(PlaywrightDownloader), request.Owner, url, response.StatusCode),
					("Url", url),
					("ServiceEventName", "Downloader"),
					("Redirects", redirects),
					("ServiceEventType", "ResponseReceived"),
					("TrackingId", trackingId));

				return response;
				void onRequest(object _, IRequest r) => requests.Add(r);
			}
			catch (Exception e)
			{
				var failedResponse = Response.CreateFailedResponse(e, request.Hash);
				logger.LogWithProperties(l => l.LogInformation("{Downloader} for {Owner} failed for {RequestUri} with {StatusCode}",
						nameof(PlaywrightDownloader), request.Owner, url, failedResponse.StatusCode),
					("Url", url),
					("ServiceEventName", "Downloader"),
					("ServiceEventType", "FailedResponse"),
					("TrackingId", trackingId));
				return failedResponse;
			}
		}

		private static async Task<IEnumerable<RedirectResponse>> GetRedirectsAsync(IResponse response)
		{
			var list = new List<RedirectResponse>();
			var parent = response.Request.RedirectedFrom;
			var responseStart = response.Request.Timing.ResponseStart; // This is a fallback for WebKit browser since it doesn't have response start time in the redirect chain

			while (parent != null)
			{
				var parentResponse = await parent.ResponseAsync();
				var redirectedTo = parent.RedirectedTo;
				if (parentResponse != null && redirectedTo != null)
				{
					list.Add(new RedirectResponse
					{
						RequestUri = new Uri(parent.Url),
						TargetUri = new Uri(redirectedTo.Url),
						StatusCode = (HttpStatusCode)parentResponse.Status,
						TimeToHeaders = TimeSpan.FromMilliseconds(parent.Timing.ResponseStart >= 0 ?
							parent.Timing.ResponseStart : responseStart),
					});
					parent = parent.RedirectedFrom;
				}
				else
				{
					break;
				}
			}

			list.Reverse();

			for(var i = 0; i < list.Count; i++)
			{
				list[i].Order = i;
			}

			return list;
		}

		private static async Task<HttpResponseMessage> ConvertIResponseToHttpResponse(IResponse playwrightResponse, IPage page)
		{
			var httpResponse = new HttpResponseMessage((HttpStatusCode)playwrightResponse.Status)
			{
				Content = new System.Net.Http.StringContent(await page.ContentAsync(), Encoding.UTF8, MediaTypeNames.Text.Html),
				RequestMessage = new HttpRequestMessage(new HttpMethod(playwrightResponse.Request.Method), playwrightResponse.Request.Url)
			};

			foreach (var header in playwrightResponse.Headers)
			{
				httpResponse.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
				httpResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}

			return httpResponse;
		}

		public string Name => Downloaders.Playwright;
	}
}
