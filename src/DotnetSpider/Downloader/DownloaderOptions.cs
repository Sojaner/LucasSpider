namespace DotnetSpider.Downloader
{
	public class DownloaderOptions
	{
		/// <summary>
		/// Should the downloader track redirects
		/// </summary>
		public bool TrackRedirects { get; set; } = true;

		/// <summary>
		/// Maximum number of allowed redirects
		/// </summary>
		public int MaximumAllowedRedirects { get; set; } = 5;

		/// <summary>
		/// The playwright browser to use
		/// </summary>
		public PlaywrightBrowserType BrowserType { get; set; } = PlaywrightBrowserType.Chromium;
	}
}
