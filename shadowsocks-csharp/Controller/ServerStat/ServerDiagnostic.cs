using Shadowsocks.Framework.Collections;
using Shadowsocks.Framework.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;


namespace Shadowsocks.Controller.ServerStat
{
	public class HostPingHistory
	{
		public const int MAX_HISTORY = 16;

		public PingResult this[int index] => _history[index];
		public int Size => _history.Size;
		private readonly CircularBuffer<PingResult> _history = new CircularBuffer<PingResult>(MAX_HISTORY);


		public void Add(PingResult result)
		{
			_history.PushFront(result);
		}
	}


	public class PingResult
	{
		/// See <see cref="PingReply.Address"/>
		public readonly IPAddress IP;

		/// See <see cref="PingReply.Options"/>
		public readonly PingOptions Options;

		/// <see cref="PingReply.Buffer"/> count
		public readonly int BufferCount;

		public readonly ReadOnlyCollection<Reply> Replies;

		public readonly DateTime Time;


		public PingResult(IPAddress ip, PingOptions options, int bufferCount, ReadOnlyCollection<Reply> replies, DateTime time)
		{
			IP = ip;
			Options = options;
			BufferCount = bufferCount;
			Replies = replies;
			Time = time;
		}

		public static PingResult CreateFailed(DateTime time)
		{
			return new PingResult(null, null, -1, null, time);
		}


		public class Reply
		{
			/// See <see cref="PingReply.Status"/>
			public IPStatus Status { get; private set; }

			/// See <see cref="PingReply.RoundtripTime"/>
			public long RoundtripTime { get; private set; }


			public static Reply Create(PingReply reply)
			{
				return new Reply
				{
					Status = reply.Status,
					RoundtripTime = reply.RoundtripTime
				};
			}
		}
	}




	public enum PingState
	{
		NotPing,
		Pinging,
		Timeout,
		Complete
	}




	public class ServerDiagnostic
	{
		private const int PingTimeout = 500;  
		private readonly ConcurrentDictionary<string, HostPingHistory> _hostHistories = new ConcurrentDictionary<string, HostPingHistory>();
		private readonly ConcurrentDictionary<string, object> _hostPingState = new ConcurrentDictionary<string, object>();


		public void PingAsync(string host, bool throwIfFail = true)
		{
			// set ping state
			lock (_hostPingState)
			{
				if (_hostPingState.ContainsKey(host))
				{
					if (!throwIfFail) return;
					throw new Exception("Avoid duplicate ping");
				}
				_hostPingState[host] = null;
			}

			ThreadPool.QueueUserWorkItem(state =>
			{
				try
				{
					DoPing(host);
				}
				catch (PingException e)
				{
					Debug.WriteLine($@"Ping {host}:\n" + e);
					if (throwIfFail)
						throw;
				}
				catch (Exception e)
				{
					Debug.WriteLine($@"Ping {host}:\n" + e);
					if (throwIfFail)
						throw;
				}
				finally
				{
					lock (_hostPingState)
					{
						_hostPingState.TryRemove(host, out _);
					}
				}
			});
		}


		public PingResult Ping(string host, bool throwIfFail = true)
		{
			// set ping state
			lock (_hostPingState)
			{
				if (_hostPingState.ContainsKey(host))
				{
					if (!throwIfFail) return null;
					throw new Exception("Avoid duplicate ping");
				}
				_hostPingState[host] = null;
			}

			try
			{
				return DoPing(host);
			}
			finally
			{
				lock(_hostPingState)
				{
					_hostPingState.TryRemove(host, out _);
				}
			}
		}

		private PingResult DoPing(string host)
		{
			var now = DateTime.Now;
			var replies = Diagnostic.Ping(host, 4, PingTimeout);
			var result = CreatePingResult(now, replies);

			// save as history
			if (!_hostHistories.TryGetValue(host, out var history))
			{
				history = new HostPingHistory();
				_hostHistories[host] = history;
			}
			history.Add(result);

			return result;
		}

		private static PingResult CreatePingResult(DateTime time, PingReply[] replies)
		{
			if (replies == null || replies.All(e => e == null))
				return PingResult.CreateFailed(time);

			// make replies
			var list = replies.Select(PingResult.Reply.Create).ToList();
			var collection = new ReadOnlyCollection<PingResult.Reply>(list);

			var reply = replies.First(e => e != null);
			return new PingResult(reply.Address, reply.Options, reply.Buffer.Length, collection, time);
		}


		public bool TryGetHostPingHistory(string host, out HostPingHistory history)
		{
			return _hostHistories.TryGetValue(host, out history);
		}


		public bool IsPinging(string host)
		{
			// ReSharper disable once InconsistentlySynchronizedField
			return _hostPingState.ContainsKey(host);
		}
	}
}
