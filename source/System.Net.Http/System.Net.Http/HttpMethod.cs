using System.Diagnostics.CodeAnalysis;
using System.Net.Http.QPack;
using System.Threading;

namespace System.Net.Http;

public class HttpMethod : IEquatable<HttpMethod>
{
	private readonly string _method;

	private readonly int? _http3Index;

	private int _hashcode;

	private static readonly HttpMethod s_getMethod = new HttpMethod("GET", 17);

	private static readonly HttpMethod s_putMethod = new HttpMethod("PUT", 21);

	private static readonly HttpMethod s_postMethod = new HttpMethod("POST", 20);

	private static readonly HttpMethod s_deleteMethod = new HttpMethod("DELETE", 16);

	private static readonly HttpMethod s_headMethod = new HttpMethod("HEAD", 18);

	private static readonly HttpMethod s_optionsMethod = new HttpMethod("OPTIONS", 19);

	private static readonly HttpMethod s_traceMethod = new HttpMethod("TRACE", -1);

	private static readonly HttpMethod s_patchMethod = new HttpMethod("PATCH", -1);

	private static readonly HttpMethod s_connectMethod = new HttpMethod("CONNECT", 15);

	private byte[] _http3EncodedBytes;

	public static HttpMethod Get => s_getMethod;

	public static HttpMethod Put => s_putMethod;

	public static HttpMethod Post => s_postMethod;

	public static HttpMethod Delete => s_deleteMethod;

	public static HttpMethod Head => s_headMethod;

	public static HttpMethod Options => s_optionsMethod;

	public static HttpMethod Trace => s_traceMethod;

	public static HttpMethod Patch => s_patchMethod;

	internal static HttpMethod Connect => s_connectMethod;

	public string Method => _method;

	internal bool MustHaveRequestBody
	{
		get
		{
			if ((object)this != Get && (object)this != Head && (object)this != Connect && (object)this != Options)
			{
				return (object)this != Delete;
			}
			return false;
		}
	}

	internal byte[] Http3EncodedBytes
	{
		get
		{
			byte[] array = Volatile.Read(ref _http3EncodedBytes);
			ref byte[] http3EncodedBytes;
			byte[] array2;
			if (array == null)
			{
				http3EncodedBytes = ref _http3EncodedBytes;
				int? http3Index = _http3Index;
				if (http3Index.HasValue)
				{
					int valueOrDefault = http3Index.GetValueOrDefault();
					if (valueOrDefault >= 0)
					{
						array2 = QPackEncoder.EncodeStaticIndexedHeaderFieldToArray(valueOrDefault);
						goto IL_0046;
					}
				}
				array2 = QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(17, _method);
				goto IL_0046;
			}
			goto IL_004d;
			IL_004d:
			return array;
			IL_0046:
			array = array2;
			Volatile.Write(ref http3EncodedBytes, array2);
			goto IL_004d;
		}
	}

	public HttpMethod(string method)
	{
		if (string.IsNullOrEmpty(method))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "method");
		}
		if (HttpRuleParser.GetTokenLength(method, 0) != method.Length)
		{
			throw new FormatException(System.SR.net_http_httpmethod_format_error);
		}
		_method = method;
	}

	private HttpMethod(string method, int http3StaticTableIndex)
	{
		_method = method;
		_http3Index = http3StaticTableIndex;
	}

	public bool Equals([NotNullWhen(true)] HttpMethod? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)_method == other._method)
		{
			return true;
		}
		return string.Equals(_method, other._method, StringComparison.OrdinalIgnoreCase);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as HttpMethod);
	}

	public override int GetHashCode()
	{
		if (_hashcode == 0)
		{
			_hashcode = StringComparer.OrdinalIgnoreCase.GetHashCode(_method);
		}
		return _hashcode;
	}

	public override string ToString()
	{
		return _method;
	}

	public static bool operator ==(HttpMethod? left, HttpMethod? right)
	{
		if ((object)left != null && (object)right != null)
		{
			return left.Equals(right);
		}
		return (object)left == right;
	}

	public static bool operator !=(HttpMethod? left, HttpMethod? right)
	{
		return !(left == right);
	}

	internal static HttpMethod Normalize(HttpMethod method)
	{
		int? http3Index = method._http3Index;
		if (!http3Index.HasValue && method._method.Length >= 3)
		{
			HttpMethod httpMethod = (method._method[0] | 0x20) switch
			{
				99 => s_connectMethod, 
				100 => s_deleteMethod, 
				103 => s_getMethod, 
				104 => s_headMethod, 
				111 => s_optionsMethod, 
				112 => method._method.Length switch
				{
					3 => s_putMethod, 
					4 => s_postMethod, 
					_ => s_patchMethod, 
				}, 
				116 => s_traceMethod, 
				_ => null, 
			};
			if ((object)httpMethod != null && string.Equals(method._method, httpMethod._method, StringComparison.OrdinalIgnoreCase))
			{
				return httpMethod;
			}
		}
		return method;
	}
}
