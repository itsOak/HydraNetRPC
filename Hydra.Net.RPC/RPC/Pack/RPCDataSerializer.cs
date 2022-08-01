using System;
using System.Collections.Generic;
using System.Text;

namespace Hydra.Net.RPC
{
	public class RPCDataSerializer
	{
		private List<byte> _buffer;

		private int _index = 0;

		public byte[] Buffer
		{
			get
			{
				return (this._buffer == null) ? null : this._buffer.ToArray();
			}
		}

		public RPCDataSerializer()
		{
			this._buffer = new List<byte>();
		}

		public RPCDataSerializer(byte[] buffer)
		{
			this._buffer = new List<byte>(buffer);
		}

		public ushort ReadUShort()
		{
			ushort result = 0;
			try
			{
				if (_index < 0 || _index >= _buffer.Count)
				{
					throw new IndexOutOfRangeException("ReadUShort失败");
				}
				ushort num = BitConverter.ToUInt16(this._buffer.GetRange(this._index, 2).ToArray(), 0);
				_index += 2;
				result = num;
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return result;
		}

		public int ReadInt()
		{
			int result;
			try
			{
				if (this._index < 0 || this._index >= this._buffer.Count)
				{
					throw new IndexOutOfRangeException("ReadInt失败");
				}
				int num = BitConverter.ToInt32(this._buffer.GetRange(this._index, 4).ToArray(), 0);
				this._index += 4;
				result = num;
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return result;
		}

		public string ReadString()
		{
			string result;
			try
			{
				if (this._index < 0 || this._index >= this._buffer.Count)
				{
					throw new IndexOutOfRangeException("ReadString失败");
				}
				int num = this.ReadInt();
				if (num < 0)
				{
					throw new IndexOutOfRangeException("ReadString失败,字符串长度小于0！");
				}
				string value = Encoding.Unicode.GetString(this._buffer.GetRange(this._index, num).ToArray());
				this._index += num;
				result = value;
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return result;
		}

		public byte[] ReadBytes()
		{
			byte[] result;
			try
			{
				if (this._index < 0 || this._index >= this._buffer.Count)
				{
					throw new IndexOutOfRangeException("ReadBytes失败");
				}
				int num = this.ReadInt();
				if (num < 0)
				{
					throw new IndexOutOfRangeException("ReadBytes失败,长度小于0！");
				}
				byte[] array = this._buffer.GetRange(this._index, num).ToArray();
				this._index += num;
				result = array;
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return result;
		}

		private void Write(string method, Action handler)
		{
			try
			{
				if (this._buffer == null)
				{
					throw new InvalidOperationException("数据缓存区未初始化！");
				}
				handler();
			}
			catch (InvalidOperationException ex)
			{
				throw ex;
			}
			catch (Exception ex2)
			{
				throw new InvalidOperationException(string.Format("{0}失败，{1}", method, ex2.Message));
			}
		}

		public void WriteUShort(ushort value)
		{
			this.Write("WriteUShort", delegate
			{
				this._buffer.AddRange(BitConverter.GetBytes(value));
			});
		}

		public void WriteInt(int value)
		{
			this.Write("WriteInt", delegate
			{
				this._buffer.AddRange(BitConverter.GetBytes(value));
			});
		}

		public void WriteString(string value)
		{
			this.Write("WriteString", delegate
			{
				byte[] bytes = Encoding.Unicode.GetBytes(value);
				this.WriteInt(bytes.Length);
				this._buffer.AddRange(bytes);
			});
		}

		public void WriteBytes(byte[] value)
		{
			this.Write("WriteBytes", delegate
			{
				this.WriteInt(value.Length);
				this._buffer.AddRange(value);
			});
		}
	}
}
