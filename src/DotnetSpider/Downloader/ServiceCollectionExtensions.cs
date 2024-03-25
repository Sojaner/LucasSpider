using DotnetSpider.Agent;
using DotnetSpider.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

namespace DotnetSpider.Downloader
{
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Only local crawlers can configure downloaders. The downloader registration for distributed crawlers is in the downloader agent.
		/// </summary>
		/// <param name="builder"></param>
		/// <typeparam name="TDownloader"></typeparam>
		/// <returns></returns>
		public static Builder UseDownloader<TDownloader>(this Builder builder)
			where TDownloader : class, IDownloader
		{
			builder.ConfigureServices(x =>
			{
				x.AddTransient<HttpMessageHandlerBuilder, DefaultHttpMessageHandlerBuilder>();
				x.AddAgent<TDownloader>(opts =>
				{
					opts.AgentId = ObjectId.CreateId().ToString();
					opts.AgentName = opts.AgentId;
				});
			});

			return builder;
		}
	}
}
