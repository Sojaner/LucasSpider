namespace DotnetSpider
{
	public class SpiderOptions
	{
		/// <summary>
		/// Request queue count limit
		/// </summary>
		public int RequestedQueueCount { get; set; } = 1000;

		/// <summary>
		/// Request link depth limit
		/// </summary>
		public int Depth { get; set; }

		/// <summary>
		/// Request retry limit
		/// </summary>
		public int RetriedTimes { get; set; } = 3;

		/// <summary>
		/// Timeout before exiting the crawler if no links in the queue
		/// </summary>
		public int EmptySleepTime { get; set; } = 60;

		/// <summary>
		/// Crawler collection speed, 1 means one request per second, 0.5 means 0.5 requests per second, 5 means 5 requests per second
		/// </summary>
		public double Speed { get; set; } = 1;

		/// <summary>
		/// Requests queue batch size
		/// </summary>
		public uint Batch { get; set; } = 4;

		/// <summary>
		/// Whether to remove external links
		/// </summary>
		public bool RemoveOutboundLinks { get; set; } = false;

		/// <summary>
		/// Storage type: FullTypeName, AssemblyName
		/// </summary>
		public string StorageType { get; set; } = "DotnetSpider.MySql.MySqlEntityStorage, DotnetSpider.MySql";

		/// <summary>
		/// The time interval for getting new codes
		/// </summary>
		public int RefreshProxy { get; set; } = 30;
	}
}
