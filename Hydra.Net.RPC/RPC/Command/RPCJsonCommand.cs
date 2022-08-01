using System;

namespace Hydra.Net.RPC
{
	public class RPCJsonCommand : RPCCommand
	{
		public string Data { get; set; }

		public RPCJsonCommand() : base(RPCCommandType.Json)
		{
		}

		public RPCJsonCommand(string cmd) : base(RPCCommandType.Json)
		{
			this.Data = cmd;
		}

		public override void Read(RPCDataSerializer reader)
		{
			base.Read(reader);
			this.Data = reader.ReadString();
		}

		public override void Write(RPCDataSerializer writer)
		{
			base.Write(writer);
			writer.WriteString(this.Data);
		}
	}
}
