using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hydra.Net.RPC
{
	public class JsonValue
	{
		private JObject _json = null;

		public JsonValue this[string path]
		{
			get
			{
				JsonValue result = null;
				JToken jtoken = this._json.SelectToken(path);
				bool flag = jtoken != null;
				if (flag)
				{
					result = new JsonValue((JObject)jtoken);
				}
				return result;
			}
		}

		public JsonValue(JObject json)
		{
			this._json = json;
		}

		public static JsonValue Parse(string jsonData)
		{
			JsonValue result = null;
			try
			{
				result = new JsonValue(JObject.Parse(jsonData));
			}
			catch (Exception ex)
			{
				HydraLog.Error("解析json字符串失败：{0}，原因：{1}", new object[]
				{
					jsonData,
					ex.Message
				});
				result = null;
			}
			return result;
		}

		public static JsonValue ParseFromFile(string file)
		{
			JsonValue jsonValue = null;
			bool flag = !File.Exists(file);
			JsonValue result;
			if (flag)
			{
				HydraLog.WriteLine("json文件不存在", LogLevelType.WARN, 1);
				result = jsonValue;
			}
			else
			{
				try
				{
					using (StreamReader streamReader = new StreamReader(file))
					{
						using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
						{
							jsonValue = new JsonValue((JObject)JToken.ReadFrom(jsonTextReader));
						}
					}
				}
				catch (Exception ex)
				{
					HydraLog.WriteLine("从文件总解析json失败，原因：" + ex.Message, LogLevelType.ERR, 1);
					jsonValue = null;
				}
				result = jsonValue;
			}
			return result;
		}

		public static string Format(object obj)
		{
			try
			{
				return JObject.FromObject(obj).ToString();
			}
			catch (Exception ex)
			{
				HydraLog.Error("序列化JSON对象{0}失败,原因： {1} ", new object[]
				{
					obj.ToString(),
					ex.Message
				});
			}
			return null;
		}

		public T GetValue<T>(string path)
		{
			T result = default(T);
			if (null != _json)
			{
				try
				{
					result = this._json.SelectToken(path).ToObject<T>();
				}
				catch (Exception)
				{
					result = default(T);
				}
			}
			return result;
		}

		public string AsString(string path)
		{
			return this.GetValue<string>(path);
		}

		public int AsInt(string path)
		{
			return this.GetValue<int>(path);
		}

		public float AsFloat(string path)
		{
			return this.GetValue<float>(path);
		}

		public double AsDouble(string path)
		{
			return this.GetValue<double>(path);
		}

		public bool AsBool(string path)
		{
			return this.GetValue<bool>(path);
		}

		public DateTime AsDateTime(string path)
		{
			string value = this.GetValue<string>(path);
			return DateTime.Parse(value);
		}

		public List<JsonValue> AsArray(string path)
		{
			List<JsonValue> list = new List<JsonValue>();
			JToken jtoken = this._json.SelectToken(path);
			JArray jarray = jtoken as JArray;
			if (jarray != null)
			{
				foreach (JToken jtoken2 in jarray)
				{
					JValue jvalue = jtoken2 as JValue;
					bool flag2 = jvalue == null;
					if (flag2)
					{
						list.Add(new JsonValue((JObject)jtoken2));
					}
				}
			}
			return list;
		}

		public List<string> AsStringArray(string path)
		{
			List<string> list = new List<string>();
			JToken jtoken = this._json.SelectToken(path);
			JArray jarray = jtoken as JArray;
			if (null != jarray)
			{
				foreach (JToken jtoken2 in jarray)
				{
					list.Add(jtoken2.ToObject<string>());
				}
			}
			return list;
		}

		public List<int> AsIntArray(string path)
		{
			List<int> list = new List<int>();
			JToken jtoken = this._json.SelectToken(path);
			JArray jarray = jtoken as JArray;
			if (null != jarray)
			{
				foreach (JToken jtoken2 in jarray)
				{
					list.Add(jtoken2.ToObject<int>());
				}
			}
			return list;
		}

		public List<double> AsDoubleArray(string path)
		{
			List<double> list = new List<double>();
			JToken jtoken = this._json.SelectToken(path);
			JArray jarray = jtoken as JArray;
			if (null != jarray)
			{
				foreach (JToken jtoken2 in jarray)
				{
					list.Add(jtoken2.ToObject<double>());
				}
			}
			return list;
		}

		public void Reset()
		{
			this._json = null;
		}

		public JObject GetJObject()
		{
			return this._json;
		}

		public JToken SelectToken(string path)
		{
			return this._json.SelectToken(path);
		}

		public override string ToString()
		{
			return (this._json != null) ? this._json.ToString() : "";
		}
	}
}
