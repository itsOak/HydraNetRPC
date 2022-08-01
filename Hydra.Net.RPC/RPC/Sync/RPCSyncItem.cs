using System;

namespace Hydra.Net.RPC
{
	public abstract class RPCSyncItem
	{
		public string Id { get; set; }

		public string Request { get; set; }

		public DateTime ExpireTime { get; set; }

		public RPCSyncItem(string id, string request, int timeout = 10)
		{
			this.Id = id;
			this.Request = request;
			this.ExpireTime = DateTime.Now.AddSeconds((double)timeout);
		}

		public abstract void OnTimeout(string response);
	}
}
