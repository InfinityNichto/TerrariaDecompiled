namespace System.Net;

internal static class ContextFlagsAdapterPal
{
	private readonly struct ContextFlagMapping
	{
		public readonly global::Interop.SspiCli.ContextFlags Win32Flag;

		public readonly ContextFlagsPal ContextFlag;

		public ContextFlagMapping(global::Interop.SspiCli.ContextFlags win32Flag, ContextFlagsPal contextFlag)
		{
			Win32Flag = win32Flag;
			ContextFlag = contextFlag;
		}
	}

	private static readonly ContextFlagMapping[] s_contextFlagMapping = new ContextFlagMapping[22]
	{
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptExtendedError, ContextFlagsPal.AcceptExtendedError),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.InitManualCredValidation, ContextFlagsPal.InitManualCredValidation),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptIntegrity, ContextFlagsPal.AcceptIntegrity),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptStream, ContextFlagsPal.AcceptStream),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AllocateMemory, ContextFlagsPal.AllocateMemory),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AllowMissingBindings, ContextFlagsPal.AllowMissingBindings),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.Confidentiality, ContextFlagsPal.Confidentiality),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.Connection, ContextFlagsPal.Connection),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.Delegate, ContextFlagsPal.Delegate),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.InitExtendedError, ContextFlagsPal.InitExtendedError),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptIntegrity, ContextFlagsPal.AcceptIntegrity),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.InitManualCredValidation, ContextFlagsPal.InitManualCredValidation),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptStream, ContextFlagsPal.AcceptStream),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.AcceptExtendedError, ContextFlagsPal.AcceptExtendedError),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.InitUseSuppliedCreds, ContextFlagsPal.InitUseSuppliedCreds),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.MutualAuth, ContextFlagsPal.MutualAuth),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.ProxyBindings, ContextFlagsPal.ProxyBindings),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.ReplayDetect, ContextFlagsPal.ReplayDetect),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.SequenceDetect, ContextFlagsPal.SequenceDetect),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.UnverifiedTargetName, ContextFlagsPal.UnverifiedTargetName),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.UseSessionKey, ContextFlagsPal.UseSessionKey),
		new ContextFlagMapping(global::Interop.SspiCli.ContextFlags.Zero, ContextFlagsPal.None)
	};

	internal static ContextFlagsPal GetContextFlagsPalFromInterop(global::Interop.SspiCli.ContextFlags win32Flags)
	{
		ContextFlagsPal contextFlagsPal = ContextFlagsPal.None;
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

	internal static global::Interop.SspiCli.ContextFlags GetInteropFromContextFlagsPal(ContextFlagsPal flags)
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
