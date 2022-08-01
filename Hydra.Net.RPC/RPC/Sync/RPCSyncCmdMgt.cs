using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Hydra.Net.RPC
{
	public class RPCSyncCmdMgt
	{
        #region 静态实例
		private static RPCSyncCmdMgt _instance = null;

		public static RPCSyncCmdMgt Instance
        {
            get
            {
                lock (_lockSyncObj)
                {
                    if (null == _instance)
                    {
                        _instance = new RPCSyncCmdMgt();
                    }
                    return _instance;
                }
            }
        }
		#endregion

		private static object _lockSyncObj = new object();

		private static object _lockSyncItemObj = new object();

		private List<RPCSyncItem> _syncItems = new List<RPCSyncItem>();

		private Timer _timer = new Timer(1000.0)
		{
			Enabled = false
		};

		public string ID { get; set; }

		public RPCSyncCmdMgt()
		{
			this.ID = Guid.NewGuid().ToString();
			this._timer.Elapsed += this._timer_Elapsed;
			this._timer.Enabled = true;
		}

		private void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (_lockSyncItemObj)
			{
				DateTime now = DateTime.Now;
				var expireItems = new List<RPCSyncItem>();
				foreach (var item in this._syncItems)
				{
					if (now >= item.ExpireTime)
					{
						HydraLog.Test("timeout {0}!", item.Id);
						item.OnTimeout(RPCUitlity.GetBadResponse(item.Request, -1, "操作等待超时"));
						expireItems.Add(item);
					}
				}
				foreach (RPCSyncItem item in expireItems)
				{
					this._syncItems.Remove(item);
					HydraLog.Test("remove {0} with timeout", item.Id);
				}
			}
		}

		public void Add(RPCSyncItem item)
		{
			lock (_lockSyncItemObj)
			{
				var rpcsyncItem = this._syncItems.FirstOrDefault((RPCSyncItem sync) => sync.Id == item.Id);
				if (rpcsyncItem == null)
				{
					this._syncItems.Add(item);
					HydraLog.Test("add {0}", item.Id);
				}
			}
		}

		public void Remove(string item)
		{
			lock (_lockSyncItemObj)
			{
				var rpcsyncItem = this._syncItems.FirstOrDefault((RPCSyncItem sync) => sync.Id == item);
				if (rpcsyncItem != null)
				{
					HydraLog.Test("remove {0}", item);
					this._syncItems.Remove(rpcsyncItem);
				}
			}
		}

		public void Singal(string item, string response)
		{
			RPCSyncItem rpcsyncItem = null;
			lock (_lockSyncItemObj)
			{
				rpcsyncItem = this._syncItems.FirstOrDefault((RPCSyncItem sync) => sync.Id == item);
				if (rpcsyncItem != null)
				{
					HydraLog.Test("remove {0}", item);
					this._syncItems.Remove(rpcsyncItem);
				}
				else
				{
					HydraLog.Warn("remove {0} not found", item);
				}
			}
			var syncCallItem = rpcsyncItem as SyncCallItem;
			if (syncCallItem != null)
			{
				syncCallItem.OnTimeout(response);
			}
		}
	}
}
