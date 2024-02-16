using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net;

public static class WebUtility
{
	private struct UrlDecoder
	{
		private readonly int _bufferSize;

		private int _numChars;

		private char[] _charBuffer;

		private int _numBytes;

		private byte[] _byteBuffer;

		private readonly Encoding _encoding;

		private void FlushBytes()
		{
			if (_charBuffer == null)
			{
				_charBuffer = new char[_bufferSize];
			}
			_numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
			_numBytes = 0;
		}

		internal UrlDecoder(int bufferSize, Encoding encoding)
		{
			_bufferSize = bufferSize;
			_encoding = encoding;
			_charBuffer = null;
			_numChars = 0;
			_numBytes = 0;
			_byteBuffer = null;
		}

		internal void AddChar(char ch)
		{
			if (_numBytes > 0)
			{
				FlushBytes();
			}
			if (_charBuffer == null)
			{
				_charBuffer = new char[_bufferSize];
			}
			_charBuffer[_numChars++] = ch;
		}

		internal void AddByte(byte b)
		{
			if (_byteBuffer == null)
			{
				_byteBuffer = new byte[_bufferSize];
			}
			_byteBuffer[_numBytes++] = b;
		}

		internal string GetString()
		{
			if (_numBytes > 0)
			{
				FlushBytes();
			}
			return new string(_charBuffer, 0, _numChars);
		}
	}

	private static class HtmlEntities
	{
		private static readonly Dictionary<ulong, char> s_lookupTable = InitializeLookupTable();

