using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetSpider.Http;
using DotnetSpider.Infrastructure;
using DotnetSpider.Scheduler.Component;
using Microsoft.Extensions.Options;

namespace DotnetSpider.Scheduler
{
	/// <summary>
	/// Memory-based breadth-first scheduling (deduplicated URLs)
	/// </summary>
	public class QueueDistinctBfsScheduler : SchedulerBase
	{
		private readonly SpiderOptions _options;

		private readonly List<Request> _requests =
			new();

		public QueueDistinctBfsScheduler(IDuplicateRemover duplicateRemover, IRequestHasher requestHasher, IOptions<SpiderOptions> options)
			: base(duplicateRemover, requestHasher)
		{
			_options = options.Value;
		}

		public override void Dispose()
		{
			_requests.Clear();
			base.Dispose();
		}

		/// <summary>
		/// If the request is not repeated, add it to the queue
		/// </summary>
		/// <param name="request">Request</param>
		protected override Task PushWhenNoDuplicate(Request request)
		{
			if (request == null)
			{
				return Task.CompletedTask;
			}
			_requests.Add(request);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Remove the specified number of requests for the specified crawler from the queue
		/// </summary>
		/// <param name="count">Number of outputs</param>
		/// <returns>Request</returns>
		protected override Task<IEnumerable<Request>> ImplDequeueAsync(int count = 1)
		{
			var requests = _options.OneRequestDoneFirst ?
					_requests
					.OrderByDescending(x => x.Depth)
					.Take(count).ToArray() :
					_requests.Take(count).ToArray();

			if (requests.Length > 0)
			{
				requests = _requests
					.OrderByDescending(x => x.Depth)
					.Take(count).ToArray();

				if (requests.Length > 0)
				{
					foreach (var request in requests)
					{
						_requests.Remove(request);
					}
				}
			}
			else
			{
				requests = _requests.Take(count).ToArray();
				if (requests.Length > 0)
				{
					_requests.RemoveRange(0, count);
				}
			}

			return Task.FromResult(requests.Select(x => x.Clone()));
		}
	}
}
