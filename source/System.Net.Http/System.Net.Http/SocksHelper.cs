using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal static class SocksHelper
{
	public static async ValueTask EstablishSocksTunnelAsync(Stream stream, string host, int port, Uri proxyUri, ICredentials proxyCredentials, bool async, CancellationToken cancellationToken)
	{
		using (cancellationToken.Register(delegate(object s)
		{
			((Stream)s).Dispose();
		}, stream))
		{
			_ = 2;
			try
			{
				NetworkCredential credentials = proxyCredentials?.GetCredential(proxyUri, proxyUri.Scheme);
				if (string.Equals(proxyUri.Scheme, "socks5", StringComparison.OrdinalIgnoreCase))
				{
					await EstablishSocks5TunnelAsync(stream, host, port, proxyUri, credentials, async).ConfigureAwait(continueOnCapturedContext: false);
				}
				else if (string.Equals(proxyUri.Scheme, "socks4a", StringComparison.OrdinalIgnoreCase))
				{
					await EstablishSocks4TunnelAsync(stream, isVersion4a: true, host, port, proxyUri, credentials, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else if (string.Equals(proxyUri.Scheme, "socks4", StringComparison.OrdinalIgnoreCase))
				{
					await EstablishSocks4TunnelAsync(stream, isVersion4a: false, host, port, proxyUri, credentials, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch
			{
				stream.Dispose();
				throw;
			}
		}
	}

	private static async ValueTask EstablishSocks5TunnelAsync(Stream stream, string host, int port, Uri proxyUri, NetworkCredential credentials, bool async)
	{
		byte[] buffer = ArrayPool<byte>.Shared.Rent(513);
		try
		{
			buffer[0] = 5;
			if (credentials == null)
			{
				buffer[1] = 1;
				buffer[2] = 0;
			}
			else
			{
				buffer[1] = 2;
				buffer[2] = 0;
				buffer[3] = 2;
			}
			await WriteAsync(stream, buffer.AsMemory(0, buffer[1] + 2), async).ConfigureAwait(continueOnCapturedContext: false);
			await ReadToFillAsync(stream, buffer.AsMemory(0, 2), async).ConfigureAwait(continueOnCapturedContext: false);
			VerifyProtocolVersion(5, buffer[0]);
			switch (buffer[1])
			{
			case 2:
			{
				if (credentials == null)
				{
					throw new SocksException(System.SR.net_socks_auth_required);
				}
				buffer[0] = 1;
				byte b = (buffer[1] = EncodeString(credentials.UserName, buffer.AsSpan(2), "UserName"));
				await WriteAsync(stream, buffer.AsMemory(0, 3 + b + (buffer[2 + b] = EncodeString(credentials.Password, buffer.AsSpan(3 + b), "Password"))), async).ConfigureAwait(continueOnCapturedContext: false);
				await ReadToFillAsync(stream, buffer.AsMemory(0, 2), async).ConfigureAwait(continueOnCapturedContext: false);
				if (buffer[0] != 1 || buffer[1] != 0)
				{
					throw new SocksException(System.SR.net_socks_auth_failed);
				}
				break;
			}
			default:
				throw new SocksException(System.SR.net_socks_no_auth_method);
			case 0:
				break;
			}
			buffer[0] = 5;
			buffer[1] = 1;
			buffer[2] = 0;
			int num;
			if (IPAddress.TryParse(host, out IPAddress address))
			{
				if (address.AddressFamily == AddressFamily.InterNetwork)
				{
					buffer[3] = 1;
					address.TryWriteBytes(buffer.AsSpan(4), out var _);
					num = 4;
				}
				else
				{
					buffer[3] = 4;
					address.TryWriteBytes(buffer.AsSpan(4), out var _);
					num = 16;
				}
			}
			else
			{
				buffer[3] = 3;
				num = (buffer[4] = EncodeString(host, buffer.AsSpan(5), "host")) + 1;
			}
			BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(num + 4), (ushort)port);
			await WriteAsync(stream, buffer.AsMemory(0, num + 6), async).ConfigureAwait(continueOnCapturedContext: false);
			await ReadToFillAsync(stream, buffer.AsMemory(0, 5), async).ConfigureAwait(continueOnCapturedContext: false);
			VerifyProtocolVersion(5, buffer[0]);
			if (buffer[1] != 0)
			{
				throw new SocksException(System.SR.net_socks_connection_failed);
			}
			await ReadToFillAsync(stream, buffer.AsMemory(0, buffer[3] switch
			{
				1 => 5, 
				4 => 17, 
				3 => buffer[4] + 2, 
				_ => throw new SocksException(System.SR.net_socks_bad_address_type), 
			}), async).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private static async ValueTask EstablishSocks4TunnelAsync(Stream stream, bool isVersion4a, string host, int port, Uri proxyUri, NetworkCredential credentials, bool async, CancellationToken cancellationToken)
	{
		byte[] buffer = ArrayPool<byte>.Shared.Rent(513);
		try
		{
			buffer[0] = 4;
			buffer[1] = 1;
			BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), (ushort)port);
			IPAddress iPAddress = null;
			if (IPAddress.TryParse(host, out IPAddress address))
			{
				if (address.AddressFamily == AddressFamily.InterNetwork)
				{
					iPAddress = address;
				}
				else
				{
					if (!address.IsIPv4MappedToIPv6)
					{
						throw new SocksException(System.SR.net_socks_ipv6_notsupported);
					}
					iPAddress = address.MapToIPv4();
				}
			}
			else if (!isVersion4a)
			{
				IPAddress[] array2;
				try
				{
					IPAddress[] array = ((!async) ? Dns.GetHostAddresses(host, AddressFamily.InterNetwork) : (await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
					array2 = array;
				}
				catch (Exception innerException)
				{
					throw new SocksException(System.SR.net_socks_no_ipv4_address, innerException);
				}
				if (array2.Length == 0)
				{
					throw new SocksException(System.SR.net_socks_no_ipv4_address);
				}
				iPAddress = array2[0];
			}
			if (iPAddress == null)
			{
				buffer[4] = 0;
				buffer[5] = 0;
				buffer[6] = 0;
				buffer[7] = byte.MaxValue;
			}
			else
			{
				iPAddress.TryWriteBytes(buffer.AsSpan(4), out var _);
			}
			byte b = EncodeString(credentials?.UserName, buffer.AsSpan(8), "UserName");
			buffer[8 + b] = 0;
			int num = 9 + b;
			if (iPAddress == null)
			{
				byte b2 = EncodeString(host, buffer.AsSpan(num), "host");
				buffer[num + b2] = 0;
				num += b2 + 1;
			}
			await WriteAsync(stream, buffer.AsMemory(0, num), async).ConfigureAwait(continueOnCapturedContext: false);
			await ReadToFillAsync(stream, buffer.AsMemory(0, 8), async).ConfigureAwait(continueOnCapturedContext: false);
			switch (buffer[1])
			{
			case 93:
				throw new SocksException(System.SR.net_socks_auth_failed);
			default:
				throw new SocksException(System.SR.net_socks_connection_failed);
			case 90:
				break;
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private static byte EncodeString(ReadOnlySpan<char> chars, Span<byte> buffer, string parameterName)
	{
		try
		{
			return checked((byte)Encoding.UTF8.GetBytes(chars, buffer));
		}
		catch
		{
			throw new SocksException(System.SR.Format(System.SR.net_socks_string_too_long, parameterName));
		}
	}

	private static void VerifyProtocolVersion(byte expected, byte version)
	{
		if (expected != version)
		{
			throw new SocksException(System.SR.Format(System.SR.net_socks_unexpected_version, expected, version));
		}
	}

	private static ValueTask WriteAsync(Stream stream, Memory<byte> buffer, bool async)
	{
		if (async)
		{
			return stream.WriteAsync(buffer);
		}
		stream.Write(buffer.Span);
		return default(ValueTask);
	}

	private static async ValueTask ReadToFillAsync(Stream stream, Memory<byte> buffer, bool async)
	{
		while (buffer.Length != 0)
		{
			int num = ((!async) ? stream.Read(buffer.Span) : (await stream.ReadAsync(buffer).ConfigureAwait(continueOnCapturedContext: false)));
			int num2 = num;
			if (num2 == 0)
			{
				throw new IOException(System.SR.net_http_invalid_response_premature_eof);
			}
			Memory<byte> memory = buffer;
			buffer = memory[num2..];
		}
	}
}
