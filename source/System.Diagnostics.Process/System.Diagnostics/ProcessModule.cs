using System.ComponentModel;

namespace System.Diagnostics;

[Designer("System.Diagnostics.Design.ProcessModuleDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public class ProcessModule : Component
{
	private FileVersionInfo _fileVersionInfo;

	public string? ModuleName { get; internal set; }

	public string? FileName { get; internal set; }

	public IntPtr BaseAddress { get; internal set; }

	public int ModuleMemorySize { get; internal set; }

	public IntPtr EntryPointAddress { get; internal set; }

	public FileVersionInfo FileVersionInfo => _fileVersionInfo ?? (_fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(FileName));

	internal ProcessModule()
	{
	}

	public override string ToString()
	{
		return base.ToString() + " (" + ModuleName + ")";
	}
}
