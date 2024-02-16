using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Net;

public class CredentialCache : ICredentials, ICredentialsByHost, IEnumerable
{
	private class CredentialEnumerator : IEnumerator
	{
		private class SingleTableCredentialEnumerator<TKey> : CredentialEnumerator
		{
			private Dictionary<TKey, NetworkCredential>.ValueCollection.Enumerator _enumerator;

			public SingleTableCredentialEnumerator(CredentialCache cache, Dictionary<TKey, NetworkCredential> table)
				: base(cache)
			{
				_enumerator = table.Values.GetEnumerator();
			}

			protected override bool MoveNext(out NetworkCredential current)
			{
				return DictionaryEnumeratorHelper.MoveNext(ref _enumerator, out current);
			}

			public override void Reset()
			{
				DictionaryEnumeratorHelper.Reset(ref _enumerator);
				base.Reset();
			}
		}

		private sealed class DoubleTableCredentialEnumerator : SingleTableCredentialEnumerator<CredentialKey>
		{
			private Dictionary<CredentialHostKey, NetworkCredential>.ValueCollection.Enumerator _enumerator;

			private bool _onThisEnumerator;

			public DoubleTableCredentialEnumerator(CredentialCache cache)
				: base(cache, cache._cache)
			{
				_enumerator = cache._cacheForHosts.Values.GetEnumerator();
			}

			protected override bool MoveNext(out NetworkCredential current)
			{
				if (!_onThisEnumerator)
				{
					if (base.MoveNext(out current))
					{
						return true;
					}
					_onThisEnumerator = true;
				}
				return DictionaryEnumeratorHelper.MoveNext(ref _enumerator, out current);
			}

			public override void Reset()
			{
				_onThisEnumerator = false;
				DictionaryEnumeratorHelper.Reset(ref _enumerator);
				base.Reset();
			}
		}

		private static class DictionaryEnumeratorHelper
		{
			internal static bool MoveNext<TKey, TValue>(ref Dictionary<TKey, TValue>.ValueCollection.Enumerator enumerator, out TValue current)
			{
				bool result = enumerator.MoveNext();
				current = enumerator.Current;
				return result;
			}

			internal static void Reset<TEnumerator>(ref TEnumerator enumerator) where TEnumerator : IEnumerator
			{
				try
				{
					enumerator.Reset();
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		private readonly CredentialCache _cache;

		private readonly int _version;

		private bool _enumerating;

		private NetworkCredential _current;

		public object Current
		{
			get
			{
				if (!_enumerating)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				if (_version != _cache._version)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
				}
				return _current;
			}
		}

		internal static CredentialEnumerator Create(CredentialCache cache)
		{
			if (cache._cache != null)
			{
				if (cache._cacheForHosts == null)
				{
					return new SingleTableCredentialEnumerator<CredentialKey>(cache, cache._cache);
				}
				return new DoubleTableCredentialEnumerator(cache);
			}
			if (cache._cacheForHosts == null)
			{
				return new CredentialEnumerator(cache);
			}
			return new SingleTableCredentialEnumerator<CredentialHostKey>(cache, cache._cacheForHosts);
		}

		private CredentialEnumerator(CredentialCache cache)
		{
			_cache = cache;
			_version = cache._version;
		}

		public bool MoveNext()
		{
			if (_version != _cache._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			return _enumerating = MoveNext(out _current);
		}

		protected virtual bool MoveNext(out NetworkCredential current)
		{
			current = null;
			return false;
		}

		public virtual void Reset()
		{
			_enumerating = false;
		}
	}

	private Dictionary<CredentialKey, NetworkCredential> _cache;

	private Dictionary<CredentialHostKey, NetworkCredential> _cacheForHosts;

	private int _version;

	public static ICredentials DefaultCredentials => SystemNetworkCredential.s_defaultCredential;

	public static NetworkCredential DefaultNetworkCredentials => SystemNetworkCredential.s_defaultCredential;

	public void Add(Uri uriPrefix, string authType, NetworkCredential cred)
	{
		if (uriPrefix == null)
		{
			throw new ArgumentNullException("uriPrefix");
		}
		if (authType == null)
		{
			throw new ArgumentNullException("authType");
		}
		if (cred is SystemNetworkCredential && !string.Equals(authType, "NTLM", StringComparison.OrdinalIgnoreCase) && !string.Equals(authType, "Kerberos", StringComparison.OrdinalIgnoreCase) && !string.Equals(authType, "Negotiate", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_nodefaultcreds, authType), "authType");
		}
		_version++;
		CredentialKey credentialKey = new CredentialKey(uriPrefix, authType);
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, $"Adding key:[{credentialKey}], cred:[{cred.Domain}],[{cred.UserName}]", "Add");
		}
		if (_cache == null)
		{
			_cache = new Dictionary<CredentialKey, NetworkCredential>();
		}
		_cache.Add(credentialKey, cred);
	}

