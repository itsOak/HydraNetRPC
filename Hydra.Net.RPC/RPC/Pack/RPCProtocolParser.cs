using System;
using System.Collections.Generic;

namespace Hydra.Net.RPC
{
	public class RPCProtocolParser
	{
		private List<byte> _buffer = new List<byte>();

		private int[] _headNext = new int[4];

		private byte[] _head = new byte[]
		{
			154,
			154,
			240,
			240
		};

		public RPCProtocolParser()
		{
			this.Next(this._head, this._headNext, 4);
		}

		public void Push(byte[] datas)
		{
			try
			{
				if (this._buffer != null && datas != null)
				{
					this._buffer.AddRange(datas);
				}
			}
			catch (Exception ex)
			{
				HydraLog.WriteLine("添加已接收数据失败！" + ex.Message, LogLevelType.INFO, 1);
			}
		}

		public void Parse(RPCClient client)
		{
            try
            {
                if (this._buffer == null || this._buffer.Count == 0)
                    return;

				while (true)
				{
					int num = this.KMPSearch(this._buffer, this._head, this._headNext, 4);
					if (num < 0)
					{
						this._buffer.Clear();
						continue;
					}
					else
					{
						this._buffer.RemoveRange(0, num);
						int num2 = 0;
						if (this._buffer.Count >= 8)
						{
							num2 = BitConverter.ToInt32(this._buffer.GetRange(4, 4).ToArray(), 0);
							num2 += 16;
						}
						if (!(num2 >= 16 && this._buffer.Count >= 16 && this._buffer.Count >= num2))
						{
							break;
						}
						try
						{
							RPCDataPackage rpcdataPackage = RPCDataPackage.Decode(this._buffer.GetRange(0, num2).ToArray());
							if (rpcdataPackage != null)
							{
								this._buffer.RemoveRange(0, num2);
								RPCCommand rpccommand = rpcdataPackage.BuildCommand();
								if (rpccommand != null && client != null)
								{
									RPCJsonCommand rpcjsonCommand = rpccommand as RPCJsonCommand;
									if (rpcjsonCommand != null)
									{
										client.CmdBus.PostCall(client, rpcjsonCommand.Data);
									}
								}
							}
						}
						catch (Exception ex)
						{
							HydraLog.WriteLine(ex.Message, LogLevelType.INFO, 1);
							this._buffer.RemoveRange(0, 4);
						}
					}
				}
            }
            catch (Exception ex2)
            {
                HydraLog.WriteLine("解析数据包出错！" + ex2.Message, LogLevelType.INFO, 1);
            }
        }

		/// <summary>
		/// 算法来源： https://www.cnblogs.com/zhy2002/archive/2008/03/31/1131794.html
		/// </summary>
		private void Next(byte[] pattern, int[] next, int length)
		{
			next[0] = -1;
			if (length >= 2)
			{
				next[1] = 0;
				int i = 2;
				int j = 0;
				while (i < length)
				{
					if (pattern[i - 1] == pattern[j])
					{
						next[i++] = ++j;
					}
					else
					{
						j = next[j];
						if (j == -1)
						{
							next[i++] = ++j;
						}
					}
				}
			}
		}

		/// <summary>
		/// 算法来源： https://www.cnblogs.com/zhy2002/archive/2008/03/31/1131794.html
		/// </summary>
		private int KMPSearch(List<byte> source, byte[] pattern, int[] next, int length)
		{
			int i = 0;
			int j = 0;
			while (j < length && i < source.Count)
			{
				if (source[i] == pattern[j])
				{
					i++;
					j++;
				}
				else
				{
					j = next[j];
					if (j == -1)
					{
						i++;
						j++;
					}
				}
			}
			return (j < length) ? -1 : (i - j);
		}
	}
}
