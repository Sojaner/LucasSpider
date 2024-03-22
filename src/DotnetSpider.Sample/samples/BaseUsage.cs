using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Serilog;
using Serilog.Events;

namespace DotnetSpider.Sample.samples
{
	public class BaseUsageSpider : Spider
	{
		public static async Task RunAsync()
		{
			var builder = Builder.CreateDefaultBuilder<BaseUsageSpider>(x =>
			{
				x.Batch = 500;
				x.Speed = 500;
				x.Depth = 1000;
			});
			builder.UseSerilog((_, configuration) => configuration.WriteTo.File("C:\\Users\\sojan\\Desktop\\crawler\\log.txt", LogEventLevel.Error, flushToDiskInterval: TimeSpan.FromSeconds(1)));
			builder.UseDownloader<HttpClientDownloader>();
			builder.UseQueueDistinctBfsScheduler<HashSetDuplicateRemover>();
			await builder.Build().RunAsync();
		}

		private const string Domain = "ledigajobb.se";

		class MyDataParser : DataParser
		{
			private readonly ConcurrentDictionary<string, string> _dictionary = new();

			private int _count;

			protected override Task ParseAsync(DataFlowContext context)
			{
				_count++;

				var links = context.Selectable.Links().ToList();

				var uri = context.Request.RequestUri.AbsoluteUri;

				foreach (var link in links)
				{
					_dictionary.TryAdd(link, uri);
				}

				if (context.Selectable is HtmlSelectable)
				{
					//context.AddData($"URL {_count}", uri);
					//context.AddData("Parent", _dictionary.TryGetValue(uri, out uri) ? uri : "---");
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

		private int _count;

		protected override void ConfigureRequest(Request request)
		{
			/*if (_count == 500)
			{
				Options.Speed = 1;

				Options.Batch = 1;
			}
			else if (_count == 600)
			{
				Options.Speed = 500;

				Options.Batch = 500;
			}*/

			_count++;

			base.ConfigureRequest(request);
		}

		protected override async Task InitializeAsync(CancellationToken stoppingToken = default)
		{
			var request = new Request($"https://{Domain}/");
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
