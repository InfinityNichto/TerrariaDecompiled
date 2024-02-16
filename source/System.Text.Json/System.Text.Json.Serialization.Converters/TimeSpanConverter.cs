using System.Buffers;
using System.Buffers.Text;

namespace System.Text.Json.Serialization.Converters;

internal sealed class TimeSpanConverter : JsonConverter<TimeSpan>
{
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
		{
			throw ThrowHelper.GetInvalidOperationException_ExpectedString(reader.TokenType);
		}
		bool stringHasEscaping = reader._stringHasEscaping;
		int num = (stringHasEscaping ? 156 : 26);
		ReadOnlySpan<byte> readOnlySpan = default(Span<byte>);
		if (reader.HasValueSequence)
		{
			ReadOnlySequence<byte> source = reader.ValueSequence;
			long length = source.Length;
			if (!JsonHelpers.IsInRangeInclusive(length, 8L, num))
			{
				throw ThrowHelper.GetFormatException(DataType.TimeSpan);
			}
			Span<byte> destination = stackalloc byte[stringHasEscaping ? 156 : 26];
			source.CopyTo(destination);
			readOnlySpan = destination.Slice(0, (int)length);
		}
		else
		{
			readOnlySpan = reader.ValueSpan;
			if (!JsonHelpers.IsInRangeInclusive(readOnlySpan.Length, 8, num))
			{
				throw ThrowHelper.GetFormatException(DataType.TimeSpan);
			}
		}
		if (stringHasEscaping)
		{
			int idx = readOnlySpan.IndexOf<byte>(92);
			Span<byte> destination2 = stackalloc byte[156];
			JsonReaderHelper.Unescape(readOnlySpan, destination2, idx, out var written);
			readOnlySpan = destination2.Slice(0, written);
		}
		byte b = readOnlySpan[0];
		if (!JsonHelpers.IsDigit(b) && b != 45)
		{
			throw ThrowHelper.GetFormatException(DataType.TimeSpan);
		}
		if (Utf8Parser.TryParse(readOnlySpan, out TimeSpan value, out int bytesConsumed, 'c') && readOnlySpan.Length == bytesConsumed)
		{
			return value;
		}
		throw ThrowHelper.GetFormatException(DataType.TimeSpan);
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
	{
		Span<byte> destination = stackalloc byte[26];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten, 'c');
		writer.WriteStringValue(destination.Slice(0, bytesWritten));
	}
}
