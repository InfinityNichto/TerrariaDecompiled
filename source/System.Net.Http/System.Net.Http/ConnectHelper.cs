using System.IO;
using System.Net.Quic;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal static class ConnectHelper
{
	internal sealed class CertificateCallbackMapper
	{
		public readonly Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> FromHttpClientHandler;

		public readonly RemoteCertificateValidationCallback ForSocketsHttpHandler;

		public CertificateCallbackMapper(Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> fromHttpClientHandler)
		{
			FromHttpClientHandler = fromHttpClientHandler;
			ForSocketsHttpHandler = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => FromHttpClientHandler((HttpRequestMessage)sender, certificate as X509Certificate2, chain, sslPolicyErrors);
		}
	}

	private static SslClientAuthenticationOptions SetUpRemoteCertificateValidationCallback(SslClientAuthenticationOptions sslOptions, HttpRequestMessage request)
	{
		RemoteCertificateValidationCallback remoteCertificateValidationCallback = sslOptions.RemoteCertificateValidationCallback;
		if (remoteCertificateValidationCallback != null && remoteCertificateValidationCallback.Target is CertificateCallbackMapper certificateCallbackMapper)
		{
			sslOptions = sslOptions.ShallowClone();
			Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> localFromHttpClientHandler = certificateCallbackMapper.FromHttpClientHandler;
			HttpRequestMessage localRequest = request;
			sslOptions.RemoteCertificateValidationCallback = delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
			{
				bool result = localFromHttpClientHandler(localRequest, certificate as X509Certificate2, chain, sslPolicyErrors);
				localRequest = null;
				return result;
			};
		}
		return sslOptions;
	}

	public static async ValueTask<SslStream> EstablishSslConnectionAsync(SslClientAuthenticationOptions sslOptions, HttpRequestMessage request, bool async, Stream stream, CancellationToken cancellationToken)
	{
		sslOptions = SetUpRemoteCertificateValidationCallback(sslOptions, request);
		SslStream sslStream = new SslStream(stream);
		try
		{
			if (async)
			{
				await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				using (cancellationToken.UnsafeRegister(delegate(object s)
				{
					((Stream)s).Dispose();
				}, stream))
				{
					sslStream.AuthenticateAsClient(sslOptions);
				}
			}
		}
		catch (Exception ex)
		{
			sslStream.Dispose();
			if (ex is OperationCanceledException)
			{
				throw;
			}
			if (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			throw new HttpRequestException(System.SR.net_http_ssl_connection_failed, ex);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			sslStream.Dispose();
			throw CancellationHelper.CreateOperationCanceledException(null, cancellationToken);
		}
		return sslStream;
	}

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("macos")]
	public static async ValueTask<QuicConnection> ConnectQuicAsync(HttpRequestMessage request, QuicImplementationProvider quicImplementationProvider, DnsEndPoint endPoint, SslClientAuthenticationOptions clientAuthenticationOptions, CancellationToken cancellationToken)
	{
		clientAuthenticationOptions = SetUpRemoteCertificateValidationCallback(clientAuthenticationOptions, request);
		QuicConnection con = new QuicConnection(quicImplementationProvider, endPoint, clientAuthenticationOptions);
		try
		{
			await con.ConnectAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return con;
		}
		catch (Exception error)
		{
			con.Dispose();
			throw CreateWrappedException(error, endPoint.Host, endPoint.Port, cancellationToken);
		}
	}

	internal static Exception CreateWrappedException(Exception error, string host, int port, CancellationToken cancellationToken)
	{
		if (!CancellationHelper.ShouldWrapInOperationCanceledException(error, cancellationToken))
		{
			return new HttpRequestException($"{error.Message} ({host}:{port})", error, RequestRetryType.RetryOnNextProxy);
		}
		return CancellationHelper.CreateOperationCanceledException(error, cancellationToken);
	}
}
