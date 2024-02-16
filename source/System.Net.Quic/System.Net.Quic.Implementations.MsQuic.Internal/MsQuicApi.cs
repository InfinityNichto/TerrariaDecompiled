using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal sealed class MsQuicApi
{
	private static readonly Version MinWindowsVersion;

	public SafeMsQuicRegistrationHandle Registration { get; }

	internal static MsQuicApi Api { get; }

	internal static bool IsQuicSupported { get; }

	internal MsQuicNativeMethods.RegistrationOpenDelegate RegistrationOpenDelegate { get; }

	internal MsQuicNativeMethods.RegistrationCloseDelegate RegistrationCloseDelegate { get; }

	internal MsQuicNativeMethods.ConfigurationOpenDelegate ConfigurationOpenDelegate { get; }

	internal MsQuicNativeMethods.ConfigurationCloseDelegate ConfigurationCloseDelegate { get; }

	internal MsQuicNativeMethods.ConfigurationLoadCredentialDelegate ConfigurationLoadCredentialDelegate { get; }

	internal MsQuicNativeMethods.ListenerOpenDelegate ListenerOpenDelegate { get; }

	internal MsQuicNativeMethods.ListenerCloseDelegate ListenerCloseDelegate { get; }

	internal MsQuicNativeMethods.ListenerStartDelegate ListenerStartDelegate { get; }

	internal MsQuicNativeMethods.ListenerStopDelegate ListenerStopDelegate { get; }

	internal MsQuicNativeMethods.ConnectionOpenDelegate ConnectionOpenDelegate { get; }

	internal MsQuicNativeMethods.ConnectionCloseDelegate ConnectionCloseDelegate { get; }

	internal MsQuicNativeMethods.ConnectionShutdownDelegate ConnectionShutdownDelegate { get; }

	internal MsQuicNativeMethods.ConnectionStartDelegate ConnectionStartDelegate { get; }

	internal MsQuicNativeMethods.ConnectionSetConfigurationDelegate ConnectionSetConfigurationDelegate { get; }

	internal MsQuicNativeMethods.StreamOpenDelegate StreamOpenDelegate { get; }

	internal MsQuicNativeMethods.StreamCloseDelegate StreamCloseDelegate { get; }

	internal MsQuicNativeMethods.StreamStartDelegate StreamStartDelegate { get; }

	internal MsQuicNativeMethods.StreamShutdownDelegate StreamShutdownDelegate { get; }

	internal MsQuicNativeMethods.StreamSendDelegate StreamSendDelegate { get; }

	internal MsQuicNativeMethods.StreamReceiveCompleteDelegate StreamReceiveCompleteDelegate { get; }

	internal MsQuicNativeMethods.StreamReceiveSetEnabledDelegate StreamReceiveSetEnabledDelegate { get; }

	internal MsQuicNativeMethods.SetCallbackHandlerDelegate SetCallbackHandlerDelegate { get; }

	internal MsQuicNativeMethods.SetParamDelegate SetParamDelegate { get; }

	internal MsQuicNativeMethods.GetParamDelegate GetParamDelegate { get; }

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicRegistrationHandle))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicConfigurationHandle))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicListenerHandle))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicConnectionHandle))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(SafeMsQuicStreamHandle))]
	private unsafe MsQuicApi(MsQuicNativeMethods.NativeApi* vtable)
	{
		SetParamDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetParamDelegate>(vtable->SetParam);
		GetParamDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.GetParamDelegate>(vtable->GetParam);
		SetCallbackHandlerDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.SetCallbackHandlerDelegate>(vtable->SetCallbackHandler);
		RegistrationOpenDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationOpenDelegate>(vtable->RegistrationOpen);
		RegistrationCloseDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.RegistrationCloseDelegate>(vtable->RegistrationClose);
		ConfigurationOpenDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConfigurationOpenDelegate>(vtable->ConfigurationOpen);
		ConfigurationCloseDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConfigurationCloseDelegate>(vtable->ConfigurationClose);
		ConfigurationLoadCredentialDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConfigurationLoadCredentialDelegate>(vtable->ConfigurationLoadCredential);
		ListenerOpenDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerOpenDelegate>(vtable->ListenerOpen);
		ListenerCloseDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerCloseDelegate>(vtable->ListenerClose);
		ListenerStartDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStartDelegate>(vtable->ListenerStart);
		ListenerStopDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ListenerStopDelegate>(vtable->ListenerStop);
		ConnectionOpenDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionOpenDelegate>(vtable->ConnectionOpen);
		ConnectionCloseDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionCloseDelegate>(vtable->ConnectionClose);
		ConnectionSetConfigurationDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionSetConfigurationDelegate>(vtable->ConnectionSetConfiguration);
		ConnectionShutdownDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionShutdownDelegate>(vtable->ConnectionShutdown);
		ConnectionStartDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.ConnectionStartDelegate>(vtable->ConnectionStart);
		StreamOpenDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamOpenDelegate>(vtable->StreamOpen);
		StreamCloseDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamCloseDelegate>(vtable->StreamClose);
		StreamStartDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamStartDelegate>(vtable->StreamStart);
		StreamShutdownDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamShutdownDelegate>(vtable->StreamShutdown);
		StreamSendDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamSendDelegate>(vtable->StreamSend);
		StreamReceiveCompleteDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamReceiveCompleteDelegate>(vtable->StreamReceiveComplete);
		StreamReceiveSetEnabledDelegate = Marshal.GetDelegateForFunctionPointer<MsQuicNativeMethods.StreamReceiveSetEnabledDelegate>(vtable->StreamReceiveSetEnabled);
		MsQuicNativeMethods.RegistrationConfig config = new MsQuicNativeMethods.RegistrationConfig
		{
			AppName = ".NET",
			ExecutionProfile = QUIC_EXECUTION_PROFILE.QUIC_EXECUTION_PROFILE_LOW_LATENCY
		};
		SafeMsQuicRegistrationHandle registrationContext;
		uint status = RegistrationOpenDelegate(ref config, out registrationContext);
		QuicExceptionHelpers.ThrowIfFailed(status, "RegistrationOpen failed.");
		Registration = registrationContext;
	}

	unsafe static MsQuicApi()
	{
		MinWindowsVersion = new Version(10, 0, 20145, 1000);
		Api = null;
		if (OperatingSystem.IsWindows() && !IsWindowsVersionSupported())
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Current Windows version ({Environment.OSVersion}) is not supported by QUIC. Minimal supported version is {MinWindowsVersion}", ".cctor");
			}
		}
		else
		{
			if (!NativeLibrary.TryLoad("msquic.dll", typeof(MsQuicApi).Assembly, DllImportSearchPath.AssemblyDirectory, out var handle))
			{
				return;
			}
			try
			{
				if (NativeLibrary.TryGetExport(handle, "MsQuicOpenVersion", out var address))
				{
					Unsafe.SkipInit(out MsQuicNativeMethods.NativeApi* vtable);
					uint status = ((delegate* unmanaged[Cdecl]<uint, out MsQuicNativeMethods.NativeApi*, uint>)(void*)address)(1u, out vtable);
					if (MsQuicStatusHelper.SuccessfulStatusCode(status))
					{
						IsQuicSupported = true;
						Api = new MsQuicApi(vtable);
					}
				}
			}
			finally
			{
				if (!IsQuicSupported)
				{
					NativeLibrary.Free(handle);
				}
			}
		}
	}

	private static bool IsWindowsVersionSupported()
	{
		return OperatingSystem.IsWindowsVersionAtLeast(MinWindowsVersion.Major, MinWindowsVersion.Minor, MinWindowsVersion.Build, MinWindowsVersion.Revision);
	}
}
