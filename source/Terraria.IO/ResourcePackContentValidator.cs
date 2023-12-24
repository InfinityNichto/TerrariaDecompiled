using System.IO;
using ReLogic.Content;
using Terraria.GameContent;

namespace Terraria.IO;

public class ResourcePackContentValidator
{
	public void ValidateResourePack(ResourcePack pack)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((int)(AssetReaderCollection)Main.instance.Services.GetService(typeof(AssetReaderCollection)) != 0)
		{
			pack.GetContentSource().GetAllAssetsStartingWith("Images" + Path.DirectorySeparatorChar);
			VanillaContentValidator.Instance.GetValidImageFilePaths();
		}
	}
}
