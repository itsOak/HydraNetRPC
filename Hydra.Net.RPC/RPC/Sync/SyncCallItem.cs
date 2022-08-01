using System;
using System.Threading;

namespace Hydra.Net.RPC
{
	public class SyncCallItem : RPCSyncItem
	{
		public string Response { get; set; }

		public AutoResetEvent Singal { get; private set; }

		public SyncCallItem(string id, string request, int timeout = 10) : base(id, request, timeout)
		{
			this.Singal = new AutoResetEvent(false);
		}

		public override void OnTimeout(string response)
		{
			if (this.Singal != null)
			{
				this.Response = response;
				this.Singal.Set();
			}
		}
	}
}