	public void Add(string host, int port, string authenticationType, NetworkCredential credential)
	{
		if (host == null)
		{
			throw new ArgumentNullException("host");
		}
		if (authenticationType == null)
		{
			throw new ArgumentNullException("authenticationType");
		}
		if (host.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "host"), "host");
		}
		if (port < 0)
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (credential is SystemNetworkCredential && !string.Equals(authenticationType, "NTLM", StringComparison.OrdinalIgnoreCase) && !string.Equals(authenticationType, "Kerberos", StringComparison.OrdinalIgnoreCase) && !string.Equals(authenticationType, "Negotiate", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_nodefaultcreds, authenticationType), "authenticationType");
		}
		_version++;
		CredentialHostKey credentialHostKey = new CredentialHostKey(host, port, authenticationType);
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, $"Adding key:[{credentialHostKey}], cred:[{credential.Domain}],[{credential.UserName}]", "Add");
		}
		if (_cacheForHosts == null)
		{
			_cacheForHosts = new Dictionary<CredentialHostKey, NetworkCredential>();
		}
		_cacheForHosts.Add(credentialHostKey, credential);
	}

	public void Remove(Uri? uriPrefix, string? authType)
	{
		if (uriPrefix == null || authType == null)
		{
			return;
		}
		if (_cache == null)
		{
			if (NetEventSource.Log.IsEnabled())
			{
				NetEventSource.Info(this, "Short-circuiting because the dictionary is null.", "Remove");
			}
			return;
		}
		_version++;
		CredentialKey credentialKey = new CredentialKey(uriPrefix, authType);
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, $"Removing key:[{credentialKey}]", "Remove");
		}
		_cache.Remove(credentialKey);
	}

	public void Remove(string? host, int port, string? authenticationType)
	{
		if (host == null || authenticationType == null || port < 0)
		{
			return;
		}
		if (_cacheForHosts == null)
		{
			if (NetEventSource.Log.IsEnabled())
			{
				NetEventSource.Info(this, "Short-circuiting because the dictionary is null.", "Remove");
			}
			return;
		}
		_version++;
		CredentialHostKey credentialHostKey = new CredentialHostKey(host, port, authenticationType);
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, $"Removing key:[{credentialHostKey}]", "Remove");
		}
		_cacheForHosts.Remove(credentialHostKey);
	}

	public NetworkCredential? GetCredential(Uri uriPrefix, string authType)
	{
		if (uriPrefix == null)
		{
			throw new ArgumentNullException("uriPrefix");
		}
		if (authType == null)
		{
			throw new ArgumentNullException("authType");
		}
		if (_cache == null)
		{
			if (NetEventSource.Log.IsEnabled())
			{
				NetEventSource.Info(this, "CredentialCache::GetCredential short-circuiting because the dictionary is null.", "GetCredential");
			}
			return null;
		}
		int num = -1;
		NetworkCredential networkCredential = null;
		foreach (KeyValuePair<CredentialKey, NetworkCredential> item in _cache)
		{
			CredentialKey key = item.Key;
			if (key.Match(uriPrefix, authType))
			{
				int uriPrefixLength = key.UriPrefixLength;
				if (uriPrefixLength > num)
				{
					num = uriPrefixLength;
					networkCredential = item.Value;
				}
			}
		}
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, FormattableStringFactory.Create("Returning {0}", (networkCredential == null) ? "null" : ("(" + networkCredential.UserName + ":" + networkCredential.Domain + ")")), "GetCredential");
		}
		return networkCredential;
	}

	public NetworkCredential? GetCredential(string host, int port, string authenticationType)
	{
		if (host == null)
		{
			throw new ArgumentNullException("host");
		}
		if (authenticationType == null)
		{
			throw new ArgumentNullException("authenticationType");
		}
		if (host.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "host"), "host");
		}
		if (port < 0)
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_cacheForHosts == null)
		{
			if (NetEventSource.Log.IsEnabled())
			{
				NetEventSource.Info(this, "CredentialCache::GetCredential short-circuiting because the dictionary is null.", "GetCredential");
			}
			return null;
		}
		CredentialHostKey key = new CredentialHostKey(host, port, authenticationType);
		NetworkCredential value = null;
		_cacheForHosts.TryGetValue(key, out value);
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, FormattableStringFactory.Create("Returning {0}", (value == null) ? "null" : ("(" + value.UserName + ":" + value.Domain + ")")), "GetCredential");
		}
		return value;
	}

	public IEnumerator GetEnumerator()
	{
		return CredentialEnumerator.Create(this);
	}
}
