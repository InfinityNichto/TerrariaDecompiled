using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal abstract class HttpConnectionBase : IDisposable, IHttpTrace
{
	private string _lastDateHeaderValue;

	private string _lastServerHeaderValue;

	private readonly long _creationTickCount = Environment.TickCount64;

	public string GetResponseHeaderValueWithCaching(HeaderDescriptor descriptor, ReadOnlySpan<byte> value, Encoding valueEncoding)
	{
		if (descriptor.KnownHeader != KnownHeaders.Date)
		{
			if (descriptor.KnownHeader != KnownHeaders.Server)
			{
				return descriptor.GetHeaderValue(value, valueEncoding);
			}
			return GetOrAddCachedValue(ref _lastServerHeaderValue, descriptor, value, valueEncoding);
		}
		return GetOrAddCachedValue(ref _lastDateHeaderValue, descriptor, value, valueEncoding);
		static string GetOrAddCachedValue([NotNull] ref string cache, HeaderDescriptor descriptor, ReadOnlySpan<byte> value, Encoding encoding)
		{
			string text = cache;
			if (text == null || !ByteArrayHelpers.EqualsOrdinalAscii(text, value))
			{
				text = (cache = descriptor.GetHeaderValue(value, encoding));
			}
			return text;
		}
	}

	public abstract void Trace(string message, [CallerMemberName] string memberName = null);

	protected void TraceConnection(Stream stream)
	{
		if (stream is SslStream sslStream)
		{
			Trace($"{this}. SslProtocol:{sslStream.SslProtocol}, NegotiatedApplicationProtocol:{sslStream.NegotiatedApplicationProtocol}, NegotiatedCipherSuite:{sslStream.NegotiatedCipherSuite}, CipherAlgorithm:{sslStream.CipherAlgorithm}, CipherStrength:{sslStream.CipherStrength}, HashAlgorithm:{sslStream.HashAlgorithm}, HashStrength:{sslStream.HashStrength}, KeyExchangeAlgorithm:{sslStream.KeyExchangeAlgorithm}, KeyExchangeStrength:{sslStream.KeyExchangeStrength}, LocalCertificate:{sslStream.LocalCertificate}, RemoteCertificate:{sslStream.RemoteCertificate}", "TraceConnection");
		}
		else
		{
			Trace($"{this}", "TraceConnection");
		}
	}

	public long GetLifetimeTicks(long nowTicks)
	{
		return nowTicks - _creationTickCount;
	}

	public abstract long GetIdleTicks(long nowTicks);

	public virtual bool CheckUsabilityOnScavenge()
	{
		return true;
	}

	internal static bool IsDigit(byte c)
	{
		return (uint)(c - 48) <= 9u;
	}

	internal static int ParseStatusCode(ReadOnlySpan<byte> value)
	{
		byte b;
		byte b2;
		byte b3;
		if (value.Length != 3 || !IsDigit(b = value[0]) || !IsDigit(b2 = value[1]) || !IsDigit(b3 = value[2]))
		{
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_invalid_response_status_code, Encoding.ASCII.GetString(value)));
		}
		return 100 * (b - 48) + 10 * (b2 - 48) + (b3 - 48);
	}

	internal static void IgnoreExceptions(ValueTask<int> task)
	{
		if (task.IsCompleted)
		{
			if (task.IsFaulted)
			{
				_ = task.AsTask().Exception;
			}
		}
		else
		{
			task.AsTask().ContinueWith((Task<int> t) => t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
	}

	internal void LogExceptions(Task task)
	{
		if (task.IsCompleted)
		{
			if (task.IsFaulted)
			{
				LogFaulted(this, task);
			}
		}
		else
		{
			task.ContinueWith(delegate(Task t, object state)
			{
				LogFaulted((HttpConnectionBase)state, t);
			}, this, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
		static void LogFaulted(HttpConnectionBase connection, Task task)
		{
			Exception innerException = task.Exception.InnerException;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace($"Exception from asynchronous processing: {innerException}", "LogExceptions");
			}
		}
	}

	public abstract void Dispose();
}
