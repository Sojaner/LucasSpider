namespace DotnetSpider.Downloader
{
	public class DownloaderOptions
	{
		/// <summary>
		/// Should the downloader track redirects
		/// </summary>
		public bool TrackRedirects { get; set; }

		/// <summary>
		/// Maximum number of allowed redirects
		/// </summary>
		public int MaximumAllowedRedirects { get; set; }
	}
}
