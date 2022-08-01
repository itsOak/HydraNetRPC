using System;

namespace Hydra.Net.RPC
{
	public abstract class RPCCommand
	{
		public RPCCommandType CmdType { get; private set; }

		protected RPCCommand(RPCCommandType type)
		{
			this.CmdType = type;
		}

		public virtual void Read(RPCDataSerializer reader)
		{
			try
			{
				if (null == reader)
				{
					throw new NullReferenceException("RPC数据序列化读对象为空");
				}
				this.CmdType = (RPCCommandType)reader.ReadUShort();
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("RPC数据序列化读失败，" + ex.Message);
			}
		}

		public virtual void Write(RPCDataSerializer writer)
		{
			try
			{
				if (null == writer)
				{
					throw new NullReferenceException("RPC数据序列化写对象为空");
				}
				writer.WriteUShort((ushort)this.CmdType);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("RPC数据序列化写失败，" + ex.Message);
			}
		}
	}
}
