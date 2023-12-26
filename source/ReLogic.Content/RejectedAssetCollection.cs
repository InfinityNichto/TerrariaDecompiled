using System.Collections.Generic;

namespace ReLogic.Content;

public class RejectedAssetCollection
{
	private Dictionary<string, IRejectionReason> _rejectedAssetsAndReasons = new Dictionary<string, IRejectionReason>();

	public void Reject(string assetPath, IRejectionReason reason)
	{
		lock (_rejectedAssetsAndReasons)
		{
			_rejectedAssetsAndReasons.Add(assetPath, reason);
		}
	}

	public bool IsRejected(string assetPath)
	{
		lock (_rejectedAssetsAndReasons)
		{
			return _rejectedAssetsAndReasons.ContainsKey(assetPath);
		}
	}

	public void Clear()
	{
		lock (_rejectedAssetsAndReasons)
		{
			_rejectedAssetsAndReasons.Clear();
		}
	}

	public bool TryGetRejections(List<string> rejectionReasons)
	{
		lock (_rejectedAssetsAndReasons)
		{
			foreach (KeyValuePair<string, IRejectionReason> rejectedAssetsAndReason in _rejectedAssetsAndReasons)
			{
				rejectionReasons.Add(rejectedAssetsAndReason.Value.GetReason());
			}
		}
		return _rejectedAssetsAndReasons.Count > 0;
	}
}
