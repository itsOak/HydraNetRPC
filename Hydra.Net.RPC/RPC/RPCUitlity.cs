using System;

namespace Hydra.Net.RPC
{
	public class RPCUitlity
	{
		public static string GetBadResponse(string request, int errCode, string errMsg)
		{
			return ToACK(request, (errCode == 0) ? -1 : errCode, string.IsNullOrWhiteSpace(errMsg) ? "请求操作失败" : errMsg, null);
		}

		public static string GetOKResponse(string request, object info)
		{
			return ToACK(request, 0, "", info);
		}

		private static string ToACK(string request, int errCode, string errMsg, object info)
		{
			var jsonValue = JsonValue.Parse(request);
			if (jsonValue == null)
			{
				HydraLog.Throw("生成失败响应失败，原因： 不是一个有效的json数据{0}", request);
			}
			int id = jsonValue.AsInt("id");
			string method = jsonValue.AsString("method");
			string result;
			if (errCode == 0)
			{
				result = JsonValue.Format(new
				{
					id = id,
					method = method,
					result = true,
					info = info
				});
			}
			else
			{
				result = JsonValue.Format(new
				{
					id = id,
					method = method,
					result = false,
					error = new
					{
						errCode,
						errMsg
					}
				});
			}
			return result;
		}

		public static string ToACK(JsonValue request, int errCode = 0, string errMsg = "", object info = null, bool bAlwaysUseInfo = false)
		{
			if (request == null)
			{
				HydraLog.Throw("应答对应的请求信令为空", new object[0]);
			}
			int id = request.AsInt("id");
			string method = request.AsString("method");
			string result;
			if (errCode == 0)
			{
				result = JsonValue.Format(new
				{
					id = id,
					method = method,
					result = true,
					info = info
				});
			}
			else
			{
				result = JsonValue.Format(new
				{
					id = id,
					method = method,
					result = false,
					error = new
					{
						errCode,
						errMsg
					},
					info = (bAlwaysUseInfo ? info : "")
				});
			}
			return result;
		}

		public static string ToACK_OK(JsonValue request, object info = null)
		{
			return ToACK(request, 0, "", info, false);
		}

		public static string IsOK(string response)
		{
			var jsonValue = JsonValue.Parse(response);
			string result = null;
			if (jsonValue == null)
			{
				result = "解析json字符串失败";
			}
			else
			{
				if (!jsonValue.AsBool("result"))
				{
					string errMsg = jsonValue.AsString("error.errMsg");
					if (null == errMsg)
					{
						return "未定义错误";
					}
				}
				result = null;
			}
			return result;
		}
	}
}