		private static Dictionary<ulong, char> InitializeLookupTable()
		{
			ReadOnlySpan<byte> source = new byte[2530]
			{
				116, 111, 117, 113, 0, 0, 0, 0, 34, 0,
				112, 109, 97, 0, 0, 0, 0, 0, 38, 0,
				115, 111, 112, 97, 0, 0, 0, 0, 39, 0,
				116, 108, 0, 0, 0, 0, 0, 0, 60, 0,
				116, 103, 0, 0, 0, 0, 0, 0, 62, 0,
				112, 115, 98, 110, 0, 0, 0, 0, 160, 0,
				108, 99, 120, 101, 105, 0, 0, 0, 161, 0,
				116, 110, 101, 99, 0, 0, 0, 0, 162, 0,
				100, 110, 117, 111, 112, 0, 0, 0, 163, 0,
				110, 101, 114, 114, 117, 99, 0, 0, 164, 0,
				110, 101, 121, 0, 0, 0, 0, 0, 165, 0,
				114, 97, 98, 118, 114, 98, 0, 0, 166, 0,
				116, 99, 101, 115, 0, 0, 0, 0, 167, 0,
				108, 109, 117, 0, 0, 0, 0, 0, 168, 0,
				121, 112, 111, 99, 0, 0, 0, 0, 169, 0,
				102, 100, 114, 111, 0, 0, 0, 0, 170, 0,
				111, 117, 113, 97, 108, 0, 0, 0, 171, 0,
				116, 111, 110, 0, 0, 0, 0, 0, 172, 0,
				121, 104, 115, 0, 0, 0, 0, 0, 173, 0,
				103, 101, 114, 0, 0, 0, 0, 0, 174, 0,
				114, 99, 97, 109, 0, 0, 0, 0, 175, 0,
				103, 101, 100, 0, 0, 0, 0, 0, 176, 0,
				110, 109, 115, 117, 108, 112, 0, 0, 177, 0,
				50, 112, 117, 115, 0, 0, 0, 0, 178, 0,
				51, 112, 117, 115, 0, 0, 0, 0, 179, 0,
				101, 116, 117, 99, 97, 0, 0, 0, 180, 0,
				111, 114, 99, 105, 109, 0, 0, 0, 181, 0,
				97, 114, 97, 112, 0, 0, 0, 0, 182, 0,
				116, 111, 100, 100, 105, 109, 0, 0, 183, 0,
				108, 105, 100, 101, 99, 0, 0, 0, 184, 0,
				49, 112, 117, 115, 0, 0, 0, 0, 185, 0,
				109, 100, 114, 111, 0, 0, 0, 0, 186, 0,
				111, 117, 113, 97, 114, 0, 0, 0, 187, 0,
				52, 49, 99, 97, 114, 102, 0, 0, 188, 0,
				50, 49, 99, 97, 114, 102, 0, 0, 189, 0,
				52, 51, 99, 97, 114, 102, 0, 0, 190, 0,
				116, 115, 101, 117, 113, 105, 0, 0, 191, 0,
				101, 118, 97, 114, 103, 65, 0, 0, 192, 0,
				101, 116, 117, 99, 97, 65, 0, 0, 193, 0,
				99, 114, 105, 99, 65, 0, 0, 0, 194, 0,
				101, 100, 108, 105, 116, 65, 0, 0, 195, 0,
				108, 109, 117, 65, 0, 0, 0, 0, 196, 0,
				103, 110, 105, 114, 65, 0, 0, 0, 197, 0,
				103, 105, 108, 69, 65, 0, 0, 0, 198, 0,
				108, 105, 100, 101, 99, 67, 0, 0, 199, 0,
				101, 118, 97, 114, 103, 69, 0, 0, 200, 0,
				101, 116, 117, 99, 97, 69, 0, 0, 201, 0,
				99, 114, 105, 99, 69, 0, 0, 0, 202, 0,
				108, 109, 117, 69, 0, 0, 0, 0, 203, 0,
				101, 118, 97, 114, 103, 73, 0, 0, 204, 0,
				101, 116, 117, 99, 97, 73, 0, 0, 205, 0,
				99, 114, 105, 99, 73, 0, 0, 0, 206, 0,
				108, 109, 117, 73, 0, 0, 0, 0, 207, 0,
				72, 84, 69, 0, 0, 0, 0, 0, 208, 0,
				101, 100, 108, 105, 116, 78, 0, 0, 209, 0,
				101, 118, 97, 114, 103, 79, 0, 0, 210, 0,
				101, 116, 117, 99, 97, 79, 0, 0, 211, 0,
				99, 114, 105, 99, 79, 0, 0, 0, 212, 0,
				101, 100, 108, 105, 116, 79, 0, 0, 213, 0,
				108, 109, 117, 79, 0, 0, 0, 0, 214, 0,
				115, 101, 109, 105, 116, 0, 0, 0, 215, 0,
				104, 115, 97, 108, 115, 79, 0, 0, 216, 0,
				101, 118, 97, 114, 103, 85, 0, 0, 217, 0,
				101, 116, 117, 99, 97, 85, 0, 0, 218, 0,
				99, 114, 105, 99, 85, 0, 0, 0, 219, 0,
				108, 109, 117, 85, 0, 0, 0, 0, 220, 0,
				101, 116, 117, 99, 97, 89, 0, 0, 221, 0,
				78, 82, 79, 72, 84, 0, 0, 0, 222, 0,
				103, 105, 108, 122, 115, 0, 0, 0, 223, 0,
				101, 118, 97, 114, 103, 97, 0, 0, 224, 0,
				101, 116, 117, 99, 97, 97, 0, 0, 225, 0,
				99, 114, 105, 99, 97, 0, 0, 0, 226, 0,
				101, 100, 108, 105, 116, 97, 0, 0, 227, 0,
				108, 109, 117, 97, 0, 0, 0, 0, 228, 0,
				103, 110, 105, 114, 97, 0, 0, 0, 229, 0,
				103, 105, 108, 101, 97, 0, 0, 0, 230, 0,
				108, 105, 100, 101, 99, 99, 0, 0, 231, 0,
				101, 118, 97, 114, 103, 101, 0, 0, 232, 0,
				101, 116, 117, 99, 97, 101, 0, 0, 233, 0,
				99, 114, 105, 99, 101, 0, 0, 0, 234, 0,
				108, 109, 117, 101, 0, 0, 0, 0, 235, 0,
				101, 118, 97, 114, 103, 105, 0, 0, 236, 0,
				101, 116, 117, 99, 97, 105, 0, 0, 237, 0,
				99, 114, 105, 99, 105, 0, 0, 0, 238, 0,
				108, 109, 117, 105, 0, 0, 0, 0, 239, 0,
				104, 116, 101, 0, 0, 0, 0, 0, 240, 0,
				101, 100, 108, 105, 116, 110, 0, 0, 241, 0,
				101, 118, 97, 114, 103, 111, 0, 0, 242, 0,
				101, 116, 117, 99, 97, 111, 0, 0, 243, 0,
				99, 114, 105, 99, 111, 0, 0, 0, 244, 0,
				101, 100, 108, 105, 116, 111, 0, 0, 245, 0,
				108, 109, 117, 111, 0, 0, 0, 0, 246, 0,
				101, 100, 105, 118, 105, 100, 0, 0, 247, 0,
				104, 115, 97, 108, 115, 111, 0, 0, 248, 0,
				101, 118, 97, 114, 103, 117, 0, 0, 249, 0,
				101, 116, 117, 99, 97, 117, 0, 0, 250, 0,
				99, 114, 105, 99, 117, 0, 0, 0, 251, 0,
				108, 109, 117, 117, 0, 0, 0, 0, 252, 0,
				101, 116, 117, 99, 97, 121, 0, 0, 253, 0,
				110, 114, 111, 104, 116, 0, 0, 0, 254, 0,
				108, 109, 117, 121, 0, 0, 0, 0, 255, 0,
				103, 105, 108, 69, 79, 0, 0, 0, 82, 1,
				103, 105, 108, 101, 111, 0, 0, 0, 83, 1,
				110, 111, 114, 97, 99, 83, 0, 0, 96, 1,
				110, 111, 114, 97, 99, 115, 0, 0, 97, 1,
				108, 109, 117, 89, 0, 0, 0, 0, 120, 1,
				102, 111, 110, 102, 0, 0, 0, 0, 146, 1,
				99, 114, 105, 99, 0, 0, 0, 0, 198, 2,
				101, 100, 108, 105, 116, 0, 0, 0, 220, 2,
				97, 104, 112, 108, 65, 0, 0, 0, 145, 3,
				97, 116, 101, 66, 0, 0, 0, 0, 146, 3,
				97, 109, 109, 97, 71, 0, 0, 0, 147, 3,
				97, 116, 108, 101, 68, 0, 0, 0, 148, 3,
				110, 111, 108, 105, 115, 112, 69, 0, 149, 3,
				97, 116, 101, 90, 0, 0, 0, 0, 150, 3,
				97, 116, 69, 0, 0, 0, 0, 0, 151, 3,
				97, 116, 101, 104, 84, 0, 0, 0, 152, 3,
				97, 116, 111, 73, 0, 0, 0, 0, 153, 3,
				97, 112, 112, 97, 75, 0, 0, 0, 154, 3,
				97, 100, 98, 109, 97, 76, 0, 0, 155, 3,
				117, 77, 0, 0, 0, 0, 0, 0, 156, 3,
				117, 78, 0, 0, 0, 0, 0, 0, 157, 3,
				105, 88, 0, 0, 0, 0, 0, 0, 158, 3,
				110, 111, 114, 99, 105, 109, 79, 0, 159, 3,
				105, 80, 0, 0, 0, 0, 0, 0, 160, 3,
				111, 104, 82, 0, 0, 0, 0, 0, 161, 3,
				97, 109, 103, 105, 83, 0, 0, 0, 163, 3,
				117, 97, 84, 0, 0, 0, 0, 0, 164, 3,
				110, 111, 108, 105, 115, 112, 85, 0, 165, 3,
				105, 104, 80, 0, 0, 0, 0, 0, 166, 3,
				105, 104, 67, 0, 0, 0, 0, 0, 167, 3,
				105, 115, 80, 0, 0, 0, 0, 0, 168, 3,
				97, 103, 101, 109, 79, 0, 0, 0, 169, 3,
				97, 104, 112, 108, 97, 0, 0, 0, 177, 3,
				97, 116, 101, 98, 0, 0, 0, 0, 178, 3,
				97, 109, 109, 97, 103, 0, 0, 0, 179, 3,
				97, 116, 108, 101, 100, 0, 0, 0, 180, 3,
				110, 111, 108, 105, 115, 112, 101, 0, 181, 3,
				97, 116, 101, 122, 0, 0, 0, 0, 182, 3,
				97, 116, 101, 0, 0, 0, 0, 0, 183, 3,
				97, 116, 101, 104, 116, 0, 0, 0, 184, 3,
				97, 116, 111, 105, 0, 0, 0, 0, 185, 3,
				97, 112, 112, 97, 107, 0, 0, 0, 186, 3,
				97, 100, 98, 109, 97, 108, 0, 0, 187, 3,
				117, 109, 0, 0, 0, 0, 0, 0, 188, 3,
				117, 110, 0, 0, 0, 0, 0, 0, 189, 3,
				105, 120, 0, 0, 0, 0, 0, 0, 190, 3,
				110, 111, 114, 99, 105, 109, 111, 0, 191, 3,
				105, 112, 0, 0, 0, 0, 0, 0, 192, 3,
				111, 104, 114, 0, 0, 0, 0, 0, 193, 3,
				102, 97, 109, 103, 105, 115, 0, 0, 194, 3,
				97, 109, 103, 105, 115, 0, 0, 0, 195, 3,
				117, 97, 116, 0, 0, 0, 0, 0, 196, 3,
				110, 111, 108, 105, 115, 112, 117, 0, 197, 3,
				105, 104, 112, 0, 0, 0, 0, 0, 198, 3,
				105, 104, 99, 0, 0, 0, 0, 0, 199, 3,
				105, 115, 112, 0, 0, 0, 0, 0, 200, 3,
				97, 103, 101, 109, 111, 0, 0, 0, 201, 3,
				109, 121, 115, 97, 116, 101, 104, 116, 209, 3,
				104, 105, 115, 112, 117, 0, 0, 0, 210, 3,
				118, 105, 112, 0, 0, 0, 0, 0, 214, 3,
				112, 115, 110, 101, 0, 0, 0, 0, 2, 32,
				112, 115, 109, 101, 0, 0, 0, 0, 3, 32,
				112, 115, 110, 105, 104, 116, 0, 0, 9, 32,
				106, 110, 119, 122, 0, 0, 0, 0, 12, 32,
				106, 119, 122, 0, 0, 0, 0, 0, 13, 32,
				109, 114, 108, 0, 0, 0, 0, 0, 14, 32,
				109, 108, 114, 0, 0, 0, 0, 0, 15, 32,
				104, 115, 97, 100, 110, 0, 0, 0, 19, 32,
				104, 115, 97, 100, 109, 0, 0, 0, 20, 32,
				111, 117, 113, 115, 108, 0, 0, 0, 24, 32,
				111, 117, 113, 115, 114, 0, 0, 0, 25, 32,
				111, 117, 113, 98, 115, 0, 0, 0, 26, 32,
				111, 117, 113, 100, 108, 0, 0, 0, 28, 32,
				111, 117, 113, 100, 114, 0, 0, 0, 29, 32,
				111, 117, 113, 100, 98, 0, 0, 0, 30, 32,
				114, 101, 103, 103, 97, 100, 0, 0, 32, 32,
				114, 101, 103, 103, 97, 68, 0, 0, 33, 32,
				108, 108, 117, 98, 0, 0, 0, 0, 34, 32,
				112, 105, 108, 108, 101, 104, 0, 0, 38, 32,
				108, 105, 109, 114, 101, 112, 0, 0, 48, 32,
				101, 109, 105, 114, 112, 0, 0, 0, 50, 32,
				101, 109, 105, 114, 80, 0, 0, 0, 51, 32,
				111, 117, 113, 97, 115, 108, 0, 0, 57, 32,
				111, 117, 113, 97, 115, 114, 0, 0, 58, 32,
				101, 110, 105, 108, 111, 0, 0, 0, 62, 32,
				108, 115, 97, 114, 102, 0, 0, 0, 68, 32,
				111, 114, 117, 101, 0, 0, 0, 0, 172, 32,
				101, 103, 97, 109, 105, 0, 0, 0, 17, 33,
				112, 114, 101, 105, 101, 119, 0, 0, 24, 33,
				108, 97, 101, 114, 0, 0, 0, 0, 28, 33,
				101, 100, 97, 114, 116, 0, 0, 0, 34, 33,
				109, 121, 115, 102, 101, 108, 97, 0, 53, 33,
				114, 114, 97, 108, 0, 0, 0, 0, 144, 33,
				114, 114, 97, 117, 0, 0, 0, 0, 145, 33,
				114, 114, 97, 114, 0, 0, 0, 0, 146, 33,
				114, 114, 97, 100, 0, 0, 0, 0, 147, 33,
				114, 114, 97, 104, 0, 0, 0, 0, 148, 33,
				114, 114, 97, 114, 99, 0, 0, 0, 181, 33,
				114, 114, 65, 108, 0, 0, 0, 0, 208, 33,
				114, 114, 65, 117, 0, 0, 0, 0, 209, 33,
				114, 114, 65, 114, 0, 0, 0, 0, 210, 33,
				114, 114, 65, 100, 0, 0, 0, 0, 211, 33,
				114, 114, 65, 104, 0, 0, 0, 0, 212, 33,
				108, 108, 97, 114, 111, 102, 0, 0, 0, 34,
				116, 114, 97, 112, 0, 0, 0, 0, 2, 34,
				116, 115, 105, 120, 101, 0, 0, 0, 3, 34,
				121, 116, 112, 109, 101, 0, 0, 0, 5, 34,
				97, 108, 98, 97, 110, 0, 0, 0, 7, 34,
				110, 105, 115, 105, 0, 0, 0, 0, 8, 34,
				110, 105, 116, 111, 110, 0, 0, 0, 9, 34,
				105, 110, 0, 0, 0, 0, 0, 0, 11, 34,
				100, 111, 114, 112, 0, 0, 0, 0, 15, 34,
				109, 117, 115, 0, 0, 0, 0, 0, 17, 34,
				115, 117, 110, 105, 109, 0, 0, 0, 18, 34,
				116, 115, 97, 119, 111, 108, 0, 0, 23, 34,
				99, 105, 100, 97, 114, 0, 0, 0, 26, 34,
				112, 111, 114, 112, 0, 0, 0, 0, 29, 34,
				110, 105, 102, 110, 105, 0, 0, 0, 30, 34,
				103, 110, 97, 0, 0, 0, 0, 0, 32, 34,
				100, 110, 97, 0, 0, 0, 0, 0, 39, 34,
				114, 111, 0, 0, 0, 0, 0, 0, 40, 34,
				112, 97, 99, 0, 0, 0, 0, 0, 41, 34,
				112, 117, 99, 0, 0, 0, 0, 0, 42, 34,
				116, 110, 105, 0, 0, 0, 0, 0, 43, 34,
				52, 101, 114, 101, 104, 116, 0, 0, 52, 34,
				109, 105, 115, 0, 0, 0, 0, 0, 60, 34,
				103, 110, 111, 99, 0, 0, 0, 0, 69, 34,
				112, 109, 121, 115, 97, 0, 0, 0, 72, 34,
				101, 110, 0, 0, 0, 0, 0, 0, 96, 34,
				118, 105, 117, 113, 101, 0, 0, 0, 97, 34,
				101, 108, 0, 0, 0, 0, 0, 0, 100, 34,
				101, 103, 0, 0, 0, 0, 0, 0, 101, 34,
				98, 117, 115, 0, 0, 0, 0, 0, 130, 34,
				112, 117, 115, 0, 0, 0, 0, 0, 131, 34,
				98, 117, 115, 110, 0, 0, 0, 0, 132, 34,
				101, 98, 117, 115, 0, 0, 0, 0, 134, 34,
				101, 112, 117, 115, 0, 0, 0, 0, 135, 34,
				115, 117, 108, 112, 111, 0, 0, 0, 149, 34,
				115, 101, 109, 105, 116, 111, 0, 0, 151, 34,
				112, 114, 101, 112, 0, 0, 0, 0, 165, 34,
				116, 111, 100, 115, 0, 0, 0, 0, 197, 34,
				108, 105, 101, 99, 108, 0, 0, 0, 8, 35,
				108, 105, 101, 99, 114, 0, 0, 0, 9, 35,
				114, 111, 111, 108, 102, 108, 0, 0, 10, 35,
				114, 111, 111, 108, 102, 114, 0, 0, 11, 35,
				103, 110, 97, 108, 0, 0, 0, 0, 41, 35,
				103, 110, 97, 114, 0, 0, 0, 0, 42, 35,
				122, 111, 108, 0, 0, 0, 0, 0, 202, 37,
				115, 101, 100, 97, 112, 115, 0, 0, 96, 38,
				115, 98, 117, 108, 99, 0, 0, 0, 99, 38,
				115, 116, 114, 97, 101, 104, 0, 0, 101, 38,
				115, 109, 97, 105, 100, 0, 0, 0, 102, 38
			};
			Dictionary<ulong, char> dictionary = new Dictionary<ulong, char>(source.Length / 10);
			while (!source.IsEmpty)
			{
				ulong key = BinaryPrimitives.ReadUInt64LittleEndian(source);
				char value = (char)BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(8));
				dictionary[key] = value;
				source = source.Slice(10);
			}
			return dictionary;
		}

