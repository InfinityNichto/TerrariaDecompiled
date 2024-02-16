namespace System.Diagnostics;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = false)]
public sealed class DebuggableAttribute : Attribute
{
	[Flags]
	public enum DebuggingModes
	{
		None = 0,
		Default = 1,
		DisableOptimizations = 0x100,
		IgnoreSymbolStoreSequencePoints = 2,
		EnableEditAndContinue = 4
	}

	public bool IsJITTrackingEnabled => (DebuggingFlags & DebuggingModes.Default) != 0;

	public bool IsJITOptimizerDisabled => (DebuggingFlags & DebuggingModes.DisableOptimizations) != 0;

	public DebuggingModes DebuggingFlags { get; }

	public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled)
	{
		DebuggingFlags = DebuggingModes.None;
		if (isJITTrackingEnabled)
		{
			DebuggingFlags |= DebuggingModes.Default;
		}
		if (isJITOptimizerDisabled)
		{
			DebuggingFlags |= DebuggingModes.DisableOptimizations;
		}
	}

	public DebuggableAttribute(DebuggingModes modes)
	{
		DebuggingFlags = modes;
	}
}
