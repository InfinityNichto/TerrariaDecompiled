using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;

namespace System.Net.Security;

internal static class SslSessionsCache
{
	private readonly struct SslCredKey : IEquatable<SslCredKey>
	{
		private readonly byte[] _thumbPrint;

		private readonly int _allowedProtocols;

		private readonly EncryptionPolicy _encryptionPolicy;

		private readonly bool _isServerMode;

		internal SslCredKey(byte[] thumbPrint, int allowedProtocols, bool isServerMode, EncryptionPolicy encryptionPolicy)
		{
			_thumbPrint = thumbPrint ?? Array.Empty<byte>();
			_allowedProtocols = allowedProtocols;
			_encryptionPolicy = encryptionPolicy;
			_isServerMode = isServerMode;
		}

		public override int GetHashCode()
		{
			int num = 0;
			if (_thumbPrint.Length != 0)
			{
				num ^= _thumbPrint[0];
				if (1 < _thumbPrint.Length)
				{
					num ^= _thumbPrint[1] << 8;
				}
				if (2 < _thumbPrint.Length)
				{
					num ^= _thumbPrint[2] << 16;
				}
				if (3 < _thumbPrint.Length)
				{
					num ^= _thumbPrint[3] << 24;
				}
			}
			num ^= _allowedProtocols;
			num ^= (int)_encryptionPolicy;
			return num ^ (_isServerMode ? 65536 : 131072);
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is SslCredKey)
			{
				return Equals((SslCredKey)obj);
			}
			return false;
		}

		public bool Equals(SslCredKey other)
		{
			byte[] thumbPrint = _thumbPrint;
			byte[] thumbPrint2 = other._thumbPrint;
			if (thumbPrint.Length != thumbPrint2.Length)
			{
				return false;
			}
			if (_encryptionPolicy != other._encryptionPolicy)
			{
				return false;
			}
			if (_allowedProtocols != other._allowedProtocols)
			{
				return false;
			}
			if (_isServerMode != other._isServerMode)
			{
				return false;
			}
			for (int i = 0; i < thumbPrint.Length; i++)
			{
				if (thumbPrint[i] != thumbPrint2[i])
				{
					return false;
				}
			}
			return true;
		}
	}

	private static readonly ConcurrentDictionary<SslCredKey, SafeCredentialReference> s_cachedCreds = new ConcurrentDictionary<SslCredKey, SafeCredentialReference>();

	internal static SafeFreeCredentials TryCachedCredential(byte[] thumbPrint, SslProtocols sslProtocols, bool isServer, EncryptionPolicy encryptionPolicy)
	{
		if (s_cachedCreds.IsEmpty)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Not found, Current Cache Count = {s_cachedCreds.Count}", "TryCachedCredential");
			}
			return null;
		}
		SslCredKey key = new SslCredKey(thumbPrint, (int)sslProtocols, isServer, encryptionPolicy);
		SafeFreeCredentials cachedCredential = GetCachedCredential(key);
		if (cachedCredential == null || cachedCredential.IsClosed || cachedCredential.IsInvalid)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Not found or invalid, Current Cache Coun = {s_cachedCreds.Count}", "TryCachedCredential");
			}
			return null;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, $"Found a cached Handle = {cachedCredential}", "TryCachedCredential");
		}
		return cachedCredential;
	}

	private static SafeFreeCredentials GetCachedCredential(SslCredKey key)
	{
		if (!s_cachedCreds.TryGetValue(key, out var value))
		{
			return null;
		}
		return value.Target;
	}

	internal static void CacheCredential(SafeFreeCredentials creds, byte[] thumbPrint, SslProtocols sslProtocols, bool isServer, EncryptionPolicy encryptionPolicy)
	{
		if (creds.IsInvalid)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Refused to cache an Invalid Handle {creds}, Current Cache Count = {s_cachedCreds.Count}", "CacheCredential");
			}
			return;
		}
		SslCredKey key = new SslCredKey(thumbPrint, (int)sslProtocols, isServer, encryptionPolicy);
		SafeFreeCredentials cachedCredential = GetCachedCredential(key);
		if (cachedCredential == null || cachedCredential.IsClosed || cachedCredential.IsInvalid)
		{
			lock (s_cachedCreds)
			{
				cachedCredential = GetCachedCredential(key);
				if (cachedCredential == null || cachedCredential.IsClosed || cachedCredential.IsInvalid)
				{
					SafeCredentialReference safeCredentialReference = SafeCredentialReference.CreateReference(creds);
					if (safeCredentialReference != null)
					{
						s_cachedCreds[key] = safeCredentialReference;
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(null, $"Caching New Handle = {creds}, Current Cache Count = {s_cachedCreds.Count}", "CacheCredential");
						}
						ShrinkCredentialCache();
					}
				}
				else if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(null, $"CacheCredential() (locked retry) Found already cached Handle = {cachedCredential}", "CacheCredential");
				}
				return;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, $"CacheCredential() Ignoring incoming handle = {creds} since found already cached Handle = {cachedCredential}", "CacheCredential");
		}
		static void ShrinkCredentialCache()
		{
			if (s_cachedCreds.Count % 32 == 0)
			{
				KeyValuePair<SslCredKey, SafeCredentialReference>[] array = s_cachedCreds.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					SafeCredentialReference value = array[i].Value;
					SafeFreeCredentials target = value.Target;
					SafeCredentialReference value2;
					if (target == null)
					{
						s_cachedCreds.TryRemove(array[i].Key, out value2);
					}
					else
					{
						value.Dispose();
						value = SafeCredentialReference.CreateReference(target);
						if (value != null)
						{
							s_cachedCreds[array[i].Key] = value;
						}
						else
						{
							s_cachedCreds.TryRemove(array[i].Key, out value2);
						}
					}
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(null, $"Scavenged cache, New Cache Count = {s_cachedCreds.Count}", "CacheCredential");
				}
			}
		}
	}
}