		public static char Lookup(ReadOnlySpan<char> entity)
		{
			if (entity.Length <= 8)
			{
				s_lookupTable.TryGetValue(ToUInt64Key(entity), out var value);
				return value;
			}
			return '\0';
		}

		private static ulong ToUInt64Key(ReadOnlySpan<char> entity)
		{
			ulong num = 0uL;
			for (int i = 0; i < entity.Length; i++)
			{
				if (entity[i] > 'ÿ')
				{
					return 0uL;
				}
				num = (num << 8) | entity[i];
			}
			return num;
		}
	}

	[return: NotNullIfNotNull("value")]
	public static string? HtmlEncode(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		ReadOnlySpan<char> input = value.AsSpan();
		int num = IndexOfHtmlEncodingChars(input);
		if (num == -1)
		{
			return value;
		}
		ValueStringBuilder valueStringBuilder;
		if (value.Length < 80)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new ValueStringBuilder(value.Length + 200);
		}
		ValueStringBuilder output = valueStringBuilder;
		output.Append(input.Slice(0, num));
		HtmlEncode(input.Slice(num), ref output);
		return output.ToString();
	}

	public static void HtmlEncode(string? value, TextWriter output)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		if (string.IsNullOrEmpty(value))
		{
			output.Write(value);
			return;
		}
		ReadOnlySpan<char> input = value.AsSpan();
		int num = IndexOfHtmlEncodingChars(input);
		if (num == -1)
		{
			output.Write(value);
			return;
		}
		ValueStringBuilder valueStringBuilder;
		if (value.Length < 80)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new ValueStringBuilder(value.Length + 200);
		}
		ValueStringBuilder output2 = valueStringBuilder;
		output2.Append(input.Slice(0, num));
		HtmlEncode(input.Slice(num), ref output2);
		output.Write(output2.AsSpan());
		output2.Dispose();
	}

	private static void HtmlEncode(ReadOnlySpan<char> input, ref ValueStringBuilder output)
	{
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c <= '>')
			{
				switch (c)
				{
				case '<':
					output.Append("&lt;");
					break;
				case '>':
					output.Append("&gt;");
					break;
				case '"':
					output.Append("&quot;");
					break;
				case '\'':
					output.Append("&#39;");
					break;
				case '&':
					output.Append("&amp;");
					break;
				default:
					output.Append(c);
					break;
				}
				continue;
			}
			int num = -1;
			if (c >= '\u00a0' && c < 'Ā')
			{
				num = c;
			}
			else if (char.IsSurrogate(c))
			{
				int nextUnicodeScalarValueFromUtf16Surrogate = GetNextUnicodeScalarValueFromUtf16Surrogate(input, ref i);
				if (nextUnicodeScalarValueFromUtf16Surrogate >= 65536)
				{
					num = nextUnicodeScalarValueFromUtf16Surrogate;
				}
				else
				{
					c = (char)nextUnicodeScalarValueFromUtf16Surrogate;
				}
			}
			if (num >= 0)
			{
				output.Append("&#");
				Span<char> destination = output.AppendSpan(10);
				num.TryFormat(destination, out var charsWritten);
				output.Length -= 10 - charsWritten;
				output.Append(';');
			}
			else
			{
				output.Append(c);
			}
		}
	}

	[return: NotNullIfNotNull("value")]
	public static string? HtmlDecode(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		ReadOnlySpan<char> input = value.AsSpan();
		int num = IndexOfHtmlDecodingChars(input);
		if (num == -1)
		{
			return value;
		}
		ValueStringBuilder valueStringBuilder;
		if (value.Length <= 256)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new ValueStringBuilder(value.Length);
		}
		ValueStringBuilder output = valueStringBuilder;
		output.Append(input.Slice(0, num));
		HtmlDecode(input.Slice(num), ref output);
		return output.ToString();
	}

	public static void HtmlDecode(string? value, TextWriter output)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		if (string.IsNullOrEmpty(value))
		{
			output.Write(value);
			return;
		}
		ReadOnlySpan<char> input = value.AsSpan();
		int num = IndexOfHtmlDecodingChars(input);
		if (num == -1)
		{
			output.Write(value);
			return;
		}
		ValueStringBuilder valueStringBuilder;
		if (value.Length <= 256)
		{
			Span<char> initialBuffer = stackalloc char[256];
			valueStringBuilder = new ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new ValueStringBuilder(value.Length);
		}
		ValueStringBuilder output2 = valueStringBuilder;
		output2.Append(input.Slice(0, num));
		HtmlDecode(input.Slice(num), ref output2);
		output.Write(output2.AsSpan());
		output2.Dispose();
	}

	private static void HtmlDecode(ReadOnlySpan<char> input, ref ValueStringBuilder output)
	{
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c == '&')
			{
				ReadOnlySpan<char> span = input.Slice(i + 1);
				int num = span.IndexOfAny(';', '&');
				if (num >= 0 && span[num] == ';')
				{
					int num2 = i + 1 + num;
					if (num > 1 && span[0] == '#')
					{
						uint result;
						bool flag = ((span[1] == 'x' || span[1] == 'X') ? uint.TryParse(span.Slice(2, num - 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result) : uint.TryParse(span.Slice(1, num - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out result));
						if (flag)
						{
							flag = result < 55296 || (57343 < result && result <= 1114111);
						}
						if (flag)
						{
							if (result <= 65535)
							{
								output.Append((char)result);
							}
							else
							{
								ConvertSmpToUtf16(result, out var leadingSurrogate, out var trailingSurrogate);
								output.Append(leadingSurrogate);
								output.Append(trailingSurrogate);
							}
							i = num2;
							continue;
						}
					}
					else
					{
						ReadOnlySpan<char> readOnlySpan = span.Slice(0, num);
						i = num2;
						char c2 = HtmlEntities.Lookup(readOnlySpan);
						if (c2 == '\0')
						{
							output.Append('&');
							output.Append(readOnlySpan);
							output.Append(';');
							continue;
						}
						c = c2;
					}
				}
			}
			output.Append(c);
		}
	}

	private static int IndexOfHtmlEncodingChars(ReadOnlySpan<char> input)
	{
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c <= '>')
			{
				switch (c)
				{
				case '"':
				case '&':
				case '\'':
				case '<':
				case '>':
					return i;
				}
				continue;
			}
			if (c >= '\u00a0' && c < 'Ā')
			{
				return i;
			}
			if (char.IsSurrogate(c))
			{
				return i;
			}
		}
		return -1;
	}

	private static void GetEncodedBytes(byte[] originalBytes, int offset, int count, byte[] expandedBytes)
	{
		int num = 0;
		int num2 = offset + count;
		for (int i = offset; i < num2; i++)
		{
			byte b = originalBytes[i];
			char c = (char)b;
			if (IsUrlSafeChar(c))
			{
				expandedBytes[num++] = b;
				continue;
			}
			if (c == ' ')
			{
				expandedBytes[num++] = 43;
				continue;
			}
			expandedBytes[num++] = 37;
			expandedBytes[num++] = (byte)HexConverter.ToCharUpper(b >> 4);
			expandedBytes[num++] = (byte)HexConverter.ToCharUpper(b);
		}
	}

	[return: NotNullIfNotNull("value")]
	public static string? UrlEncode(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		int num = 0;
		int num2 = 0;
		foreach (char c in value)
		{
			if (IsUrlSafeChar(c))
			{
				num++;
			}
			else if (c == ' ')
			{
				num2++;
			}
		}
		int num3 = num + num2;
		if (num3 == value.Length)
		{
			if (num2 != 0)
			{
				return value.Replace(' ', '+');
			}
			return value;
		}
		int byteCount = Encoding.UTF8.GetByteCount(value);
		int num4 = byteCount - num3;
		int num5 = num4 * 2;
		byte[] array = new byte[byteCount + num5];
		Encoding.UTF8.GetBytes(value, 0, value.Length, array, num5);
		GetEncodedBytes(array, num5, byteCount, array);
		return Encoding.UTF8.GetString(array);
	}

	[return: NotNullIfNotNull("value")]
	public static byte[]? UrlEncodeToBytes(byte[]? value, int offset, int count)
	{
		if (!ValidateUrlEncodingParameters(value, offset, count))
		{
			return null;
		}
		bool flag = false;
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			char c = (char)value[offset + i];
			if (c == ' ')
			{
				flag = true;
			}
			else if (!IsUrlSafeChar(c))
			{
				num++;
			}
		}
		if (!flag && num == 0)
		{
			byte[] array = new byte[count];
			Buffer.BlockCopy(value, offset, array, 0, count);
			return array;
		}
		byte[] array2 = new byte[count + num * 2];
		GetEncodedBytes(value, offset, count, array2);
		return array2;
	}

	[return: NotNullIfNotNull("value")]
	private static string UrlDecodeInternal(string value, Encoding encoding)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		int length = value.Length;
		UrlDecoder urlDecoder = new UrlDecoder(length, encoding);
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < length; i++)
		{
			char c = value[i];
			switch (c)
			{
			case '+':
				flag2 = true;
				c = ' ';
				break;
			case '%':
				if (i < length - 2)
				{
					int num = HexConverter.FromChar(value[i + 1]);
					int num2 = HexConverter.FromChar(value[i + 2]);
					if ((num | num2) != 255)
					{
						byte b = (byte)((num << 4) | num2);
						i += 2;
						urlDecoder.AddByte(b);
						flag = true;
						continue;
					}
				}
				break;
			}
			if ((c & 0xFF80) == 0)
			{
				urlDecoder.AddByte((byte)c);
			}
			else
			{
				urlDecoder.AddChar(c);
			}
		}
		if (!flag)
		{
			if (flag2)
			{
				return value.Replace('+', ' ');
			}
			return value;
		}
		return urlDecoder.GetString();
	}

	[return: NotNullIfNotNull("bytes")]
	private static byte[] UrlDecodeInternal(byte[] bytes, int offset, int count)
	{
		if (!ValidateUrlEncodingParameters(bytes, offset, count))
		{
			return null;
		}
		int num = 0;
		byte[] array = new byte[count];
		for (int i = 0; i < count; i++)
		{
			int num2 = offset + i;
			byte b = bytes[num2];
			switch (b)
			{
			case 43:
				b = 32;
				break;
			case 37:
				if (i < count - 2)
				{
					int num3 = HexConverter.FromChar(bytes[num2 + 1]);
					int num4 = HexConverter.FromChar(bytes[num2 + 2]);
					if ((num3 | num4) != 255)
					{
						b = (byte)((num3 << 4) | num4);
						i += 2;
					}
				}
				break;
			}
			array[num++] = b;
		}
		if (num < array.Length)
		{
			Array.Resize(ref array, num);
		}
		return array;
	}

	[return: NotNullIfNotNull("encodedValue")]
	public static string? UrlDecode(string? encodedValue)
	{
		return UrlDecodeInternal(encodedValue, Encoding.UTF8);
	}

	[return: NotNullIfNotNull("encodedValue")]
	public static byte[]? UrlDecodeToBytes(byte[]? encodedValue, int offset, int count)
	{
		return UrlDecodeInternal(encodedValue, offset, count);
	}

	private static void ConvertSmpToUtf16(uint smpChar, out char leadingSurrogate, out char trailingSurrogate)
	{
		int num = (int)(smpChar - 65536);
		leadingSurrogate = (char)(num / 1024 + 55296);
		trailingSurrogate = (char)(num % 1024 + 56320);
	}

	private static int GetNextUnicodeScalarValueFromUtf16Surrogate(ReadOnlySpan<char> input, ref int index)
	{
		if (input.Length - index <= 1)
		{
			return 65533;
		}
		char c = input[index];
		char c2 = input[index + 1];
		if (!char.IsSurrogatePair(c, c2))
		{
			return 65533;
		}
		index++;
		return (c - 55296) * 1024 + (c2 - 56320) + 65536;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsUrlSafeChar(char ch)
	{
		if ((uint)(ch - 97) > 25u && (uint)(ch - 65) > 25u && ((uint)(ch - 32) > 25u || ((1 << ch - 32) & 0x3FF6702) == 0))
		{
			return ch == '_';
		}
		return true;
	}

	private static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
	{
		if (bytes == null && count == 0)
		{
			return false;
		}
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		if (offset < 0 || offset > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || offset + count > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		return true;
	}

	private static int IndexOfHtmlDecodingChars(ReadOnlySpan<char> input)
	{
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c == '&' || char.IsSurrogate(c))
			{
				return i;
			}
		}
		return -1;
	}
}
