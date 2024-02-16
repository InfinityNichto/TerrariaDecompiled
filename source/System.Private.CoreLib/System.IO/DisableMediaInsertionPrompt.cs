namespace System.IO;

internal struct DisableMediaInsertionPrompt : IDisposable
{
	private bool _disableSuccess;

	private uint _oldMode;

	public static DisableMediaInsertionPrompt Create()
	{
		DisableMediaInsertionPrompt result = default(DisableMediaInsertionPrompt);
		result._disableSuccess = Interop.Kernel32.SetThreadErrorMode(1u, out result._oldMode);
		return result;
	}

	public void Dispose()
	{
		if (_disableSuccess)
		{
			Interop.Kernel32.SetThreadErrorMode(_oldMode, out var _);
		}
	}
}
