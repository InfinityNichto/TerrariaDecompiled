using System.Linq;

namespace Terraria.Testing.ChatCommands;

public static class ArgumentHelper
{
	public static ArgumentListResult ParseList(string arguments)
	{
		return new ArgumentListResult(from arg in arguments.Split(' ')
			select arg.Trim() into arg
			where arg.Length != 0
			select arg);
	}
}
