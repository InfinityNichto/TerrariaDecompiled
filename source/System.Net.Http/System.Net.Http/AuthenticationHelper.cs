using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal static class AuthenticationHelper
{
	private enum AuthenticationType
	{
		Basic,
		Digest,
		Ntlm,
		Negotiate
	}

	private readonly struct AuthenticationChallenge
	{
		public AuthenticationType AuthenticationType { get; }

		public string SchemeName { get; }

		public NetworkCredential Credential { get; }

		public string ChallengeData { get; }

		public AuthenticationChallenge(AuthenticationType authenticationType, string schemeName, NetworkCredential credential, string challenge)
		{
			AuthenticationType = authenticationType;
			SchemeName = schemeName;
			Credential = credential;
			ChallengeData = challenge;
		}
	}

	internal sealed class DigestResponse
	{
		internal readonly Dictionary<string, string> Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		internal DigestResponse(string challenge)
		{
			if (!string.IsNullOrEmpty(challenge))
			{
				Parse(challenge);
			}
		}

		private static bool CharIsSpaceOrTab(char ch)
		{
			if (ch != ' ')
			{
				return ch == '\t';
			}
			return true;
		}

		private static bool MustValueBeQuoted(string key)
		{
			if (!key.Equals("realm", StringComparison.OrdinalIgnoreCase) && !key.Equals("nonce", StringComparison.OrdinalIgnoreCase) && !key.Equals("opaque", StringComparison.OrdinalIgnoreCase))
			{
				return key.Equals("qop", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}

		private string GetNextKey(string data, int currentIndex, out int parsedIndex)
		{
			while (currentIndex < data.Length && CharIsSpaceOrTab(data[currentIndex]))
			{
				currentIndex++;
			}
			int num = currentIndex;
			while (currentIndex < data.Length && data[currentIndex] != '=' && !CharIsSpaceOrTab(data[currentIndex]))
			{
				currentIndex++;
			}
			if (currentIndex == data.Length)
			{
				parsedIndex = currentIndex;
				return null;
			}
			int length = currentIndex - num;
			if (CharIsSpaceOrTab(data[currentIndex]))
			{
				while (currentIndex < data.Length && CharIsSpaceOrTab(data[currentIndex]))
				{
					currentIndex++;
				}
				if (currentIndex == data.Length || data[currentIndex] != '=')
				{
					parsedIndex = currentIndex;
					return null;
				}
			}
			while (currentIndex < data.Length && (CharIsSpaceOrTab(data[currentIndex]) || data[currentIndex] == '='))
			{
				currentIndex++;
			}
			parsedIndex = currentIndex;
			return data.Substring(num, length);
		}

		private string GetNextValue(string data, int currentIndex, bool expectQuotes, out int parsedIndex)
		{
			bool flag = false;
			if (data[currentIndex] == '"')
			{
				flag = true;
				currentIndex++;
			}
			if (expectQuotes && !flag)
			{
				parsedIndex = currentIndex;
				return null;
			}
			StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
			while (currentIndex < data.Length && ((flag && data[currentIndex] != '"') || (!flag && data[currentIndex] != ',')))
			{
				stringBuilder.Append(data[currentIndex]);
				currentIndex++;
				if (currentIndex == data.Length || (!flag && CharIsSpaceOrTab(data[currentIndex])))
				{
					break;
				}
				if (flag && data[currentIndex] == '"' && data[currentIndex - 1] == '\\')
				{
					stringBuilder.Append(data[currentIndex]);
					currentIndex++;
				}
			}
			if (flag)
			{
				currentIndex++;
			}
			while (currentIndex < data.Length && CharIsSpaceOrTab(data[currentIndex]))
			{
				currentIndex++;
			}
			if (currentIndex == data.Length)
			{
				parsedIndex = currentIndex;
				return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
			}
			if (data[currentIndex++] != ',')
			{
				parsedIndex = currentIndex;
				return null;
			}
			while (currentIndex < data.Length && CharIsSpaceOrTab(data[currentIndex]))
			{
				currentIndex++;
			}
			parsedIndex = currentIndex;
			return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
		}

		private void Parse(string challenge)
		{
			int parsedIndex = 0;
			while (parsedIndex < challenge.Length)
			{
				string nextKey = GetNextKey(challenge, parsedIndex, out parsedIndex);
				if (!string.IsNullOrEmpty(nextKey) && parsedIndex < challenge.Length)
				{
					string nextValue = GetNextValue(challenge, parsedIndex, MustValueBeQuoted(nextKey), out parsedIndex);
					if (nextValue != null && (!(nextValue == string.Empty) || nextKey.Equals("opaque", StringComparison.OrdinalIgnoreCase) || nextKey.Equals("domain", StringComparison.OrdinalIgnoreCase) || nextKey.Equals("realm", StringComparison.OrdinalIgnoreCase)))
					{
						Parameters.Add(nextKey, nextValue);
						continue;
					}
					break;
				}
				break;
			}
		}
	}

	private static readonly int[] s_alphaNumChooser = new int[3] { 48, 65, 97 };

	private static volatile int s_usePortInSpn = -1;

	private static bool UsePortInSpn
	{
		get
		{
			int num = s_usePortInSpn;
			if (num != -1)
			{
				return num != 0;
			}
			if (AppContext.TryGetSwitch("System.Net.Http.UsePortInSpn", out var isEnabled))
			{
				s_usePortInSpn = (isEnabled ? 1 : 0);
			}
			else
			{
				string environmentVariable = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_USEPORTINSPN");
				s_usePortInSpn = ((environmentVariable != null && (environmentVariable == "1" || environmentVariable.Equals("true", StringComparison.OrdinalIgnoreCase))) ? 1 : 0);
			}
			return s_usePortInSpn != 0;
		}
	}

	private static bool TryGetChallengeDataForScheme(string scheme, HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues, out string challengeData)
	{
		foreach (AuthenticationHeaderValue authenticationHeaderValue in authenticationHeaderValues)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(scheme, authenticationHeaderValue.Scheme))
			{
				challengeData = authenticationHeaderValue.Parameter;
				return true;
			}
		}
		challengeData = null;
		return false;
	}

	internal static bool IsSessionAuthenticationChallenge(HttpResponseMessage response)
	{
		if (response.StatusCode != HttpStatusCode.Unauthorized)
		{
			return false;
		}
		HttpHeaderValueCollection<AuthenticationHeaderValue> responseAuthenticationHeaderValues = GetResponseAuthenticationHeaderValues(response, isProxyAuth: false);
		foreach (AuthenticationHeaderValue item in responseAuthenticationHeaderValues)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals("Negotiate", item.Scheme) || StringComparer.OrdinalIgnoreCase.Equals("NTLM", item.Scheme))
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryGetValidAuthenticationChallengeForScheme(string scheme, AuthenticationType authenticationType, Uri uri, ICredentials credentials, HttpHeaderValueCollection<AuthenticationHeaderValue> authenticationHeaderValues, out AuthenticationChallenge challenge)
	{
		challenge = default(AuthenticationChallenge);
		if (!TryGetChallengeDataForScheme(scheme, authenticationHeaderValues, out var challengeData))
		{
			return false;
		}
		NetworkCredential credential = credentials.GetCredential(uri, scheme);
		if (credential == null)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.AuthenticationInfo(uri, "Authentication scheme '" + scheme + "' supported by server, but not by client.");
			}
			return false;
		}
		challenge = new AuthenticationChallenge(authenticationType, scheme, credential, challengeData);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.AuthenticationInfo(uri, "Authentication scheme '" + scheme + "' selected. Client username=" + challenge.Credential.UserName);
		}
		return true;
	}

	private static bool TryGetAuthenticationChallenge(HttpResponseMessage response, bool isProxyAuth, Uri authUri, ICredentials credentials, out AuthenticationChallenge challenge)
	{
		if (!IsAuthenticationChallenge(response, isProxyAuth))
		{
			challenge = default(AuthenticationChallenge);
			return false;
		}
		HttpHeaderValueCollection<AuthenticationHeaderValue> responseAuthenticationHeaderValues = GetResponseAuthenticationHeaderValues(response, isProxyAuth);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.AuthenticationInfo(authUri, $"{(isProxyAuth ? "Proxy" : "Server")} authentication requested with WWW-Authenticate header value '{responseAuthenticationHeaderValues}'");
		}
		if (!TryGetValidAuthenticationChallengeForScheme("Negotiate", AuthenticationType.Negotiate, authUri, credentials, responseAuthenticationHeaderValues, out challenge) && !TryGetValidAuthenticationChallengeForScheme("NTLM", AuthenticationType.Ntlm, authUri, credentials, responseAuthenticationHeaderValues, out challenge) && !TryGetValidAuthenticationChallengeForScheme("Digest", AuthenticationType.Digest, authUri, credentials, responseAuthenticationHeaderValues, out challenge))
		{
			return TryGetValidAuthenticationChallengeForScheme("Basic", AuthenticationType.Basic, authUri, credentials, responseAuthenticationHeaderValues, out challenge);
		}
		return true;
	}

	private static bool TryGetRepeatedChallenge(HttpResponseMessage response, string scheme, bool isProxyAuth, out string challengeData)
	{
		challengeData = null;
		if (!IsAuthenticationChallenge(response, isProxyAuth))
		{
			return false;
		}
		if (!TryGetChallengeDataForScheme(scheme, GetResponseAuthenticationHeaderValues(response, isProxyAuth), out challengeData))
		{
			return false;
		}
		return true;
	}

	private static bool IsAuthenticationChallenge(HttpResponseMessage response, bool isProxyAuth)
	{
		if (!isProxyAuth)
		{
			return response.StatusCode == HttpStatusCode.Unauthorized;
		}
		return response.StatusCode == HttpStatusCode.ProxyAuthenticationRequired;
	}

	private static HttpHeaderValueCollection<AuthenticationHeaderValue> GetResponseAuthenticationHeaderValues(HttpResponseMessage response, bool isProxyAuth)
	{
		if (!isProxyAuth)
		{
			return response.Headers.WwwAuthenticate;
		}
		return response.Headers.ProxyAuthenticate;
	}

	private static void SetRequestAuthenticationHeaderValue(HttpRequestMessage request, AuthenticationHeaderValue headerValue, bool isProxyAuth)
	{
		if (isProxyAuth)
		{
			request.Headers.ProxyAuthorization = headerValue;
		}
		else
		{
			request.Headers.Authorization = headerValue;
		}
	}

	private static void SetBasicAuthToken(HttpRequestMessage request, NetworkCredential credential, bool isProxyAuth)
	{
		string s = ((!string.IsNullOrEmpty(credential.Domain)) ? (credential.Domain + "\\" + credential.UserName + ":" + credential.Password) : (credential.UserName + ":" + credential.Password));
		string parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
		SetRequestAuthenticationHeaderValue(request, new AuthenticationHeaderValue("Basic", parameter), isProxyAuth);
	}

	private static async ValueTask<bool> TrySetDigestAuthToken(HttpRequestMessage request, NetworkCredential credential, DigestResponse digestResponse, bool isProxyAuth)
	{
		string text = await GetDigestTokenForCredential(credential, request, digestResponse).ConfigureAwait(continueOnCapturedContext: false);
		if (string.IsNullOrEmpty(text))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.AuthenticationError(request.RequestUri, "Unable to find 'Digest' authentication token when authenticating with " + (isProxyAuth ? "proxy" : "server"));
			}
			return false;
		}
		AuthenticationHeaderValue headerValue = new AuthenticationHeaderValue("Digest", text);
		SetRequestAuthenticationHeaderValue(request, headerValue, isProxyAuth);
		return true;
	}

	private static ValueTask<HttpResponseMessage> InnerSendAsync(HttpRequestMessage request, bool async, bool isProxyAuth, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
	{
		if (!isProxyAuth)
		{
			return pool.SendWithProxyAuthAsync(request, async, doRequestAuth, cancellationToken);
		}
		return pool.SendWithVersionDetectionAndRetryAsync(request, async, doRequestAuth, cancellationToken);
	}

	private static async ValueTask<HttpResponseMessage> SendWithAuthAsync(HttpRequestMessage request, Uri authUri, bool async, ICredentials credentials, bool preAuthenticate, bool isProxyAuth, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
	{
		bool performedBasicPreauth = false;
		if (preAuthenticate)
		{
			NetworkCredential credential;
			lock (pool.PreAuthCredentials)
			{
				credential = pool.PreAuthCredentials.GetCredential(authUri, "Basic");
			}
			if (credential != null)
			{
				SetBasicAuthToken(request, credential, isProxyAuth);
				performedBasicPreauth = true;
			}
		}
		HttpResponseMessage response = await InnerSendAsync(request, async, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (TryGetAuthenticationChallenge(response, isProxyAuth, authUri, credentials, out var challenge))
		{
			switch (challenge.AuthenticationType)
			{
			case AuthenticationType.Digest:
			{
				DigestResponse digestResponse = new DigestResponse(challenge.ChallengeData);
				if (!(await TrySetDigestAuthToken(request, challenge.Credential, digestResponse, isProxyAuth).ConfigureAwait(continueOnCapturedContext: false)))
				{
					break;
				}
				response.Dispose();
				response = await InnerSendAsync(request, async, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (TryGetRepeatedChallenge(response, challenge.SchemeName, isProxyAuth, out var challengeData))
				{
					digestResponse = new DigestResponse(challengeData);
					bool flag = IsServerNonceStale(digestResponse);
					bool flag2 = flag;
					if (flag2)
					{
						flag2 = await TrySetDigestAuthToken(request, challenge.Credential, digestResponse, isProxyAuth).ConfigureAwait(continueOnCapturedContext: false);
					}
					if (flag2)
					{
						response.Dispose();
						response = await InnerSendAsync(request, async, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				break;
			}
			case AuthenticationType.Basic:
			{
				if (performedBasicPreauth)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.AuthenticationError(authUri, "Pre-authentication with " + (isProxyAuth ? "proxy" : "server") + " failed.");
					}
					break;
				}
				response.Dispose();
				SetBasicAuthToken(request, challenge.Credential, isProxyAuth);
				response = await InnerSendAsync(request, async, isProxyAuth, doRequestAuth, pool, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (!preAuthenticate)
				{
					break;
				}
				HttpStatusCode statusCode = response.StatusCode;
				if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.ProxyAuthenticationRequired)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.AuthenticationError(authUri, "Pre-authentication with " + (isProxyAuth ? "proxy" : "server") + " failed.");
					}
					break;
				}
				lock (pool.PreAuthCredentials)
				{
					try
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(pool.PreAuthCredentials, $"Adding Basic credential to cache, uri={authUri}, username={challenge.Credential.UserName}", "SendWithAuthAsync");
						}
						pool.PreAuthCredentials.Add(authUri, "Basic", challenge.Credential);
					}
					catch (ArgumentException)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(pool.PreAuthCredentials, $"Basic credential present in cache, uri={authUri}, username={challenge.Credential.UserName}", "SendWithAuthAsync");
						}
					}
				}
				break;
			}
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled() && response.StatusCode == HttpStatusCode.Unauthorized)
		{
			System.Net.NetEventSource.AuthenticationError(authUri, (isProxyAuth ? "Proxy" : "Server") + " authentication failed.");
		}
		return response;
	}

	public static ValueTask<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, Uri proxyUri, bool async, ICredentials proxyCredentials, bool doRequestAuth, HttpConnectionPool pool, CancellationToken cancellationToken)
	{
		return SendWithAuthAsync(request, proxyUri, async, proxyCredentials, preAuthenticate: false, isProxyAuth: true, doRequestAuth, pool, cancellationToken);
	}

	public static ValueTask<HttpResponseMessage> SendWithRequestAuthAsync(HttpRequestMessage request, bool async, ICredentials credentials, bool preAuthenticate, HttpConnectionPool pool, CancellationToken cancellationToken)
	{
		return SendWithAuthAsync(request, request.RequestUri, async, credentials, preAuthenticate, isProxyAuth: false, doRequestAuth: true, pool, cancellationToken);
	}

	public static async Task<string> GetDigestTokenForCredential(NetworkCredential credential, HttpRequestMessage request, DigestResponse digestResponse)
	{
		StringBuilder sb = System.Text.StringBuilderCache.Acquire();
		string algorithm;
		bool isAlgorithmSpecified = digestResponse.Parameters.TryGetValue("algorithm", out algorithm);
		if (isAlgorithmSpecified)
		{
			if (!algorithm.Equals("SHA-256", StringComparison.OrdinalIgnoreCase) && !algorithm.Equals("MD5", StringComparison.OrdinalIgnoreCase) && !algorithm.Equals("SHA-256-sess", StringComparison.OrdinalIgnoreCase) && !algorithm.Equals("MD5-sess", StringComparison.OrdinalIgnoreCase))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(digestResponse, $"Algorithm not supported: {algorithm}", "GetDigestTokenForCredential");
				}
				return null;
			}
		}
		else
		{
			algorithm = "MD5";
		}
		if (!digestResponse.Parameters.TryGetValue("nonce", out var nonce))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(digestResponse, "Nonce missing", "GetDigestTokenForCredential");
			}
			return null;
		}
		digestResponse.Parameters.TryGetValue("opaque", out var opaque);
		if (!digestResponse.Parameters.TryGetValue("realm", out var value))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(digestResponse, "Realm missing", "GetDigestTokenForCredential");
			}
			return null;
		}
		if (digestResponse.Parameters.TryGetValue("userhash", out var value2) && value2 == "true")
		{
			sb.AppendKeyValue("username", ComputeHash(credential.UserName + ":" + value, algorithm));
			sb.AppendKeyValue("userhash", value2, includeQuotes: false);
		}
		else if (HeaderUtilities.ContainsNonAscii(credential.UserName))
		{
			string value3 = HeaderUtilities.Encode5987(credential.UserName);
			sb.AppendKeyValue("username*", value3, includeQuotes: false);
		}
		else
		{
			sb.AppendKeyValue("username", credential.UserName);
		}
		sb.AppendKeyValue("realm", value);
		sb.AppendKeyValue("nonce", nonce);
		sb.AppendKeyValue("uri", request.RequestUri.PathAndQuery);
		string qop = "auth";
		bool isQopSpecified = digestResponse.Parameters.ContainsKey("qop");
		if (isQopSpecified)
		{
			int num = digestResponse.Parameters["qop"].IndexOf("auth-int", StringComparison.Ordinal);
			if (num != -1)
			{
				int num2 = digestResponse.Parameters["qop"].IndexOf("auth", StringComparison.Ordinal);
				if (num2 == num)
				{
					num2 = digestResponse.Parameters["qop"].IndexOf("auth", num + "auth-int".Length, StringComparison.Ordinal);
					if (num2 == -1)
					{
						qop = "auth-int";
					}
				}
			}
		}
		string cnonce = GetRandomAlphaNumericString();
		string a1 = credential.UserName + ":" + value + ":" + credential.Password;
		if (algorithm.EndsWith("sess", StringComparison.OrdinalIgnoreCase))
		{
			a1 = ComputeHash(a1, algorithm) + ":" + nonce + ":" + cnonce;
		}
		string a2 = request.Method.Method + ":" + request.RequestUri.PathAndQuery;
		if (qop == "auth-int")
		{
			string text = ((request.Content != null) ? (await request.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false)) : string.Empty);
			string data = text;
			a2 = a2 + ":" + ComputeHash(data, algorithm);
		}
		string value4 = ((!isQopSpecified) ? ComputeHash(ComputeHash(a1, algorithm) + ":" + nonce + ":" + ComputeHash(a2, algorithm), algorithm) : ComputeHash(ComputeHash(a1, algorithm) + ":" + nonce + ":00000001:" + cnonce + ":" + qop + ":" + ComputeHash(a2, algorithm), algorithm));
		sb.AppendKeyValue("response", value4, includeQuotes: true, opaque != null || isAlgorithmSpecified || isQopSpecified);
		if (opaque != null)
		{
			sb.AppendKeyValue("opaque", opaque, includeQuotes: true, isAlgorithmSpecified || isQopSpecified);
		}
		if (isAlgorithmSpecified)
		{
			sb.AppendKeyValue("algorithm", algorithm, includeQuotes: false, isQopSpecified);
		}
		if (isQopSpecified)
		{
			sb.AppendKeyValue("qop", qop, includeQuotes: false);
			sb.AppendKeyValue("nc", "00000001", includeQuotes: false);
			sb.AppendKeyValue("cnonce", cnonce, includeQuotes: true, includeComma: false);
		}
		return System.Text.StringBuilderCache.GetStringAndRelease(sb);
	}

	public static bool IsServerNonceStale(DigestResponse digestResponse)
	{
		if (digestResponse.Parameters.TryGetValue("stale", out var value))
		{
			return value == "true";
		}
		return false;
	}

	private static string GetRandomAlphaNumericString()
	{
		Span<byte> data = stackalloc byte[32];
		RandomNumberGenerator.Fill(data);
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		int num = 0;
		while (num < data.Length)
		{
			int num2 = data[num++] % 3;
			int num3 = data[num++] % ((num2 == 0) ? 10 : 26);
			stringBuilder.Append((char)(s_alphaNumChooser[num2] + num3));
		}
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	private static string ComputeHash(string data, string algorithm)
	{
		using HashAlgorithm hashAlgorithm = (algorithm.StartsWith("SHA-256", StringComparison.OrdinalIgnoreCase) ? ((HashAlgorithm)SHA256.Create()) : ((HashAlgorithm)MD5.Create()));
		Span<byte> span = stackalloc byte[hashAlgorithm.HashSize / 8];
		int bytesWritten;
		bool flag = hashAlgorithm.TryComputeHash(Encoding.UTF8.GetBytes(data), span, out bytesWritten);
		return System.HexConverter.ToString(span, System.HexConverter.Casing.Lower);
	}

	private static Task<HttpResponseMessage> InnerSendAsync(HttpRequestMessage request, bool async, bool isProxyAuth, HttpConnectionPool pool, HttpConnection connection, CancellationToken cancellationToken)
	{
		if (!isProxyAuth)
		{
			return pool.SendWithNtProxyAuthAsync(connection, request, async, cancellationToken);
		}
		return connection.SendAsyncCore(request, async, cancellationToken);
	}

	private static bool ProxySupportsConnectionAuth(HttpResponseMessage response)
	{
		if (!response.Headers.TryGetValues(KnownHeaders.ProxySupport.Descriptor, out var values))
		{
			return false;
		}
		foreach (string item in values)
		{
			if (item == "Session-Based-Authentication")
			{
				return true;
			}
		}
		return false;
	}

	private static async Task<HttpResponseMessage> SendWithNtAuthAsync(HttpRequestMessage request, Uri authUri, bool async, ICredentials credentials, bool isProxyAuth, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
	{
		HttpResponseMessage response = await InnerSendAsync(request, async, isProxyAuth, connectionPool, connection, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (!isProxyAuth && connection.Kind == HttpConnectionKind.Proxy && !ProxySupportsConnectionAuth(response))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(connection, $"Proxy doesn't support connection-based auth, uri={authUri}", "SendWithNtAuthAsync");
			}
			return response;
		}
		if (TryGetAuthenticationChallenge(response, isProxyAuth, authUri, credentials, out var challenge) && (challenge.AuthenticationType == AuthenticationType.Negotiate || challenge.AuthenticationType == AuthenticationType.Ntlm))
		{
			bool isNewConnection = false;
			bool needDrain = true;
			try
			{
				if (response.Headers.ConnectionClose.GetValueOrDefault())
				{
					connection.DetachFromPool();
					connection = await connectionPool.CreateHttp11ConnectionAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					connection.Acquire();
					isNewConnection = true;
					needDrain = false;
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(connection, $"Authentication: {challenge.AuthenticationType}, Uri: {authUri.AbsoluteUri}", "SendWithNtAuthAsync");
				}
				string text;
				if (!isProxyAuth && request.HasHeaders && request.Headers.Host != null)
				{
					text = request.Headers.Host;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(connection, $"Authentication: {challenge.AuthenticationType}, Host: {text}", "SendWithNtAuthAsync");
					}
				}
				else
				{
					UriHostNameType hostNameType = authUri.HostNameType;
					text = ((hostNameType != UriHostNameType.IPv6 && hostNameType != UriHostNameType.IPv4) ? (await Dns.GetHostEntryAsync(authUri.IdnHost, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).HostName : authUri.IdnHost);
					if (!isProxyAuth && !authUri.IsDefaultPort && UsePortInSpn)
					{
						IFormatProvider formatProvider = null;
						IFormatProvider provider = formatProvider;
						Span<char> initialBuffer = stackalloc char[128];
						DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 2, formatProvider, initialBuffer);
						handler.AppendFormatted(text);
						handler.AppendLiteral(":");
						handler.AppendFormatted(authUri.Port);
						text = string.Create(provider, initialBuffer, ref handler);
					}
				}
				string text2 = "HTTP/" + text;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(connection, $"Authentication: {challenge.AuthenticationType}, SPN: {text2}", "SendWithNtAuthAsync");
				}
				ChannelBinding channelBinding = connection.TransportContext?.GetChannelBinding(ChannelBindingKind.Endpoint);
				System.Net.NTAuthentication authContext = new System.Net.NTAuthentication(isServer: false, challenge.SchemeName, challenge.Credential, text2, System.Net.ContextFlagsPal.Connection | System.Net.ContextFlagsPal.AcceptStream, channelBinding);
				string challengeData = challenge.ChallengeData;
				try
				{
					while (true)
					{
						string challengeResponse = authContext.GetOutgoingBlob(challengeData);
						if (challengeResponse != null)
						{
							if (needDrain)
							{
								await connection.DrainResponseAsync(response, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
							}
							SetRequestAuthenticationHeaderValue(request, new AuthenticationHeaderValue(challenge.SchemeName, challengeResponse), isProxyAuth);
							response = await InnerSendAsync(request, async, isProxyAuth, connectionPool, connection, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
							if (!authContext.IsCompleted && TryGetRepeatedChallenge(response, challenge.SchemeName, isProxyAuth, out challengeData))
							{
								needDrain = true;
								continue;
							}
							break;
						}
						break;
					}
				}
				finally
				{
					authContext.CloseContext();
				}
			}
			finally
			{
				if (isNewConnection)
				{
					connection.Release();
				}
			}
		}
		return response;
	}

	public static Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpRequestMessage request, Uri proxyUri, bool async, ICredentials proxyCredentials, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
	{
		return SendWithNtAuthAsync(request, proxyUri, async, proxyCredentials, isProxyAuth: true, connection, connectionPool, cancellationToken);
	}

	public static Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpRequestMessage request, bool async, ICredentials credentials, HttpConnection connection, HttpConnectionPool connectionPool, CancellationToken cancellationToken)
	{
		return SendWithNtAuthAsync(request, request.RequestUri, async, credentials, isProxyAuth: false, connection, connectionPool, cancellationToken);
	}
}
