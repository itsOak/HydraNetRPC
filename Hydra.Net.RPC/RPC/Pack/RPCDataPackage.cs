using System;
using System.Collections.Generic;

namespace Hydra.Net.RPC
{
	public class RPCDataPackage
	{
		private byte[] _head = new byte[]
		{
			154,
			154,
			240,
			240
		};

		private byte[] _tail = new byte[]
		{
			169,
			15,
			169,
			15
		};

		public byte[] Head
		{
			get
			{
				return this._head;
			}
		}

		public byte[] Body { get; set; }

		public int CRC32 { get; private set; }

		public byte[] Tail
		{
			get
			{
				return this._tail;
			}
		}

		internal byte[] GetBytes()
		{
			byte[] result;
			try
			{
				List<byte> list = new List<byte>();
				list.AddRange(this.Head);
				list.AddRange(BitConverter.GetBytes(this.Body.Length));
				list.AddRange(this.Body);
				byte[] bytes = BitConverter.GetBytes(this.CRC32);
				list.AddRange(bytes);
				list.AddRange(this.Tail);
				result = list.ToArray();
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("获取数据包字节数组失败！" + ex.Message);
			}
			return result;
		}

		internal RPCCommand BuildCommand()
		{
			try
			{
				if (null == Body || Body.Length < 2)
				{
					return null;
				}
				RPCCommandType rpccommandType = (RPCCommandType)BitConverter.ToUInt16(this.Body, 0);
				RPCDataSerializer reader = new RPCDataSerializer(this.Body);
				RPCCommand rpccommand = null;
				if (rpccommandType == RPCCommandType.Json)
				{
					rpccommand = new RPCJsonCommand();
				}
				else if (RPCCommandType.Binary == rpccommandType)
			    {
				    rpccommand = new RPCBinaryCommand();
				}
				if (null != rpccommand)
				{
					rpccommand.Read(reader);
				}
				return rpccommand;
			}
			catch (Exception ex)
			{
				HydraLog.WriteLine("构造一个RPC协议包出错！" + ex.Message, LogLevelType.INFO, 1);
			}
			return null;
		}

		public static byte[] Encode(string data)
		{
			byte[] bytes = null;
			try
			{
				RPCDataPackage rpcdataPackage = new RPCDataPackage();
				RPCJsonCommand rpcjsonCommand = new RPCJsonCommand(data);
				RPCDataSerializer rpcdataSerializer = new RPCDataSerializer();
				rpcjsonCommand.Write(rpcdataSerializer);
				rpcdataPackage.Body = rpcdataSerializer.Buffer;
				if (rpcdataPackage.Body == null)
				{
					throw new InvalidOperationException("有效数据为空");
				}
				rpcdataPackage.CRC32 = Hydra.Net.RPC.CRC32.GetCRC(rpcdataPackage.Body);
				bytes = rpcdataPackage.GetBytes();
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Encode失败，" + ex.Message);
			}
			return bytes;
		}

		public static RPCDataPackage Decode(byte[] datas)
		{
			RPCDataPackage rpcdataPackage = new RPCDataPackage();
			try
			{
				if (datas == null || datas.Length < 16)
				{
					return null;
				}
				if (!(rpcdataPackage.Head[0] == datas[0] && rpcdataPackage.Head[1] == datas[1] && rpcdataPackage.Head[2] == datas[2] && rpcdataPackage.Head[3] == datas[3]))
				{
					return null;
				}
				int num = datas.Length;
				if (!(rpcdataPackage.Tail[0] == datas[num - 4] && rpcdataPackage.Tail[1] == datas[num - 3] && rpcdataPackage.Tail[2] == datas[num - 2] && rpcdataPackage.Tail[3] == datas[num - 1]))
				{
					return null;
				}
				num = BitConverter.ToInt32(datas, 4);
				if (num < 0)
				{
					return null;
				}
				List<byte> list = new List<byte>(datas);
				rpcdataPackage.Body = list.GetRange(8, num).ToArray();
				rpcdataPackage.CRC32 = BitConverter.ToInt32(datas, datas.Length - 8);
				int crc = Hydra.Net.RPC.CRC32.GetCRC(rpcdataPackage.Body);
				if (rpcdataPackage.CRC32 != crc)
				{
					return null;
				}
			}
			catch (Exception e)
			{
				HydraLog.WriteLine("Decode失败", e);
				rpcdataPackage = null;
			}
			return rpcdataPackage;
		}
	}
}
