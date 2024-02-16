using System.IO;
using System.Reflection;

namespace Microsoft.Xna.Framework;

internal static class TitleLocation
{
	private static string _titleLocation;

	public static string Path
	{
		get
		{
			if (_titleLocation == null)
			{
				string titleLocation = string.Empty;
				Assembly assembly = Assembly.GetEntryAssembly();
				if (assembly == null)
				{
					assembly = Assembly.GetCallingAssembly();
				}
				if (assembly != null)
				{
					titleLocation = System.IO.Path.GetDirectoryName(assembly.Location);
				}
				_titleLocation = titleLocation;
			}
			return _titleLocation;
		}
	}
}
