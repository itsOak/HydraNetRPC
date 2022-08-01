using System;

namespace Hydra.Net.RPC
{
	public class RPCBinaryCommand : RPCCommand
	{
		public byte[] Datas { get; set; }

		public RPCBinaryCommand() : base(RPCCommandType.Binary)
		{
		}

		public RPCBinaryCommand(byte[] datas) : base(RPCCommandType.Binary)
		{
			this.Datas = datas;
		}

		public override void Read(RPCDataSerializer reader)
		{
			base.Read(reader);
			this.Datas = reader.ReadBytes();
		}

		public override void Write(RPCDataSerializer writer)
		{
			base.Write(writer);
			writer.WriteBytes(this.Datas);
		}
	}
}
