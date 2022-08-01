using System;
using System.Net.Sockets;

namespace Hydra.Net.RPC
{
	public class RPCSession : RPCClient
	{
		private RPCServer _server = null;

		public RPCSession(RPCServer server, TcpClient client)
		{
			base.IsAtServerSide = true;
			this._server = server;
			if (null != this._server)
			{
				base.CmdBus = this._server.CmdBus;
			}
			base.BeginRead(client);
		}

		protected override void Register()
		{
		}

		public void Close()
		{
			base.DisConnect();
		}

		public void Notify(string datas)
		{
			base.Send(datas);
		}
	}
}
