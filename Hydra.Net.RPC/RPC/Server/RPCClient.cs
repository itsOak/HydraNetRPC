using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Hydra.Net.RPC
{
	public abstract class RPCClient
	{
		private byte[] _buffer = new byte[256];

		private int _requestId = 1;

		internal byte[] Buffer
		{
			get
			{
				return this._buffer;
			}
		}

		public int RequestID
		{
			get
			{
				int num = 0;
				lock (this)
				{
					int requestId = this._requestId;
					this._requestId = requestId + 1;
					num = requestId;
					bool flag2 = (double)num > 100000000.0;
					if (flag2)
					{
						this._requestId = 1;
					}
				}
				return num;
			}
		}

		internal TcpClient EndPoint { get; set; }

		internal RPCProtocolParser Parser { get; private set; }
		
		internal CommandBus CmdBus { get; set; }

		internal bool IsAtServerSide { get; set; }

		public string ServerIP { get; set; }

		public int Port { get; set; }

		public bool IsConnected
		{
			get
			{
				return this.EndPoint != null && this.EndPoint.Connected;
			}
		}

		public string SessionID { get; private set; }

		protected RPCClient()
		{
			this.Parser = new RPCProtocolParser();
			this.CmdBus = new CommandBus();
			this.Register();
		}

		protected abstract void Register();

		protected void Attach(string cmd, Func<string, JsonValue, string> handler)
		{
			if (string.IsNullOrWhiteSpace(cmd))
			{
				HydraLog.Throw("请求方法名称为空", new object[]
				{
					cmd
				});
			}
			if (handler == null)
			{
				HydraLog.Throw("请求的处理方法委托为空对象", new object[0]);
			}
			this.CmdBus.Register(cmd, handler);
		}

		public bool Connect(string ip, int port)
		{
			bool result;
			if (port <= 0 || port >= ushort.MaxValue)
			{
				HydraLog.Error("连接到RPC服务失败，无效端口号: {0}", new object[]
				{
					port
				});
				result = false;
			}
			else
			{
				IPAddress address;
				if (!IPAddress.TryParse(ip, out address))
				{
					HydraLog.Error("连接到RPC服务失败，无效的IP: {0}]", new object[]
					{
						ip
					});
					result = false;
				}
				else
				{
					if (this.EndPoint == null)
					{
						this.EndPoint = new TcpClient();
					}
					if (!this.IsConnected)
					{
						try
						{
							this.EndPoint.Connect(address, port);
							this.ServerIP = ip;
							this.Port = port;
							NetworkStream stream = this.EndPoint.GetStream();
							if (stream != null)
							{
								this.SessionID = this.EndPoint.Client.Handle.ToString();
								stream.BeginRead(this.Buffer, 0, this.Buffer.Length, new AsyncCallback(this.OnData), this);
							}
						}
						catch (Exception ex)
						{
							this.ServerIP = "";
							this.Port = 0;
							this.SessionID = "";
							HydraLog.Error("连接到目标RPC服务端[{0}：{1}]发生错误：{2}", new object[]
							{
								ip,
								port,
								ex.Message
							});
						}
					}
					result = this.IsConnected;
				}
			}
			return result;
		}

		public void DisConnect()
		{
			HydraLog.Warn("RPC Client sessionId:{0} Disconnectd!", new object[]
			{
				this.SessionID
			});
			if (this.IsConnected)
			{
				this.EndPoint.Close();
			}
			this.EndPoint = null;
		}

		public string SyncCall(object requestObj, int nTimeout = 5)
		{
			string text = JsonValue.Format(new
			{
				result = false,
				error = new
				{
					errCode = 0,
					errMsg = "序列化请求失败"
				}
			});
			string req = JsonValue.Format(requestObj);
			string result = null;
			if (string.IsNullOrWhiteSpace(req))
			{
				result = text;
			}
			else
			{
				JsonValue jsonValue = JsonValue.Parse(req);
				if (null == jsonValue)
				{
					result = text;
				}
				else
				{
					string reqCmdId = string.Format("{0}@{1}", jsonValue.AsInt("id"), jsonValue.AsString("method"));
					SyncCallItem syncCallItem = new SyncCallItem(reqCmdId, req, nTimeout);
					RPCSyncCmdMgt.Instance.Add(syncCallItem);
					syncCallItem.ExpireTime = DateTime.Now.AddSeconds((double)nTimeout);
					HydraLog.Test("send " + reqCmdId, new object[0]);
					if (!this.Send(req))
					{
						RPCSyncCmdMgt.Instance.Remove(reqCmdId);
						result = RPCUitlity.ToACK(jsonValue, -1, "发送到服务端失败", null, false);
					}
					else
					{
						syncCallItem.Singal.WaitOne(TimeSpan.FromSeconds(30.0));
						result = syncCallItem.Response;
					}
				}
			}
			return result;
		}

		public bool PostCall(object requestObj, int nTimeout = 5)
		{
			string req = JsonValue.Format(requestObj);
			bool result = false;
			if (string.IsNullOrWhiteSpace(req))
			{
				result = false;
			}
			else
			{
				JsonValue jsonValue = JsonValue.Parse(req);
				if (jsonValue == null)
				{
					result = false;
				}
				else
				{
					string reqCmdId = string.Format("{0}@{1}", jsonValue.AsInt("id"), jsonValue.AsString("method"));
					PostCallItem postCallItem = new PostCallItem(this, reqCmdId, req, nTimeout);
					RPCSyncCmdMgt.Instance.Add(postCallItem);
					postCallItem.ExpireTime = DateTime.Now.AddSeconds((double)nTimeout);
					HydraLog.Test("send " + reqCmdId, new object[0]);
					if (!this.Send(req))
					{
						RPCSyncCmdMgt.Instance.Remove(reqCmdId);
						result = false;
					}
					else
					{
						result = true;
					}
				}
			}
			return result;
		}

		public string GetRemoteEndpoint()
		{
			return (this.EndPoint != null && this.EndPoint.Client != null) ? this.EndPoint.Client.RemoteEndPoint.ToString() : "";
		}

        protected bool BeginRead(TcpClient client)
		{
			try
			{
				if (null == client)
				{
					return false;
				}
				this.EndPoint = client;
				NetworkStream stream = this.EndPoint.GetStream();
				if (null != stream)
				{
					this.SessionID = this.EndPoint.Client.Handle.ToString();
					stream.BeginRead(this.Buffer, 0, this.Buffer.Length, new AsyncCallback(this.OnData), this);
					return true;
				}
			}
			catch (Exception ex)
			{
				this.SessionID = "";
				HydraLog.Error("开始接收数据发生错误：{0}", new object[]
				{
					ex.Message
				});
			}
			return false;
		}

		private void OnData(IAsyncResult ar)
		{
			RPCClient rpcclient = ar.AsyncState as RPCClient;
			if (null != rpcclient && null != rpcclient.EndPoint)
			{
				try
				{
					NetworkStream stream = rpcclient.EndPoint.GetStream();
					if (null != stream)
					{
						int num = stream.EndRead(ar);
						if (num > 0)
						{
							rpcclient.Parser.Push(rpcclient.Buffer.Take(num).ToArray<byte>());
							if (!stream.DataAvailable)
							{
								rpcclient.Parser.Parse(rpcclient);
							}
							stream.BeginRead(rpcclient.Buffer, 0, rpcclient.Buffer.Length, OnData, rpcclient);
						}
						else
						{
							rpcclient.NotifySessionClosed();
							rpcclient.DisConnect();
						}
					}
				}
				catch (IOException ex)
				{
					HydraLog.Error("接收服务端响应数据发生异常，原因：{0}", new object[]
					{
						ex.Message
					});
					rpcclient.NotifySessionClosed();
					rpcclient.DisConnect();
				}
				catch (ObjectDisposedException ex2)
				{
					HydraLog.Warn("当前客户端主动断开与服务器的连接！", new object[]
					{
						ex2.Message
					});
				}
				catch (Exception ex3)
				{
					HydraLog.Error("接收服务端响应数据发生异常，原因：{0}", new object[]
					{
						ex3.Message
					});
					rpcclient.NotifySessionClosed();
					rpcclient.DisConnect();
				}
			}
		}

		internal bool Send(string cmd)
		{
			bool result = false;
			if (null == EndPoint || string.IsNullOrWhiteSpace(cmd))
			{
				result = false;
			}
			else
			{
				try
				{
					NetworkStream stream = this.EndPoint.GetStream();
					if (null == stream)
					{
						return false;
					}
					byte[] array = RPCDataPackage.Encode(cmd);
					if (array.Length > 0)
					{
						stream.Write(array, 0, array.Length);
						return true;
					}
				}
				catch (Exception ex)
				{
					HydraLog.Error("发送数据到RPC服务端失败，原因：" + ex.Message, new object[0]);
				}
				result = false;
			}
			return result;
		}

		#region 事件
		public event Action<object, string> SessionClosed;

		private void NotifySessionClosed()
		{
			bool flag = this.SessionClosed != null;
			if (flag)
			{
				this.SessionClosed(this, this.SessionID);
			}
		}
		#endregion
	}
}
