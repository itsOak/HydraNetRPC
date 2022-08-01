using System;
using System.Collections.Generic;
using System.Threading;

namespace Hydra.Net.RPC
{
	public class CommandBus
	{
		private Dictionary<string, Func<string, JsonValue, string>> _handlers = null;

		public CommandBus()
        {
        }

		public void Register(string cmd, Func<string, JsonValue, string> handler)
		{
			if (null == handler)
			{
				throw new ArgumentException(string.Format("注册信令失败，原因：信令{0}的处理方法为null！", cmd));
			}
			if (null == _handlers)
			{
				this._handlers = new Dictionary<string, Func<string, JsonValue, string>>();
			}
			if (_handlers.ContainsKey(cmd))
			{
				throw new ArgumentException(string.Format("注册信令失败，原因：信令{0}已存在！", cmd));
			}
			_handlers[cmd] = handler;
		}

		private void Call(RPCClient client, string jsonData, bool bSync)
		{
			DateTime startTime = DateTime.Now;
			HydraLog.Test("Enter Call: {0}", new object[]
			{
				jsonData
			});
			if (null != _handlers && _handlers.Count > 0)
			{
				JsonValue value = JsonValue.Parse(jsonData);
				bool flag2 = value == null;
				if (flag2)
				{
					HydraLog.WriteLine("解析json信令失败", LogLevelType.INFO, 1);
				}
				else
				{
					int num = value.AsInt("id");
					string text = value.AsString("method");
					string itemId = string.Format("{0}@{1}", num, text);
					HydraLog.Info("Begin Call: {0}", new object[]
					{
						itemId
					});
					if (!client.IsAtServerSide)
					{
						RPCSyncCmdMgt.Instance.Singal(itemId, jsonData);
					}
					if (_handlers.ContainsKey(text))
					{
						Func<string, JsonValue, string> handler = this._handlers[text];
						if (null != handler)
						{
							if (bSync)
							{
								this.Invoke(handler, client, value, itemId, startTime);
							}
							else
							{
								ThreadPool.QueueUserWorkItem(delegate(object state)
								{
									this.Invoke(handler, client, value, itemId, startTime);
								});
							}
						}
					}
					else
					{
						HydraLog.Warn("End Call: {0}, this cmd not support! Total Cost: {1} ms", new object[]
						{
							itemId,
							(DateTime.Now - startTime).TotalMilliseconds
						});
					}
				}
			}
		}

		private void Invoke(Func<string, JsonValue, string> handler, RPCClient client, JsonValue req, string itemId, DateTime startTime)
		{
			try
			{
				string text = handler(client.SessionID, req);
				if (!string.IsNullOrWhiteSpace(text))
				{
					if (client.Send(text))
					{
						HydraLog.Info("Reply Call: {0}, msg = {1}", new object[]
						{
							itemId,
							(text.Length > 128) ? text.Substring(0, 128) : text
						});
					}
					else
					{
						HydraLog.Error("Reply Call: {0}, send fail", new object[]
						{
							itemId
						});
					}
				}
				else
				{
					HydraLog.Test("Invoke {0} Skip，Warning： msg is null or empty", new object[]
					{
						itemId
					});
				}
			}
			catch (Exception ex)
			{
				HydraLog.Error("Invoke {0} Fail，Error：{1}", new object[]
				{
					itemId,
					ex.StackTrace
				});
				if (null != client)
				{
					client.Send(RPCUitlity.ToACK(req, -1, ex.Message, null, false));
				}
			}
			HydraLog.Info("End Call: {0}, Total Cost: {1} ms", new object[]
			{
				itemId,
				(DateTime.Now - startTime).TotalMilliseconds
			});
		}

		public void SyncCall(RPCClient client, string jsonData)
		{
			this.Call(client, jsonData, true);
		}

		public void PostCall(RPCClient client, string jsonData)
		{
			this.Call(client, jsonData, false);
		}

		
	}
}
