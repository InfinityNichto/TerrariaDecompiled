using System.ComponentModel;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net;

internal static class SSPIWrapper
{
	internal static System.Net.SecurityPackageInfoClass[] EnumerateSecurityPackages(System.Net.ISSPIInterface secModule)
	{
		if (secModule.SecurityPackages == null)
		{
			lock (secModule)
			{
				if (secModule.SecurityPackages == null)
				{
					int pkgnum = 0;
					System.Net.Security.SafeFreeContextBuffer pkgArray = null;
					try
					{
						int num = secModule.EnumerateSecurityPackages(out pkgnum, out pkgArray);
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(null, $"arrayBase: {pkgArray}", "EnumerateSecurityPackages");
						}
						if (num != 0)
						{
							throw new Win32Exception(num);
						}
						System.Net.SecurityPackageInfoClass[] array = new System.Net.SecurityPackageInfoClass[pkgnum];
						for (int i = 0; i < pkgnum; i++)
						{
							array[i] = new System.Net.SecurityPackageInfoClass(pkgArray, i);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Log.EnumerateSecurityPackages(array[i].Name);
							}
						}
						secModule.SecurityPackages = array;
					}
					finally
					{
						pkgArray?.Dispose();
					}
				}
			}
		}
		return secModule.SecurityPackages;
	}

	internal static System.Net.SecurityPackageInfoClass GetVerifyPackageInfo(System.Net.ISSPIInterface secModule, string packageName, bool throwIfMissing)
	{
		System.Net.SecurityPackageInfoClass[] array = EnumerateSecurityPackages(secModule);
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (string.Equals(array[i].Name, packageName, StringComparison.OrdinalIgnoreCase))
				{
					return array[i];
				}
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.SspiPackageNotFound(packageName);
		}
		if (throwIfMissing)
		{
			throw new NotSupportedException(System.SR.net_securitypackagesupport);
		}
		return null;
	}

	public static System.Net.Security.SafeFreeCredentials AcquireDefaultCredential(System.Net.ISSPIInterface secModule, string package, global::Interop.SspiCli.CredentialUse intent)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.AcquireDefaultCredential(package, intent);
		}
		System.Net.Security.SafeFreeCredentials outCredential = null;
		int num = secModule.AcquireDefaultCredential(package, intent, out outCredential);
		if (num != 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, System.SR.Format(System.SR.net_log_operation_failed_with_error, "AcquireDefaultCredential", $"0x{num:X}"), "AcquireDefaultCredential");
			}
			throw new Win32Exception(num);
		}
		return outCredential;
	}

	public static System.Net.Security.SafeFreeCredentials AcquireCredentialsHandle(System.Net.ISSPIInterface secModule, string package, global::Interop.SspiCli.CredentialUse intent, ref System.Net.Security.SafeSspiAuthDataHandle authdata)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.AcquireCredentialsHandle(package, intent, authdata);
		}
		System.Net.Security.SafeFreeCredentials outCredential = null;
		int num = secModule.AcquireCredentialsHandle(package, intent, ref authdata, out outCredential);
		if (num != 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, System.SR.Format(System.SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle", $"0x{num:X}"), "AcquireCredentialsHandle");
			}
			throw new Win32Exception(num);
		}
		return outCredential;
	}

	internal static int InitializeSecurityContext(System.Net.ISSPIInterface secModule, ref System.Net.Security.SafeFreeCredentials credential, ref System.Net.Security.SafeDeleteSslContext context, string targetName, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness datarep, System.Net.Security.InputSecurityBuffers inputBuffers, ref System.Net.Security.SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.InitializeSecurityContext(credential, context, targetName, inFlags);
		}
		int num = secModule.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, datarep, inputBuffers, ref outputBuffer, ref outFlags);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.SecurityContextInputBuffers("InitializeSecurityContext", inputBuffers.Count, outputBuffer.size, (global::Interop.SECURITY_STATUS)num);
		}
		return num;
	}

	internal static int AcceptSecurityContext(System.Net.ISSPIInterface secModule, System.Net.Security.SafeFreeCredentials credential, ref System.Net.Security.SafeDeleteSslContext context, global::Interop.SspiCli.ContextFlags inFlags, global::Interop.SspiCli.Endianness datarep, System.Net.Security.InputSecurityBuffers inputBuffers, ref System.Net.Security.SecurityBuffer outputBuffer, ref global::Interop.SspiCli.ContextFlags outFlags)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.AcceptSecurityContext(credential, context, inFlags);
		}
		int num = secModule.AcceptSecurityContext(credential, ref context, inputBuffers, inFlags, datarep, ref outputBuffer, ref outFlags);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.SecurityContextInputBuffers("AcceptSecurityContext", inputBuffers.Count, outputBuffer.size, (global::Interop.SECURITY_STATUS)num);
		}
		return num;
	}

	internal static int CompleteAuthToken(System.Net.ISSPIInterface secModule, ref System.Net.Security.SafeDeleteSslContext context, in System.Net.Security.SecurityBuffer inputBuffer)
	{
		int num = secModule.CompleteAuthToken(ref context, in inputBuffer);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Log.OperationReturnedSomething("CompleteAuthToken", (global::Interop.SECURITY_STATUS)num);
		}
		return num;
	}

	public static int QuerySecurityContextToken(System.Net.ISSPIInterface secModule, System.Net.Security.SafeDeleteContext context, out System.Net.Security.SecurityContextTokenHandle token)
	{
		return secModule.QuerySecurityContextToken(context, out token);
	}

	public static bool QueryBlittableContextAttributes<T>(System.Net.ISSPIInterface secModule, System.Net.Security.SafeDeleteContext securityContext, global::Interop.SspiCli.ContextAttribute contextAttribute, Type safeHandleType, out SafeHandle sspiHandle, ref T attribute) where T : unmanaged
	{
		Span<T> span = MemoryMarshal.CreateSpan(ref attribute, 1);
		int num = secModule.QueryContextAttributes(securityContext, contextAttribute, MemoryMarshal.AsBytes(span), safeHandleType, out sspiHandle);
		if (num != 0)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, $"ERROR = {ErrorDescription(num)}", "QueryBlittableContextAttributes");
			}
			return false;
		}
		return true;
	}

	public static string QueryStringContextAttributes(System.Net.ISSPIInterface secModule, System.Net.Security.SafeDeleteContext securityContext, global::Interop.SspiCli.ContextAttribute contextAttribute)
	{
		Span<IntPtr> span = stackalloc IntPtr[1];
		SafeHandle refHandle;
		int num = secModule.QueryContextAttributes(securityContext, contextAttribute, MemoryMarshal.AsBytes(span), typeof(System.Net.Security.SafeFreeContextBuffer), out refHandle);
		using (refHandle)
		{
			if (num != 0)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(null, $"ERROR = {ErrorDescription(num)}", "QueryStringContextAttributes");
				}
				return null;
			}
			string text = Marshal.PtrToStringUni(refHandle.DangerousGetHandle());
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, text, "QueryStringContextAttributes");
			}
			return text;
		}
	}

	public static string ErrorDescription(int errorCode)
	{
		if (errorCode == -1)
		{
			return "An exception when invoking Win32 API";
		}
		return (global::Interop.SECURITY_STATUS)errorCode switch
		{
			global::Interop.SECURITY_STATUS.InvalidHandle => "Invalid handle", 
			global::Interop.SECURITY_STATUS.InvalidToken => "Invalid token", 
			global::Interop.SECURITY_STATUS.ContinueNeeded => "Continue needed", 
			global::Interop.SECURITY_STATUS.IncompleteMessage => "Message incomplete", 
			global::Interop.SECURITY_STATUS.WrongPrincipal => "Wrong principal", 
			global::Interop.SECURITY_STATUS.TargetUnknown => "Target unknown", 
			global::Interop.SECURITY_STATUS.PackageNotFound => "Package not found", 
			global::Interop.SECURITY_STATUS.BufferNotEnough => "Buffer not enough", 
			global::Interop.SECURITY_STATUS.MessageAltered => "Message altered", 
			global::Interop.SECURITY_STATUS.UntrustedRoot => "Untrusted root", 
			_ => "0x" + errorCode.ToString("x", NumberFormatInfo.InvariantInfo), 
		};
	}
}