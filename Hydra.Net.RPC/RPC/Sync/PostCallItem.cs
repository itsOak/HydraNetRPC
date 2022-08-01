using System;

namespace Hydra.Net.RPC
{
	public class PostCallItem : RPCSyncItem
	{
		public RPCClient Client { get; set; }

		public PostCallItem(RPCClient client, string id, string request, int timeout = 10) : base(id, request, timeout)
		{
			this.Client = client;
		}

		public override void OnTimeout(string response)
		{
			if (this.Client != null && this.Client.CmdBus != null)
			{
				this.Client.CmdBus.PostCall(this.Client, response);
			}
		}
	}
}
