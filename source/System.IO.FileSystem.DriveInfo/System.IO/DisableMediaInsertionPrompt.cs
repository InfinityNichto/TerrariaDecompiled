namespace System.IO;

internal struct DisableMediaInsertionPrompt : IDisposable
{
	private bool _disableSuccess;

	private uint _oldMode;

	public static System.IO.DisableMediaInsertionPrompt Create()
	{
		System.IO.DisableMediaInsertionPrompt result = default(System.IO.DisableMediaInsertionPrompt);
		result._disableSuccess = global::Interop.Kernel32.SetThreadErrorMode(1u, out result._oldMode);
		return result;
	}

	public void Dispose()
	{
		if (_disableSuccess)
		{
			global::Interop.Kernel32.SetThreadErrorMode(_oldMode, out var _);
		}
	}
}
