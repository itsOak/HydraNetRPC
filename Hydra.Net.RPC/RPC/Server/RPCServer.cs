using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Hydra.Net.RPC
{
	public abstract class RPCServer
	{
		private static object _lockSessionObj = new object();

		internal List<RPCSession> Sessions { get; set; }

		public string IP { get; set; }

		public int Port { get; set; }

		internal TcpListener Listener { get; set; }

		internal CommandBus CmdBus { get; private set; }

		protected RPCServer()
		{
			this.Sessions = new List<RPCSession>();
			this.CmdBus = new CommandBus();
			this.Register();
		}
		protected abstract void Register();

		public bool Start(string ip, int port)
		{
			bool result = false;
			if (port < 0 || port >= ushort.MaxValue)
			{
				HydraLog.Error("启动RPC服[{0}:{1}]务失败，无效端口号: {1}", new object[]
				{
					ip,
					port
				});
				result = false;
			}
			else
			{
				IPAddress localaddr = null;
				if (!IPAddress.TryParse(ip, out localaddr))
				{
					HydraLog.Error("启动RPC服务[{0}:{1}]失败，无效的IP: {0}]", new object[]
					{
						ip,
						port
					});
					result = false;
				}
				else
				{
					if (null != Listener)
					{
						result = true;
					}
					else
					{
						try
						{
							Listener = new TcpListener(localaddr, port);
							Listener.Start();
							Listener.BeginAcceptTcpClient(new AsyncCallback(this.OnConnect), this);
							IP = ip;
							if (0 == port)
							{
								this.Port = ((IPEndPoint)this.Listener.LocalEndpoint).Port;
							}
							else
							{
								this.Port = port;
							}
							HydraLog.Info("启动RPC服务[{0}:{1}]成功!", new object[]
							{
								this.IP,
								this.Port
							});
						}
						catch (Exception ex)
						{
							HydraLog.Error("启动RPC服务[{0}:{1}]失败！原因：{2}", new object[]
							{
								ip,
								port,
								ex.Message
							});
							return false;
						}
						result = true;
					}
				}
			}
			return result;
		}

		public bool Stop()
		{
			try
			{
				if (null != Listener)
				{
					this.Listener.Stop();
				}
				lock (_lockSessionObj)
				{
					if (this.Sessions != null)
					{
						this.Sessions.ForEach(delegate(RPCSession session)
						{
							session.Close();
						});
						this.Sessions.Clear();
					}
				}
				HydraLog.WriteLine("停止RPC服务成功!", LogLevelType.INFO, 1);
			}
			catch (Exception ex)
			{
				HydraLog.WriteLine("停止RPC服务失败！" + ex.Message, LogLevelType.INFO, 1);
			}
			return true;
		}

		public void Reply(string sessionId, string jsonData)
		{
			if (string.IsNullOrWhiteSpace(jsonData))
			{
				HydraLog.Error("发送应答内容失败，需要会话{0}发送的内容为空", new object[]
				{
					sessionId
				});
			}
			else
			{
				lock (_lockSessionObj)
				{
					if (null != this.Sessions && this.Sessions.Count > 0)
					{
						if (string.IsNullOrWhiteSpace(sessionId))
						{
							this.Sessions.ForEach(delegate(RPCSession session)
							{
								session.Notify(jsonData);
							});
						}
						else
						{
							RPCSession rpcsession = this.Sessions.FirstOrDefault((RPCSession s) => s.SessionID == sessionId);
							if (rpcsession != null)
							{
								rpcsession.Notify(jsonData);
							}
						}
					}
				}
			}
		}

		public RPCSession GetSession(string sessionId)
		{
			RPCSession result = null;
			lock (_lockSessionObj)
			{
				result = this.Sessions.FirstOrDefault((RPCSession s) => sessionId == s.SessionID);
			}
			return result;
		}

		public int GetSessionCount()
		{
			int result = 0;
			lock (_lockSessionObj)
			{
				result = this.Sessions.Count;
			}
			return result;
		}

		private void OnConnect(IAsyncResult ar)
		{
			try
			{
				RPCServer rpcserver = ar.AsyncState as RPCServer;
				if (rpcserver != null && rpcserver.Listener != null)
				{
					TcpClient tcpClient = rpcserver.Listener.EndAcceptTcpClient(ar);
					if (tcpClient != null)
					{
						RPCSession rpcsession = new RPCSession(rpcserver, tcpClient);
						lock (_lockSessionObj)
						{
							rpcsession.SessionClosed += this.OnClose;
							this.Sessions.Add(rpcsession);
						}
						HydraLog.Info("New RPC Client From {0} sessionId:{1} Connectd!", new object[]
						{
							tcpClient.Client.RemoteEndPoint.ToString(),
							rpcsession.SessionID
						});
					}
					rpcserver.Listener.BeginAcceptTcpClient(this.OnConnect, rpcserver);
				}
			}
			catch (Exception ex)
			{
				HydraLog.WriteLine("接收RPC客户端连接请求异常！" + ex.Message, LogLevelType.INFO, 1);
			}
		}

		public void OnClose(object client, string sessionId)
		{
			lock (_lockSessionObj)
			{
				if (this.Sessions != null)
				{
					RPCSession rpcsession = this.Sessions.FirstOrDefault((RPCSession s) => s.SessionID == sessionId);
					if (rpcsession != null)
					{
						this.Sessions.Remove(rpcsession);
					}
					HydraLog.Warn("RPC Session[{0}] Closed! Now Exist Total Sesssions: {1}", new object[]
					{
						sessionId,
						this.Sessions.Count
					});
				}
			}
		}

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
	}
}
