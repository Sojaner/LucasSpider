using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using DotnetSpider.Http;
using Microsoft.Playwright;

namespace DotnetSpider.Downloader
{
	public class PlaywrightDownloader(IBrowser browser) : IDownloader
	{
		public async Task<Response> DownloadAsync(Request request)
		{
			try
			{
				await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
				{
					UserAgent = request.Headers.UserAgent
				});
				context.SetDefaultNavigationTimeout(request.Timeout);

				var page = await context.NewPageAsync();
				var requests = new List<IRequest>();
				page.Request += (_, r) => requests.Add(r);
				await page.GotoAsync(request.RequestUri.AbsoluteUri);

				await page.WaitForLoadStateAsync();
				var document = await requests
					.Single(r => r.Frame == page.MainFrame && r.IsNavigationRequest &&
					             r.RedirectedTo == null && r.ResourceType == "document")
					.ResponseAsync();

				if (document == null)
				{
					throw new Exception("No Response");
				}

				var redirects = await GetRedirectsAsync(document);
				var httpResponse = await ConvertIResponseToHttpResponse(document);

				var response = await httpResponse.ToResponseAsync();
				response.Elapsed = TimeSpan.FromMilliseconds(document.Request.Timing.ResponseEnd);
				response.RequestHash = request.Hash;
				response.Version = httpResponse.Version;

				await context.CloseAsync();

				return response;
			}
			catch (Exception e)
			{
				return Response.CreateFailedResponse(e, request.Hash);
			}
		}

		private static async Task<IEnumerable<RedirectResponse>> GetRedirectsAsync(IResponse response)
		{
			var list = new List<RedirectResponse>();
			var parent = response.Request.RedirectedFrom;

			while (parent != null)
			{
				var patchingResponse = await parent.ResponseAsync();
				list.Add(new RedirectResponse
				{
					RequestUri = new Uri(parent.Url),
					StatusCode = (HttpStatusCode)(patchingResponse?.Status ?? 0),
					ResponseTime = parent.Timing.ResponseStart >= 0 ?
						TimeSpan.FromMilliseconds(parent.Timing.ResponseStart) :
						null
				});
				parent = parent.RedirectedFrom;
			}

			list.Reverse();
			return list;
		}

		private static async Task<HttpResponseMessage> ConvertIResponseToHttpResponse(IResponse playwrightResponse)
		{
			var httpResponse = new HttpResponseMessage((HttpStatusCode)playwrightResponse.Status)
			{
				Content = new System.Net.Http.ByteArrayContent(await playwrightResponse.BodyAsync()),
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
