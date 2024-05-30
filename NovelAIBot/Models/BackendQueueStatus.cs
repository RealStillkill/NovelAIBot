using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelAIBot.Models
{
	public class BackendQueueStatus
	{
		public Guid Id { get; set; }
		public int QueuePosition { get; set; }

		public NaiQueueState State { get; set; }

		public BackendQueueStatus()
		{
		}

		public BackendQueueStatus(Guid? id, int queuePosition, NaiQueueState state = NaiQueueState.Enqueued)
		{
			Id = id ?? Guid.Empty;
			QueuePosition = queuePosition;
			State = state;
		}
	}

	public enum NaiQueueState { Enqueued, Processing, CompletedSuccess, CompletedError }
}
