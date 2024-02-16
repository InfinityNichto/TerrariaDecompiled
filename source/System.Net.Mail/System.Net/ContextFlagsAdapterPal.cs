namespace System.Net;

internal static class ContextFlagsAdapterPal
{
	private readonly struct ContextFlagMapping
	{
		public readonly global::Interop.SspiCli.ContextFlags Win32Flag;

		public readonly System.Net.ContextFlagsPal ContextFlag;

		public ContextFlagMapping(global::Interop.SspiCli.ContextFlags win32Flag, System.Net.ContextFlagsPal contextFlag)
		{
			Win32Flag = win32Flag;
			ContextFlag = contextFlag;
		}
	}

	private static readonly ContextFlagMapping[] s_contextFlagMapping = new ContextFlagMapping[22]
	{
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptExtendedError, System.Net.ContextFlagsPal.AcceptExtendedError),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.InitManualCredValidation, System.Net.ContextFlagsPal.InitManualCredValidation),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptIntegrity, System.Net.ContextFlagsPal.AcceptIntegrity),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptStream, System.Net.ContextFlagsPal.AcceptStream),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AllocateMemory, System.Net.ContextFlagsPal.AllocateMemory),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AllowMissingBindings, System.Net.ContextFlagsPal.AllowMissingBindings),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.Confidentiality, System.Net.ContextFlagsPal.Confidentiality),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.Connection, System.Net.ContextFlagsPal.Connection),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.Delegate, System.Net.ContextFlagsPal.Delegate),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.InitExtendedError, System.Net.ContextFlagsPal.InitExtendedError),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptIntegrity, System.Net.ContextFlagsPal.AcceptIntegrity),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.InitManualCredValidation, System.Net.ContextFlagsPal.InitManualCredValidation),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptStream, System.Net.ContextFlagsPal.AcceptStream),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptExtendedError, System.Net.ContextFlagsPal.AcceptExtendedError),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.InitUseSuppliedCreds, System.Net.ContextFlagsPal.InitUseSuppliedCreds),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.MutualAuth, System.Net.ContextFlagsPal.MutualAuth),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.ProxyBindings, System.Net.ContextFlagsPal.ProxyBindings),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.ReplayDetect, System.Net.ContextFlagsPal.ReplayDetect),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.SequenceDetect, System.Net.ContextFlagsPal.SequenceDetect),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.UnverifiedTargetName, System.Net.ContextFlagsPal.UnverifiedTargetName),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.UseSessionKey, System.Net.ContextFlagsPal.UseSessionKey),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.Zero, System.Net.ContextFlagsPal.None)
	};

	internal static System.Net.ContextFlagsPal GetContextFlagsPalFromInterop(global::Interop.SspiCli.ContextFlags win32Flags)
	{
		System.Net.ContextFlagsPal contextFlagsPal = System.Net.ContextFlagsPal.None;
		ContextFlagMapping[] array = s_contextFlagMapping;
		for (int i = 0; i < array.Length; i++)
		{
			ContextFlagMapping contextFlagMapping = array[i];
			if ((win32Flags & contextFlagMapping.Win32Flag) == contextFlagMapping.Win32Flag)
			{
				contextFlagsPal |= contextFlagMapping.ContextFlag;
			}
		}
		return contextFlagsPal;
	}

	internal static global::Interop.SspiCli.ContextFlags GetInteropFromContextFlagsPal(System.Net.ContextFlagsPal flags)
	{
		global::Interop.SspiCli.ContextFlags contextFlags = global::Interop.SspiCli.ContextFlags.Zero;
		ContextFlagMapping[] array = s_contextFlagMapping;
		for (int i = 0; i < array.Length; i++)
		{
			ContextFlagMapping contextFlagMapping = array[i];
			if ((flags & contextFlagMapping.ContextFlag) == contextFlagMapping.ContextFlag)
			{
				contextFlags |= contextFlagMapping.Win32Flag;
			}
		}
		return contextFlags;
	}
}
