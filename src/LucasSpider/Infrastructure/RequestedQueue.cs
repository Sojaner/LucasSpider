using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
#if NETSTANDARD2_0
using System.Threading;
#endif
using System.Threading.Tasks;
using HWT;
using LucasSpider.Http;

namespace LucasSpider.Infrastructure
{
	public class RequestedQueue : IDisposable
	{
		private readonly ConcurrentDictionary<string, Request> _dict;
		private readonly HashedWheelTimer _timer;
		private ConcurrentBag<Request> _queue;

		public RequestedQueue()
		{
			_dict = new ConcurrentDictionary<string, Request>();
			_queue = new ConcurrentBag<Request>();
			_timer = new HashedWheelTimer(TimeSpan.FromSeconds(1)
				, 100000);
		}

		public int Count => _dict.Count;

		public IReadOnlyList<Uri> Uris => _dict.Values.Select(x => x.RequestUri).ToList();

		public bool Enqueue(Request request)
		{
			if (request.Timeout < 2000)
			{
				throw new SpiderException("Timeout should not less than 2000 milliseconds");
			}

			if (!_dict.TryAdd(request.Hash, request))
			{
				return false;
			}

			// Pass the Timestamp to the timeout task so it can verify
			// that the request in the dict is the same one it was created for.
			_timer.NewTimeout(new TimeoutTask(this, request.Hash, request.Timestamp),
				TimeSpan.FromMilliseconds(request.Timeout));
			return true;
		}


		public Request Dequeue(string hash)
		{
			return _dict.TryRemove(hash, out var request) ? request : null;
		}

		public Request[] GetAllTimeoutList()
		{
			var data = _queue.ToArray();
#if NETSTANDARD2_0
			Interlocked.Exchange(ref _queue, new ConcurrentBag<Request>());
#else
			_queue.Clear();
#endif
			return data;
		}

		private void Timeout(string hash, long timestamp)
		{
			// Only remove the request if it's the same instance that was enqueued
			// when this timer was created (identified by matching timestamp).
			// This prevents a stale timer from removing a different request that
			// reused the same hash after the original was dequeued.
			if (!_dict.TryGetValue(hash, out var request) || request.Timestamp != timestamp)
			{
				return;
			}

			if (_dict.TryRemove(hash, out var removedRequest))
			{
				_queue.Add(removedRequest);
			}
		}

		private class TimeoutTask : ITimerTask
		{
			private readonly string _hash;
			private readonly long _timestamp;
			private readonly RequestedQueue _requestedQueue;

			public TimeoutTask(RequestedQueue requestedQueue, string hash, long timestamp)
			{
				_hash = hash;
				_timestamp = timestamp;
				_requestedQueue = requestedQueue;
			}

			public Task RunAsync(ITimeout timeout)
			{
				_requestedQueue.Timeout(_hash, _timestamp);
				return Task.CompletedTask;
			}
		}

		public void Dispose()
		{
			_dict.Clear();
#if NETSTANDARD2_0
			Interlocked.Exchange(ref _queue, new ConcurrentBag<Request>());
#else
			_queue.Clear();
#endif
			_timer.Stop();
			_timer.Dispose();
		}
	}
}

