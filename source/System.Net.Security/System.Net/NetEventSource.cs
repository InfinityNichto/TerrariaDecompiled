using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net;

[EventSource(Name = "Private.InternalDiagnostics.System.Net.Security", LocalizationResources = "FxResources.System.Net.Security.SR")]
internal sealed class NetEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Default = (EventKeywords)1L;

		public const EventKeywords Debug = (EventKeywords)2L;
	}

	public static readonly System.Net.NetEventSource Log = new System.Net.NetEventSource();

	[NonEvent]
	public static void DumpBuffer(object thisOrContextObject, ReadOnlySpan<byte> buffer, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			byte[] buffer2 = buffer[..Math.Min(buffer.Length, 1024)].ToArray();
			Log.DumpBuffer(IdOf(thisOrContextObject), memberName, buffer2);
		}
	}

	[Event(8, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	public void EnumerateSecurityPackages(string securityPackage)
	{
		if (IsEnabled())
		{
			WriteEvent(8, securityPackage ?? "");
		}
	}

	[Event(9, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	public void SspiPackageNotFound(string packageName)
	{
		if (IsEnabled())
		{
			WriteEvent(9, packageName ?? "");
		}
	}

	[NonEvent]
	public void SslStreamCtor(SslStream sslStream, Stream innerStream)
	{
		if (!IsEnabled())
		{
			return;
		}
		string text = null;
		string remoteId = null;
		if (innerStream is NetworkStream networkStream)
		{
			try
			{
				text = networkStream.Socket.LocalEndPoint?.ToString();
				remoteId = networkStream.Socket.RemoteEndPoint?.ToString();
			}
			catch
			{
			}
		}
		if (text == null)
		{
			text = IdOf(innerStream);
		}
		SslStreamCtor(IdOf(sslStream), text, remoteId);
	}

	[Event(38, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void SslStreamCtor(string thisOrContextObject, string localId, string remoteId)
	{
		WriteEvent(38, thisOrContextObject, localId, remoteId);
	}

	[NonEvent]
	public void SecureChannelCtor(SecureChannel secureChannel, SslStream sslStream, string hostname, X509CertificateCollection clientCertificates, EncryptionPolicy encryptionPolicy)
	{
		if (IsEnabled())
		{
			SecureChannelCtor(IdOf(secureChannel), hostname, GetHashCode(secureChannel), clientCertificates?.Count ?? 0, encryptionPolicy);
		}
	}

	[Event(17, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void SecureChannelCtor(string sslStream, string hostname, int secureChannelHash, int clientCertificatesCount, EncryptionPolicy encryptionPolicy)
	{
		WriteEvent(17, sslStream, hostname, secureChannelHash, clientCertificatesCount, (int)encryptionPolicy);
	}

	[NonEvent]
	public void LocatingPrivateKey(X509Certificate x509Certificate, object instance)
	{
		if (IsEnabled())
		{
			LocatingPrivateKey(x509Certificate.ToString(fVerbose: true), GetHashCode(instance));
		}
	}

	[Event(18, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void LocatingPrivateKey(string x509Certificate, int secureChannelHash)
	{
		WriteEvent(18, x509Certificate, secureChannelHash);
	}

	[NonEvent]
	public void CertIsType2(object instance)
	{
		if (IsEnabled())
		{
			CertIsType2(GetHashCode(instance));
		}
	}

	[Event(19, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void CertIsType2(int secureChannelHash)
	{
		WriteEvent(19, secureChannelHash);
	}

	[NonEvent]
	public void FoundCertInStore(bool serverMode, object instance)
	{
		if (IsEnabled())
		{
			FoundCertInStore(serverMode ? "LocalMachine" : "CurrentUser", GetHashCode(instance));
		}
	}

	[Event(20, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void FoundCertInStore(string store, int secureChannelHash)
	{
		WriteEvent(20, store, secureChannelHash);
	}

	[NonEvent]
	public void NotFoundCertInStore(object instance)
	{
		if (IsEnabled())
		{
			NotFoundCertInStore(GetHashCode(instance));
		}
	}

	[Event(21, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void NotFoundCertInStore(int secureChannelHash)
	{
		WriteEvent(21, secureChannelHash);
	}

	[NonEvent]
	public void RemoteCertificate(X509Certificate remoteCertificate)
	{
		if (IsEnabled())
		{
			RemoteCertificate(remoteCertificate?.ToString(fVerbose: true));
		}
	}

	[Event(22, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void RemoteCertificate(string remoteCertificate)
	{
		WriteEvent(22, remoteCertificate);
	}

	[NonEvent]
	public void CertificateFromDelegate(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			CertificateFromDelegate(GetHashCode(secureChannel));
		}
	}

	[Event(23, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void CertificateFromDelegate(int secureChannelHash)
	{
		WriteEvent(23, secureChannelHash);
	}

	[NonEvent]
	public void NoDelegateNoClientCert(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			NoDelegateNoClientCert(GetHashCode(secureChannel));
		}
	}

	[Event(24, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void NoDelegateNoClientCert(int secureChannelHash)
	{
		WriteEvent(24, secureChannelHash);
	}

	[NonEvent]
	public void NoDelegateButClientCert(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			NoDelegateButClientCert(GetHashCode(secureChannel));
		}
	}

	[Event(25, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void NoDelegateButClientCert(int secureChannelHash)
	{
		WriteEvent(25, secureChannelHash);
	}

	[NonEvent]
	public void AttemptingRestartUsingCert(X509Certificate clientCertificate, SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			AttemptingRestartUsingCert(clientCertificate?.ToString(fVerbose: true), GetHashCode(secureChannel));
		}
	}

	[Event(26, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void AttemptingRestartUsingCert(string clientCertificate, int secureChannelHash)
	{
		WriteEvent(26, clientCertificate, secureChannelHash);
	}

	[NonEvent]
	public void NoIssuersTryAllCerts(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			NoIssuersTryAllCerts(GetHashCode(secureChannel));
		}
	}

	[Event(27, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void NoIssuersTryAllCerts(int secureChannelHash)
	{
		WriteEvent(27, secureChannelHash);
	}

	[NonEvent]
	public void LookForMatchingCerts(int issuersCount, SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			LookForMatchingCerts(issuersCount, GetHashCode(secureChannel));
		}
	}

	[Event(28, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void LookForMatchingCerts(int issuersCount, int secureChannelHash)
	{
		WriteEvent(28, issuersCount, secureChannelHash);
	}

	[NonEvent]
	public void SelectedCert(X509Certificate clientCertificate, SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			SelectedCert(clientCertificate?.ToString(fVerbose: true), GetHashCode(secureChannel));
		}
	}

	[Event(29, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void SelectedCert(string clientCertificate, int secureChannelHash)
	{
		WriteEvent(29, clientCertificate, secureChannelHash);
	}

	[NonEvent]
	public void CertsAfterFiltering(int filteredCertsCount, SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			CertsAfterFiltering(filteredCertsCount, GetHashCode(secureChannel));
		}
	}

	[Event(30, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void CertsAfterFiltering(int filteredCertsCount, int secureChannelHash)
	{
		WriteEvent(30, filteredCertsCount, secureChannelHash);
	}

	[NonEvent]
	public void FindingMatchingCerts(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			FindingMatchingCerts(GetHashCode(secureChannel));
		}
	}

	[Event(31, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void FindingMatchingCerts(int secureChannelHash)
	{
		WriteEvent(31, secureChannelHash);
	}

	[NonEvent]
	public void UsingCachedCredential(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			UsingCachedCredential(GetHashCode(secureChannel));
		}
	}

	[Event(32, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void UsingCachedCredential(int secureChannelHash)
	{
		WriteEvent(32, secureChannelHash);
	}

	[Event(33, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	public void SspiSelectedCipherSuite(string process, SslProtocols sslProtocol, CipherAlgorithmType cipherAlgorithm, int cipherStrength, HashAlgorithmType hashAlgorithm, int hashStrength, ExchangeAlgorithmType keyExchangeAlgorithm, int keyExchangeStrength)
	{
		if (IsEnabled())
		{
			WriteEvent(33, process, (int)sslProtocol, (int)cipherAlgorithm, cipherStrength, (int)hashAlgorithm, hashStrength, (int)keyExchangeAlgorithm, keyExchangeStrength);
		}
	}

	[NonEvent]
	public void RemoteCertificateError(SecureChannel secureChannel, string message)
	{
		if (IsEnabled())
		{
			RemoteCertificateError(GetHashCode(secureChannel), message);
		}
	}

	[Event(34, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose)]
	private void RemoteCertificateError(int secureChannelHash, string message)
	{
		WriteEvent(34, secureChannelHash, message);
	}

	[NonEvent]
	public void RemoteCertDeclaredValid(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			RemoteCertDeclaredValid(GetHashCode(secureChannel));
		}
	}

	[Event(35, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose)]
	private void RemoteCertDeclaredValid(int secureChannelHash)
	{
		WriteEvent(35, secureChannelHash);
	}

	[NonEvent]
	public void RemoteCertHasNoErrors(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			RemoteCertHasNoErrors(GetHashCode(secureChannel));
		}
	}

	[Event(36, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose)]
	private void RemoteCertHasNoErrors(int secureChannelHash)
	{
		WriteEvent(36, secureChannelHash);
	}

	[NonEvent]
	public void RemoteCertUserDeclaredInvalid(SecureChannel secureChannel)
	{
		if (IsEnabled())
		{
			RemoteCertUserDeclaredInvalid(GetHashCode(secureChannel));
		}
	}

	[Event(37, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose)]
	private void RemoteCertUserDeclaredInvalid(int secureChannelHash)
	{
		WriteEvent(37, secureChannelHash);
	}

	[NonEvent]
	public void SentFrame(SslStream sslStream, ReadOnlySpan<byte> frame)
	{
		if (IsEnabled())
		{
			TlsFrameHelper.TlsFrameInfo info = default(TlsFrameHelper.TlsFrameInfo);
			bool flag = TlsFrameHelper.TryGetFrameInfo(frame, ref info);
			SentFrame(IdOf(sslStream), info.ToString(), flag ? 1 : 0);
		}
	}

	[Event(39, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose)]
	private void SentFrame(string sslStream, string tlsFrame, int isComplete)
	{
		WriteEvent(39, sslStream, tlsFrame, isComplete);
	}

	[NonEvent]
	public void ReceivedFrame(SslStream sslStream, TlsFrameHelper.TlsFrameInfo frameInfo)
	{
		if (IsEnabled())
		{
			ReceivedFrame(IdOf(sslStream), frameInfo.ToString(), 1);
		}
	}

	[Event(40, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose)]
	private void ReceivedFrame(string sslStream, string tlsFrame, int isComplete)
	{
		WriteEvent(40, sslStream, tlsFrame, isComplete);
	}

	[NonEvent]
	public static void Info(object thisOrContextObject, FormattableString formattableString = null, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.Info(IdOf(thisOrContextObject), memberName, (formattableString != null) ? Format(formattableString) : "");
		}
	}

	[NonEvent]
	public static void Info(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.Info(IdOf(thisOrContextObject), memberName, Format(message).ToString());
		}
	}

	[Event(4, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void Info(string thisOrContextObject, string memberName, string message)
	{
		WriteEvent(4, thisOrContextObject, memberName ?? "(?)", message);
	}

	[NonEvent]
	public static void Error(object thisOrContextObject, FormattableString formattableString, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(formattableString));
		}
	}

	[NonEvent]
	public static void Error(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(message).ToString());
		}
	}

	[Event(5, Level = EventLevel.Error, Keywords = (EventKeywords)1L)]
	private void ErrorMessage(string thisOrContextObject, string memberName, string message)
	{
		WriteEvent(5, thisOrContextObject, memberName ?? "(?)", message);
	}

	[NonEvent]
	public static void Verbose(object thisOrContextObject, FormattableString formattableString, [CallerMemberName] string memberName = null)
	{
		if (Log.IsEnabled())
		{
			Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(formattableString));
		}
	}

	[Event(7, Level = EventLevel.Verbose, Keywords = (EventKeywords)2L)]
	private void DumpBuffer(string thisOrContextObject, string memberName, byte[] buffer)
	{
		WriteEvent(7, thisOrContextObject, memberName ?? "(?)", buffer);
	}

	[NonEvent]
	public static string IdOf(object value)
	{
		if (value == null)
		{
			return "(null)";
		}
		return value.GetType().Name + "#" + GetHashCode(value);
	}

	[NonEvent]
	public static int GetHashCode(object value)
	{
		return value?.GetHashCode() ?? 0;
	}

	[NonEvent]
	public static object Format(object value)
	{
		if (value == null)
		{
			return "(null)";
		}
		string result = null;
		AdditionalCustomizedToString(value, ref result);
		if (result != null)
		{
			return result;
		}
		if (value is Array array)
		{
			return $"{array.GetType().GetElementType()}[{((Array)value).Length}]";
		}
		if (value is ICollection collection)
		{
			return $"{collection.GetType().Name}({collection.Count})";
		}
		if (value is SafeHandle safeHandle)
		{
			return $"{safeHandle.GetType().Name}:{safeHandle.GetHashCode()}(0x{safeHandle.DangerousGetHandle():X})";
		}
		if (value is IntPtr)
		{
			return $"0x{value:X}";
		}
		string text = value.ToString();
		if (text == null || text == value.GetType().FullName)
		{
			return IdOf(value);
		}
		return value;
	}

	[NonEvent]
	private static string Format(FormattableString s)
	{
		switch (s.ArgumentCount)
		{
		case 0:
			return s.Format;
		case 1:
			return string.Format(s.Format, Format(s.GetArgument(0)));
		case 2:
			return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)));
		case 3:
			return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)), Format(s.GetArgument(2)));
		default:
		{
			object[] arguments = s.GetArguments();
			object[] array = new object[arguments.Length];
			for (int i = 0; i < arguments.Length; i++)
			{
				array[i] = Format(arguments[i]);
			}
			return string.Format(s.Format, array);
		}
		}
	}

	private static void AdditionalCustomizedToString<T>(T value, ref string result)
	{
		if (value is X509Certificate x509Certificate)
		{
			result = x509Certificate.ToString(fVerbose: true);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, byte[] arg3)
	{
		//The blocks IL_004f, IL_0053, IL_0055, IL_006e, IL_0155 are reachable both inside and outside the pinned region starting at IL_004c. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!Log.IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		if (arg3 == null)
		{
			arg3 = Array.Empty<byte>();
		}
		fixed (char* ptr5 = arg1)
		{
			char* intPtr;
			byte[] array;
			int size;
			EventData* intPtr2;
			nint num;
			nint num2;
			nint num3;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				array = arg3;
				fixed (byte* ptr2 = array)
				{
					byte* ptr3 = ptr2;
					size = arg3.Length;
					EventData* ptr4 = stackalloc EventData[4];
					intPtr2 = ptr4;
					*intPtr2 = new EventData
					{
						DataPointer = (IntPtr)ptr5,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr4 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (IntPtr)ptr,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr4 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (IntPtr)(&size),
						Size = 4
					};
					num3 = (nint)(ptr4 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (IntPtr)ptr3,
						Size = size
					};
					WriteEventCore(eventId, 4, ptr4);
				}
				return;
			}
			fixed (char* ptr6 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr6);
				array = arg3;
				fixed (byte* ptr2 = array)
				{
					byte* ptr3 = ptr2;
					size = arg3.Length;
					EventData* ptr4 = stackalloc EventData[4];
					intPtr2 = ptr4;
					*intPtr2 = new EventData
					{
						DataPointer = (IntPtr)ptr5,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr4 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (IntPtr)ptr,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr4 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (IntPtr)(&size),
						Size = 4
					};
					num3 = (nint)(ptr4 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (IntPtr)ptr3,
						Size = size
					};
					WriteEventCore(eventId, 4, ptr4);
				}
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, int arg2, int arg3, int arg4)
	{
		if (Log.IsEnabled())
		{
			if (arg1 == null)
			{
				arg1 = "";
			}
			fixed (char* ptr2 = arg1)
			{
				EventData* ptr = stackalloc EventData[4];
				*ptr = new EventData
				{
					DataPointer = (IntPtr)ptr2,
					Size = (arg1.Length + 1) * 2
				};
				ptr[1] = new EventData
				{
					DataPointer = (IntPtr)(&arg2),
					Size = 4
				};
				ptr[2] = new EventData
				{
					DataPointer = (IntPtr)(&arg3),
					Size = 4
				};
				ptr[3] = new EventData
				{
					DataPointer = (IntPtr)(&arg4),
					Size = 4
				};
				WriteEventCore(eventId, 4, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, int arg2, string arg3)
	{
		//The blocks IL_0047 are reachable both inside and outside the pinned region starting at IL_0044. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!Log.IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg3 == null)
		{
			arg3 = "";
		}
		fixed (char* ptr3 = arg1)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			if (arg3 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				EventData* ptr2 = stackalloc EventData[3];
				intPtr2 = ptr2;
				*intPtr2 = new EventData
				{
					DataPointer = (IntPtr)ptr3,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr2 + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (IntPtr)(&arg2),
					Size = 4
				};
				num2 = (nint)(ptr2 + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (IntPtr)ptr,
					Size = (arg3.Length + 1) * 2
				};
				WriteEventCore(eventId, 3, ptr2);
				return;
			}
			fixed (char* ptr4 = &arg3.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr4);
				EventData* ptr2 = stackalloc EventData[3];
				intPtr2 = ptr2;
				*intPtr2 = new EventData
				{
					DataPointer = (IntPtr)ptr3,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr2 + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (IntPtr)(&arg2),
					Size = 4
				};
				num2 = (nint)(ptr2 + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (IntPtr)ptr,
					Size = (arg3.Length + 1) * 2
				};
				WriteEventCore(eventId, 3, ptr2);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, int arg3)
	{
		//The blocks IL_0044 are reachable both inside and outside the pinned region starting at IL_0041. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!Log.IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		fixed (char* ptr3 = arg1)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				EventData* ptr2 = stackalloc EventData[3];
				intPtr2 = ptr2;
				*intPtr2 = new EventData
				{
					DataPointer = (IntPtr)ptr3,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr2 + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (IntPtr)ptr,
					Size = (arg2.Length + 1) * 2
				};
				num2 = (nint)(ptr2 + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (IntPtr)(&arg3),
					Size = 4
				};
				WriteEventCore(eventId, 3, ptr2);
				return;
			}
			fixed (char* ptr4 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr4);
				EventData* ptr2 = stackalloc EventData[3];
				intPtr2 = ptr2;
				*intPtr2 = new EventData
				{
					DataPointer = (IntPtr)ptr3,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr2 + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (IntPtr)ptr,
					Size = (arg2.Length + 1) * 2
				};
				num2 = (nint)(ptr2 + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (IntPtr)(&arg3),
					Size = 4
				};
				WriteEventCore(eventId, 3, ptr2);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3, int arg4)
	{
		//The blocks IL_004f, IL_0052, IL_0064, IL_0151 are reachable both inside and outside the pinned region starting at IL_004c. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!Log.IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		if (arg3 == null)
		{
			arg3 = "";
		}
		fixed (char* ptr5 = arg1)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			nint num3;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				fixed (char* ptr2 = arg3)
				{
					char* ptr3 = ptr2;
					EventData* ptr4 = stackalloc EventData[4];
					intPtr2 = ptr4;
					*intPtr2 = new EventData
					{
						DataPointer = (IntPtr)ptr5,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr4 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (IntPtr)ptr,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr4 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (IntPtr)ptr3,
						Size = (arg3.Length + 1) * 2
					};
					num3 = (nint)(ptr4 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (IntPtr)(&arg4),
						Size = 4
					};
					WriteEventCore(eventId, 4, ptr4);
				}
				return;
			}
			fixed (char* ptr6 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr6);
				fixed (char* ptr2 = arg3)
				{
					char* ptr3 = ptr2;
					EventData* ptr4 = stackalloc EventData[4];
					intPtr2 = ptr4;
					*intPtr2 = new EventData
					{
						DataPointer = (IntPtr)ptr5,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr4 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (IntPtr)ptr,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr4 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (IntPtr)ptr3,
						Size = (arg3.Length + 1) * 2
					};
					num3 = (nint)(ptr4 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (IntPtr)(&arg4),
						Size = 4
					};
					WriteEventCore(eventId, 4, ptr4);
				}
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8)
	{
		if (Log.IsEnabled())
		{
			if (arg1 == null)
			{
				arg1 = "";
			}
			fixed (char* ptr2 = arg1)
			{
				EventData* ptr = stackalloc EventData[8];
				*ptr = new EventData
				{
					DataPointer = (IntPtr)ptr2,
					Size = (arg1.Length + 1) * 2
				};
				ptr[1] = new EventData
				{
					DataPointer = (IntPtr)(&arg2),
					Size = 4
				};
				ptr[2] = new EventData
				{
					DataPointer = (IntPtr)(&arg3),
					Size = 4
				};
				ptr[3] = new EventData
				{
					DataPointer = (IntPtr)(&arg4),
					Size = 4
				};
				ptr[4] = new EventData
				{
					DataPointer = (IntPtr)(&arg5),
					Size = 4
				};
				ptr[5] = new EventData
				{
					DataPointer = (IntPtr)(&arg6),
					Size = 4
				};
				ptr[6] = new EventData
				{
					DataPointer = (IntPtr)(&arg7),
					Size = 4
				};
				ptr[7] = new EventData
				{
					DataPointer = (IntPtr)(&arg8),
					Size = 4
				};
				WriteEventCore(eventId, 8, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, int arg3, int arg4, int arg5)
	{
		//The blocks IL_0044 are reachable both inside and outside the pinned region starting at IL_0041. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!Log.IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		fixed (char* ptr3 = arg1)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			nint num3;
			nint num4;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				EventData* ptr2 = stackalloc EventData[5];
				intPtr2 = ptr2;
				*intPtr2 = new EventData
				{
					DataPointer = (IntPtr)ptr3,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr2 + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (IntPtr)ptr,
					Size = (arg2.Length + 1) * 2
				};
				num2 = (nint)(ptr2 + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (IntPtr)(&arg3),
					Size = 4
				};
				num3 = (nint)(ptr2 + 3);
				*(EventData*)num3 = new EventData
				{
					DataPointer = (IntPtr)(&arg4),
					Size = 4
				};
				num4 = (nint)(ptr2 + 4);
				*(EventData*)num4 = new EventData
				{
					DataPointer = (IntPtr)(&arg5),
					Size = 4
				};
				WriteEventCore(eventId, 5, ptr2);
				return;
			}
			fixed (char* ptr4 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr4);
				EventData* ptr2 = stackalloc EventData[5];
				intPtr2 = ptr2;
				*intPtr2 = new EventData
				{
					DataPointer = (IntPtr)ptr3,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr2 + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (IntPtr)ptr,
					Size = (arg2.Length + 1) * 2
				};
				num2 = (nint)(ptr2 + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (IntPtr)(&arg3),
					Size = 4
				};
				num3 = (nint)(ptr2 + 3);
				*(EventData*)num3 = new EventData
				{
					DataPointer = (IntPtr)(&arg4),
					Size = 4
				};
				num4 = (nint)(ptr2 + 4);
				*(EventData*)num4 = new EventData
				{
					DataPointer = (IntPtr)(&arg5),
					Size = 4
				};
				WriteEventCore(eventId, 5, ptr2);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "parameter intent is an enum and is trimmer safe")]
	[Event(10, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	public void AcquireDefaultCredential(string packageName, global::Interop.SspiCli.CredentialUse intent)
	{
		if (IsEnabled())
		{
			WriteEvent(10, packageName, intent);
		}
	}

	[NonEvent]
	public void AcquireCredentialsHandle(string packageName, global::Interop.SspiCli.CredentialUse intent, object authdata)
	{
		if (IsEnabled())
		{
			AcquireCredentialsHandle(packageName, intent, IdOf(authdata));
		}
	}

	[Event(11, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	public void AcquireCredentialsHandle(string packageName, global::Interop.SspiCli.CredentialUse intent, string authdata)
	{
		if (IsEnabled())
		{
			WriteEvent(11, packageName, (int)intent, authdata);
		}
	}

	[NonEvent]
	public void InitializeSecurityContext(SafeFreeCredentials credential, SafeDeleteContext context, string targetName, global::Interop.SspiCli.ContextFlags inFlags)
	{
		if (IsEnabled())
		{
			InitializeSecurityContext(IdOf(credential), IdOf(context), targetName, inFlags);
		}
	}

	[Event(12, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void InitializeSecurityContext(string credential, string context, string targetName, global::Interop.SspiCli.ContextFlags inFlags)
	{
		WriteEvent(12, credential, context, targetName, (int)inFlags);
	}

	[NonEvent]
	public void AcceptSecurityContext(SafeFreeCredentials credential, SafeDeleteContext context, global::Interop.SspiCli.ContextFlags inFlags)
	{
		if (IsEnabled())
		{
			AcceptSecurityContext(IdOf(credential), IdOf(context), inFlags);
		}
	}

	[Event(15, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void AcceptSecurityContext(string credential, string context, global::Interop.SspiCli.ContextFlags inFlags)
	{
		WriteEvent(15, credential, context, (int)inFlags);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "parameter errorCode is an enum and is trimmer safe")]
	[Event(16, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	public void OperationReturnedSomething(string operation, global::Interop.SECURITY_STATUS errorCode)
	{
		if (IsEnabled())
		{
			WriteEvent(16, operation, errorCode);
		}
	}

	[Event(14, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	public void SecurityContextInputBuffers(string context, int inputBuffersSize, int outputBufferSize, global::Interop.SECURITY_STATUS errorCode)
	{
		if (IsEnabled())
		{
			WriteEvent(14, context, inputBuffersSize, outputBufferSize, (int)errorCode);
		}
	}
}
