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
			var context = await browser.NewContextAsync();

			var page = await context.NewPageAsync();
			var requests = new List<IRequest>();
			page.Request += (_, re) => requests.Add(re);
			await page.GotoAsync(request.RequestUri.AbsoluteUri);

			await page.WaitForLoadStateAsync();
			var re = await requests.Single(req => req.Frame == page.MainFrame && req.IsNavigationRequest && req.RedirectedTo == null && req.ResourceType == "document").ResponseAsync();

			var timing = re != null ? GetRedirects(re).Select(req => req.Timing.ResponseStart) : [];

			var message = await ConvertIResponseToHttpResponse(re);

			var response = await message.ToResponseAsync();
			//response.ElapsedMilliseconds = (int)elapsedMilliseconds;
			response.RequestHash = request.Hash;
			response.Version = message.Version;

			await context.CloseAsync();
			await context.DisposeAsync();

			return response;
		}

		private static IEnumerable<IRequest> GetRedirects(IResponse response)
		{
			var list = new List<IRequest> { response.Request };

			var parent = response.Request.RedirectedFrom;

			while (parent != null)
			{
				list.Add(parent);

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
				// Playwright response headers are stored as dictionary.
				// Convert them to HTTP headers.
				httpResponse.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
				httpResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}

			return httpResponse;
		}

		public string Name => Downloaders.Playwright;
	}
}
