using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LucasSpider.Extensions;
using LucasSpider.AgentCenter.Store;
using LucasSpider.MessageQueue;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IMessageQueue = LucasSpider.MessageQueue.IMessageQueue;
using MessageQueue_IMessageQueue = LucasSpider.MessageQueue.IMessageQueue;

namespace LucasSpider.AgentCenter
{
	public class AgentCenterService : BackgroundService
	{
		private readonly ILogger<AgentCenterService> _logger;
		private readonly IAgentStore _agentStore;
		private readonly MessageQueue_IMessageQueue _messageQueue;
		private AsyncMessageConsumer<byte[]> _consumer;
		private readonly bool _distributed;

		public AgentCenterService(IAgentStore agentStore, ILogger<AgentCenterService> logger,
			MessageQueue_IMessageQueue messageQueue)
		{
			_agentStore = agentStore;
			_logger = logger;
			_messageQueue = messageQueue;
			_distributed = !(messageQueue is MessageQueue.MessageQueue);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				_logger.LogInformation("Agent center service is starting");

				await _agentStore.EnsureDatabaseAndTableCreatedAsync();

				_consumer = new AsyncMessageConsumer<byte[]>(Topics.AgentCenter);
				_consumer.Received += async bytes =>
				{
					object message;
					try
					{
						message = await bytes.DeserializeAsync(stoppingToken);
						if (message == null)
						{
							return;
						}
					}
					catch (Exception e)
					{
						_logger.LogError("Deserializing message failed: {e}", e);
						return;
					}

					switch (message)
					{
						case Messages.Agent.Register register:
						{
							if (_distributed)
							{
								_logger.LogInformation($"Registering agent: {register.AgentId}, {register.AgentName}");
							}

							await _agentStore.RegisterAsync(new AgentInfo(register.AgentId, register.AgentName,
								register.ProcessorCount,
								register.Memory));
							break;
						}
						case Messages.Agent.Heartbeat heartbeat:
						{
							if (_distributed)
							{
								_logger.LogInformation(
									$"Received heartbeat: {heartbeat.AgentId}, {heartbeat.AgentName}");
							}

							await _agentStore.HeartbeatAsync(new AgentHeartbeat(heartbeat.AgentId, heartbeat.AgentName,
								heartbeat.AvailableMemory, heartbeat.CpuLoad));
							break;
						}
						default:
						{
							var msg = JsonSerializer.Serialize(message);
							_logger.LogWarning("Message not supported: {msg}", msg);
							break;
						}
					}
				};
				await _messageQueue.ConsumeAsync(_consumer, stoppingToken);
				_logger.LogInformation("Agent center service started");
			}
			catch (Exception e)
			{
				_logger.LogCritical(e.ToString());
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Agent center service is stopping");
			_consumer?.Close();

			await base.StopAsync(cancellationToken);
			_logger.LogInformation("Agent center service stopped");
		}
	}
}
