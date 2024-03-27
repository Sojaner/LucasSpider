using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using DotnetSpider.DataFlow;
using DotnetSpider.DataFlow.Parser;
using DotnetSpider.Downloader;
using DotnetSpider.Http;
using DotnetSpider.Infrastructure;
using DotnetSpider.Scheduler;
using DotnetSpider.Scheduler.Component;
using DotnetSpider.Selector;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Serilog;

namespace DotnetSpider.Sample.samples
{
	public class BaseUsageSpider : Spider
	{
		public static async Task RunAsync()
		{
			var builder = Builder.CreateDefaultBuilder<BaseUsageSpider>(x =>
			{
				x.Batch = 1;
				x.Speed = 1;
				x.Depth = 1000;
			});
			builder.UseSerilog(/*(_, configuration) => configuration.WriteTo.File("log.txt", LogEventLevel.Information, flushToDiskInterval: TimeSpan.FromSeconds(1))*/);
			builder.UseDownloader<MyDownloader>();
			builder.UseQueueDistinctBfsScheduler<HashSetDuplicateRemover>();
			await builder.Build().RunAsync();
		}

		class MyDownloader(IBrowser browser):IDownloader
		{
			static async Task<HttpResponseMessage> ConvertIResponseToHttpResponse(IResponse playwrightResponse)
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

			private static IEnumerable<IRequest> GetRedirects(IResponse response)
			{
				var list = new List<IRequest>{response.Request};

				var parent = response.Request.RedirectedFrom;

				while (parent != null)
				{
					list.Add(parent);

					parent = parent.RedirectedFrom;
				}

				list.Reverse();

				return list;
			}

			public async Task<Response> DownloadAsync(Request request)
			{
				var context = await browser.NewContextAsync();

				var page = await context.NewPageAsync();
				List<IRequest> requests = new();
				page.Request += (_, re) => requests.Add(re);
				await page.GotoAsync(request.RequestUri.AbsoluteUri);

				await page.WaitForLoadStateAsync();
				// requests.Where(x => x.Frame == page.MainFrame && x.IsNavigationRequest && x.RedirectedTo == null && x.ResourceType == "document")
				IResponse re = await requests.Single(x => x.Frame == page.MainFrame && x.IsNavigationRequest && x.RedirectedTo == null && x.ResourceType == "document").ResponseAsync();

				var timing = re != null ? GetRedirects(re).Select(req => req.Timing.ResponseStart) : [];

				var message = await ConvertIResponseToHttpResponse(re);

				var response = await message.ToResponseAsync();
				//response.ElapsedMilliseconds = (int)elapsedMilliseconds;
				response.RequestHash = request.Hash;
				response.Version = message.Version;

				await context.CloseAsync();
				await context.DisposeAsync();

				return response;

				byte[] bytes = await re.BodyAsync();
				var html = await page.MainFrame.ContentAsync();

				return new Response();
			}

			public string Name => "MyDownloader";
		}

		private const string Domain = "www.ledigajobb.se";

		class MyDataParser : DataParser
		{
			private readonly ConcurrentDictionary<string, string> _dictionary = new();

			protected override Task ParseAsync(DataFlowContext context)
			{
				var links = context.Selectable.Links().ToList();

				var uri = context.Request.RequestUri.AbsoluteUri;

				foreach (var link in links)
				{
					_dictionary.TryAdd(link, uri);
				}

				if (context.Selectable is HtmlSelectable)
				{
				}

				return Task.CompletedTask;
			}

			public override Task HandleAsync(DataFlowContext context)
			{
				return context.Response.IsSuccessStatusCode && (context.Response.Content.Headers.ContentType?.StartsWith(MediaTypeNames.Text.Html) ?? false)
					? base.HandleAsync(context)
					: Task.CompletedTask;
			}

			public override Task InitializeAsync()
			{
				AddRequiredValidator(request => request.RequestUri.Scheme is "http" or "https" && !MimeTypePredictor.IsKnownExtension(request.RequestUri) && request.RequestUri.DnsSafeHost == Domain);
				AddFollowRequestQuerier(Selectors.XPath("//a")); // //a[not(@rel='nofollow')]
				return Task.CompletedTask;
			}
		}

		public BaseUsageSpider(IOptions<SpiderOptions> options, DependenceServices services,
			ILogger<Spider> logger) : base(
			options, services, logger)
		{
			OnRequestError += async (sender, args) =>
			{
				await Task.CompletedTask;
			};

			OnRequestTimeout += (sender) =>
			{
			};
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{
			var request = new Request($"http://{Domain}/");
			request.Headers.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
			await AddRequestsAsync(request);
			AddDataFlow(new MyDataParser());
			AddDataFlow(new ConsoleStorage());
		}

		protected override SpiderId GenerateSpiderId()
		{
			return new(ObjectId.CreateId().ToString(), "Blog Garden");
		}
	}

    public static class MimeTypePredictor
    {
        private static HashSet<string> _skippedExtensions;

        static MimeTypePredictor()
        {
            InitializeMapping();
        }

        private static void InitializeMapping()
        {
	        _skippedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	        {
		        ".txt",
		        ".css",
		        ".js",
		        ".json",
		        ".xml",
		        ".jpg",
		        ".jpeg",
		        ".png",
		        ".gif",
		        ".pdf",
		        ".woff",
		        ".woff2",
		        ".ttf",
		        ".eot",
		        ".otf",
		        ".svg",
		        ".mp4",
		        ".webm",
		        ".webp",
		        ".ogg",
		        ".mp3",
		        ".doc",
		        ".docx",
		        ".xls",
		        ".xlsx",
		        ".ppt",
		        ".pptx",
		        ".zip",
		        ".rar",
		        ".tar",
		        ".gz",
		        ".7z",
		        ".exe",
		        ".dll",
		        ".bin",
		        ".iso",
		        ".csv",
		        ".sql",
		        ".mdb",
		        ".mpg",
		        ".mpeg",
		        ".avi",
		        ".mov",
		        ".wav",
		        ".aac",
		        ".flac",
		        ".bmp",
		        ".ico",
		        ".tif",
		        ".tiff",
		        ".psd",
		        ".ai"
	        };
        }

        public static bool IsKnownExtension(Uri uri)
        {
            var extension = Path.GetExtension(uri.AbsolutePath);

            return _skippedExtensions.Contains(extension.ToLower());
        }
    }
}
