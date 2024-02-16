using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class StreamReader : TextReader
{
	private sealed class NullStreamReader : StreamReader
	{
		public override Encoding CurrentEncoding => Encoding.Unicode;

		protected override void Dispose(bool disposing)
		{
		}

		public override int Peek()
		{
			return -1;
		}

		public override int Read()
		{
			return -1;
		}

		public override int Read(char[] buffer, int index, int count)
		{
			return 0;
		}

		public override string ReadLine()
		{
			return null;
		}

		public override string ReadToEnd()
		{
			return string.Empty;
		}

		internal override int ReadBuffer()
		{
			return 0;
		}
	}

	public new static readonly StreamReader Null = new NullStreamReader();

	private readonly Stream _stream;

	private Encoding _encoding;

	private Decoder _decoder;

	private readonly byte[] _byteBuffer;

	private char[] _charBuffer;

	private int _charPos;

	private int _charLen;

	private int _byteLen;

	private int _bytePos;

	private int _maxCharsPerBuffer;

	private bool _disposed;

	private bool _detectEncoding;

	private bool _checkPreamble;

	private bool _isBlocked;

	private readonly bool _closable;

	private Task _asyncReadTask = Task.CompletedTask;

	public virtual Encoding CurrentEncoding => _encoding;

	public virtual Stream BaseStream => _stream;

	public bool EndOfStream
	{
		get
		{
			ThrowIfDisposed();
			CheckAsyncTaskInProgress();
			if (_charPos < _charLen)
			{
				return false;
			}
			int num = ReadBuffer();
			return num == 0;
		}
	}

	private void CheckAsyncTaskInProgress()
	{
		if (!_asyncReadTask.IsCompleted)
		{
			ThrowAsyncIOInProgress();
		}
	}

	[DoesNotReturn]
	private static void ThrowAsyncIOInProgress()
	{
		throw new InvalidOperationException(SR.InvalidOperation_AsyncIOInProgress);
	}

	private StreamReader()
	{
		_stream = Stream.Null;
		_closable = true;
	}

	public StreamReader(Stream stream)
		: this(stream, detectEncodingFromByteOrderMarks: true)
	{
	}

	public StreamReader(Stream stream, bool detectEncodingFromByteOrderMarks)
		: this(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, 1024, leaveOpen: false)
	{
	}

	public StreamReader(Stream stream, Encoding encoding)
		: this(stream, encoding, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: false)
	{
	}

	public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
		: this(stream, encoding, detectEncodingFromByteOrderMarks, 1024, leaveOpen: false)
	{
	}

	public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
		: this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen: false)
	{
	}

	public StreamReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false)
	{
		if (stream == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream);
		}
		if (encoding == null)
		{
			encoding = Encoding.UTF8;
		}
		if (!stream.CanRead)
		{
			throw new ArgumentException(SR.Argument_StreamNotReadable);
		}
		if (bufferSize == -1)
		{
			bufferSize = 1024;
		}
		else if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", SR.ArgumentOutOfRange_NeedPosNum);
		}
		_stream = stream;
		_encoding = encoding;
		_decoder = encoding.GetDecoder();
		if (bufferSize < 128)
		{
			bufferSize = 128;
		}
		_byteBuffer = new byte[bufferSize];
		_maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
		_charBuffer = new char[_maxCharsPerBuffer];
		_detectEncoding = detectEncodingFromByteOrderMarks;
		_checkPreamble = encoding.Preamble.Length > 0;
		_closable = !leaveOpen;
	}

	public StreamReader(string path)
		: this(path, detectEncodingFromByteOrderMarks: true)
	{
	}

	public StreamReader(string path, bool detectEncodingFromByteOrderMarks)
		: this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks, 1024)
	{
	}

	public StreamReader(string path, Encoding encoding)
		: this(path, encoding, detectEncodingFromByteOrderMarks: true, 1024)
	{
	}

	public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
		: this(path, encoding, detectEncodingFromByteOrderMarks, 1024)
	{
	}

	public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
		: this(ValidateArgsAndOpenPath(path, encoding, bufferSize), encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen: false)
	{
	}

	public StreamReader(string path, FileStreamOptions options)
		: this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, options)
	{
	}

	public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, FileStreamOptions options)
		: this(ValidateArgsAndOpenPath(path, encoding, options), encoding, detectEncodingFromByteOrderMarks, 1024)
	{
	}

	private static Stream ValidateArgsAndOpenPath(string path, Encoding encoding, FileStreamOptions options)
	{
		ValidateArgs(path, encoding);
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		if ((options.Access & FileAccess.Read) == 0)
		{
			throw new ArgumentException(SR.Argument_StreamNotReadable, "options");
		}
		return new FileStream(path, options);
	}

	private static Stream ValidateArgsAndOpenPath(string path, Encoding encoding, int bufferSize)
	{
		ValidateArgs(path, encoding);
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", SR.ArgumentOutOfRange_NeedPosNum);
		}
		return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
	}

	private static void ValidateArgs(string path, Encoding encoding)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath);
		}
	}

	public override void Close()
	{
		Dispose(disposing: true);
	}

	protected override void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (!_closable)
		{
			return;
		}
		try
		{
			if (disposing)
			{
				_stream.Close();
			}
		}
		finally
		{
			_charPos = 0;
			_charLen = 0;
			base.Dispose(disposing);
		}
	}

	public void DiscardBufferedData()
	{
		CheckAsyncTaskInProgress();
		_byteLen = 0;
		_charLen = 0;
		_charPos = 0;
		if (_encoding != null)
		{
			_decoder = _encoding.GetDecoder();
		}
		_isBlocked = false;
	}

	public override int Peek()
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (_charPos == _charLen && (_isBlocked || ReadBuffer() == 0))
		{
			return -1;
		}
		return _charBuffer[_charPos];
	}

	public override int Read()
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (_charPos == _charLen && ReadBuffer() == 0)
		{
			return -1;
		}
		int result = _charBuffer[_charPos];
		_charPos++;
		return result;
	}

	public override int Read(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		return ReadSpan(new Span<char>(buffer, index, count));
	}

	public override int Read(Span<char> buffer)
	{
		if (!(GetType() == typeof(StreamReader)))
		{
			return base.Read(buffer);
		}
		return ReadSpan(buffer);
	}

	private int ReadSpan(Span<char> buffer)
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		int num = 0;
		bool readToUserBuffer = false;
		int num2 = buffer.Length;
		while (num2 > 0)
		{
			int num3 = _charLen - _charPos;
			if (num3 == 0)
			{
				num3 = ReadBuffer(buffer.Slice(num), out readToUserBuffer);
			}
			if (num3 == 0)
			{
				break;
			}
			if (num3 > num2)
			{
				num3 = num2;
			}
			if (!readToUserBuffer)
			{
				new Span<char>(_charBuffer, _charPos, num3).CopyTo(buffer.Slice(num));
				_charPos += num3;
			}
			num += num3;
			num2 -= num3;
			if (_isBlocked)
			{
				break;
			}
		}
		return num;
	}

	public override string ReadToEnd()
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		StringBuilder stringBuilder = new StringBuilder(_charLen - _charPos);
		do
		{
			stringBuilder.Append(_charBuffer, _charPos, _charLen - _charPos);
			_charPos = _charLen;
			ReadBuffer();
		}
		while (_charLen > 0);
		return stringBuilder.ToString();
	}

	public override int ReadBlock(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return base.ReadBlock(buffer, index, count);
	}

	public override int ReadBlock(Span<char> buffer)
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadBlock(buffer);
		}
		int num = 0;
		int num2;
		do
		{
			num2 = ReadSpan(buffer.Slice(num));
			num += num2;
		}
		while (num2 > 0 && num < buffer.Length);
		return num;
	}

	private void CompressBuffer(int n)
	{
		Buffer.BlockCopy(_byteBuffer, n, _byteBuffer, 0, _byteLen - n);
		_byteLen -= n;
	}

	private void DetectEncoding()
	{
		if (_byteLen < 2)
		{
			return;
		}
		_detectEncoding = false;
		bool flag = false;
		if (_byteBuffer[0] == 254 && _byteBuffer[1] == byte.MaxValue)
		{
			_encoding = Encoding.BigEndianUnicode;
			CompressBuffer(2);
			flag = true;
		}
		else if (_byteBuffer[0] == byte.MaxValue && _byteBuffer[1] == 254)
		{
			if (_byteLen < 4 || _byteBuffer[2] != 0 || _byteBuffer[3] != 0)
			{
				_encoding = Encoding.Unicode;
				CompressBuffer(2);
				flag = true;
			}
			else
			{
				_encoding = Encoding.UTF32;
				CompressBuffer(4);
				flag = true;
			}
		}
		else if (_byteLen >= 3 && _byteBuffer[0] == 239 && _byteBuffer[1] == 187 && _byteBuffer[2] == 191)
		{
			_encoding = Encoding.UTF8;
			CompressBuffer(3);
			flag = true;
		}
		else if (_byteLen >= 4 && _byteBuffer[0] == 0 && _byteBuffer[1] == 0 && _byteBuffer[2] == 254 && _byteBuffer[3] == byte.MaxValue)
		{
			_encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
			CompressBuffer(4);
			flag = true;
		}
		else if (_byteLen == 2)
		{
			_detectEncoding = true;
		}
		if (flag)
		{
			_decoder = _encoding.GetDecoder();
			int maxCharCount = _encoding.GetMaxCharCount(_byteBuffer.Length);
			if (maxCharCount > _maxCharsPerBuffer)
			{
				_charBuffer = new char[maxCharCount];
			}
			_maxCharsPerBuffer = maxCharCount;
		}
	}

	private bool IsPreamble()
	{
		if (!_checkPreamble)
		{
			return _checkPreamble;
		}
		ReadOnlySpan<byte> preamble = _encoding.Preamble;
		int num = ((_byteLen >= preamble.Length) ? (preamble.Length - _bytePos) : (_byteLen - _bytePos));
		int num2 = 0;
		while (num2 < num)
		{
			if (_byteBuffer[_bytePos] != preamble[_bytePos])
			{
				_bytePos = 0;
				_checkPreamble = false;
				break;
			}
			num2++;
			_bytePos++;
		}
		if (_checkPreamble && _bytePos == preamble.Length)
		{
			CompressBuffer(preamble.Length);
			_bytePos = 0;
			_checkPreamble = false;
			_detectEncoding = false;
		}
		return _checkPreamble;
	}

	internal virtual int ReadBuffer()
	{
		_charLen = 0;
		_charPos = 0;
		if (!_checkPreamble)
		{
			_byteLen = 0;
		}
		do
		{
			if (_checkPreamble)
			{
				int num = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
				if (num == 0)
				{
					if (_byteLen > 0)
					{
						_charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
						_bytePos = (_byteLen = 0);
					}
					return _charLen;
				}
				_byteLen += num;
			}
			else
			{
				_byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
				if (_byteLen == 0)
				{
					return _charLen;
				}
			}
			_isBlocked = _byteLen < _byteBuffer.Length;
			if (!IsPreamble())
			{
				if (_detectEncoding && _byteLen >= 2)
				{
					DetectEncoding();
				}
				_charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
			}
		}
		while (_charLen == 0);
		return _charLen;
	}

	private int ReadBuffer(Span<char> userBuffer, out bool readToUserBuffer)
	{
		_charLen = 0;
		_charPos = 0;
		if (!_checkPreamble)
		{
			_byteLen = 0;
		}
		int num = 0;
		readToUserBuffer = userBuffer.Length >= _maxCharsPerBuffer;
		do
		{
			if (_checkPreamble)
			{
				int num2 = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);
				if (num2 == 0)
				{
					if (_byteLen > 0)
					{
						if (readToUserBuffer)
						{
							num = _decoder.GetChars(new ReadOnlySpan<byte>(_byteBuffer, 0, _byteLen), userBuffer.Slice(num), flush: false);
							_charLen = 0;
						}
						else
						{
							num = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, num);
							_charLen += num;
						}
					}
					return num;
				}
				_byteLen += num2;
			}
			else
			{
				_byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
				if (_byteLen == 0)
				{
					break;
				}
			}
			_isBlocked = _byteLen < _byteBuffer.Length;
			if (!IsPreamble())
			{
				if (_detectEncoding && _byteLen >= 2)
				{
					DetectEncoding();
					readToUserBuffer = userBuffer.Length >= _maxCharsPerBuffer;
				}
				_charPos = 0;
				if (readToUserBuffer)
				{
					num += _decoder.GetChars(new ReadOnlySpan<byte>(_byteBuffer, 0, _byteLen), userBuffer.Slice(num), flush: false);
					_charLen = 0;
				}
				else
				{
					num = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, num);
					_charLen += num;
				}
			}
		}
		while (num == 0);
		_isBlocked &= num < userBuffer.Length;
		return num;
	}

	public override string? ReadLine()
	{
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (_charPos == _charLen && ReadBuffer() == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = null;
		do
		{
			int num = _charPos;
			do
			{
				char c = _charBuffer[num];
				if (c == '\r' || c == '\n')
				{
					string result;
					if (stringBuilder != null)
					{
						stringBuilder.Append(_charBuffer, _charPos, num - _charPos);
						result = stringBuilder.ToString();
					}
					else
					{
						result = new string(_charBuffer, _charPos, num - _charPos);
					}
					_charPos = num + 1;
					if (c == '\r' && (_charPos < _charLen || ReadBuffer() > 0) && _charBuffer[_charPos] == '\n')
					{
						_charPos++;
					}
					return result;
				}
				num++;
			}
			while (num < _charLen);
			num = _charLen - _charPos;
			if (stringBuilder == null)
			{
				stringBuilder = new StringBuilder(num + 80);
			}
			stringBuilder.Append(_charBuffer, _charPos, num);
		}
		while (ReadBuffer() > 0);
		return stringBuilder.ToString();
	}

	public override Task<string?> ReadLineAsync()
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadLineAsync();
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return (Task<string?>)(_asyncReadTask = ReadLineAsyncInternal());
	}

	private async Task<string> ReadLineAsyncInternal()
	{
		bool flag = _charPos == _charLen;
		bool flag2 = flag;
		if (flag2)
		{
			flag2 = await ReadBufferAsync(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false) == 0;
		}
		if (flag2)
		{
			return null;
		}
		StringBuilder sb = null;
		do
		{
			char[] charBuffer = _charBuffer;
			int charLen = _charLen;
			int charPos = _charPos;
			int num = charPos;
			do
			{
				char c = charBuffer[num];
				if (c == '\r' || c == '\n')
				{
					string s;
					if (sb != null)
					{
						sb.Append(charBuffer, charPos, num - charPos);
						s = sb.ToString();
					}
					else
					{
						s = new string(charBuffer, charPos, num - charPos);
					}
					charPos = (_charPos = num + 1);
					bool flag3 = c == '\r';
					bool flag4 = flag3;
					if (flag4)
					{
						bool flag5 = charPos < charLen;
						bool flag6 = flag5;
						if (!flag6)
						{
							flag6 = await ReadBufferAsync(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false) > 0;
						}
						flag4 = flag6;
					}
					if (flag4)
					{
						charPos = _charPos;
						if (_charBuffer[charPos] == '\n')
						{
							_charPos = charPos + 1;
						}
					}
					return s;
				}
				num++;
			}
			while (num < charLen);
			num = charLen - charPos;
			if (sb == null)
			{
				sb = new StringBuilder(num + 80);
			}
			sb.Append(charBuffer, charPos, num);
		}
		while (await ReadBufferAsync(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false) > 0);
		return sb.ToString();
	}

	public override Task<string> ReadToEndAsync()
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadToEndAsync();
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return (Task<string>)(_asyncReadTask = ReadToEndAsyncInternal());
	}

	private async Task<string> ReadToEndAsyncInternal()
	{
		StringBuilder sb = new StringBuilder(_charLen - _charPos);
		do
		{
			int charPos = _charPos;
			sb.Append(_charBuffer, charPos, _charLen - charPos);
			_charPos = _charLen;
			await ReadBufferAsync(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		}
		while (_charLen > 0);
		return sb.ToString();
	}

	public override Task<int> ReadAsync(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadAsync(buffer, index, count);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return (Task<int>)(_asyncReadTask = ReadAsyncInternal(new Memory<char>(buffer, index, count), CancellationToken.None).AsTask());
	}

	public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadAsync(buffer, cancellationToken);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		return ReadAsyncInternal(buffer, cancellationToken);
	}

	internal override async ValueTask<int> ReadAsyncInternal(Memory<char> buffer, CancellationToken cancellationToken)
	{
		bool flag = _charPos == _charLen;
		bool flag2 = flag;
		if (flag2)
		{
			flag2 = await ReadBufferAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false) == 0;
		}
		if (flag2)
		{
			return 0;
		}
		int charsRead = 0;
		bool readToUserBuffer = false;
		byte[] tmpByteBuffer = _byteBuffer;
		Stream tmpStream = _stream;
		int count = buffer.Length;
		while (count > 0)
		{
			int i = _charLen - _charPos;
			if (i == 0)
			{
				_charLen = 0;
				_charPos = 0;
				if (!_checkPreamble)
				{
					_byteLen = 0;
				}
				readToUserBuffer = count >= _maxCharsPerBuffer;
				do
				{
					if (_checkPreamble)
					{
						int bytePos = _bytePos;
						int num = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer, bytePos, tmpByteBuffer.Length - bytePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						if (num == 0)
						{
							if (_byteLen > 0)
							{
								if (readToUserBuffer)
								{
									i = _decoder.GetChars(new ReadOnlySpan<byte>(tmpByteBuffer, 0, _byteLen), buffer.Span.Slice(charsRead), flush: false);
									_charLen = 0;
								}
								else
								{
									i = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0);
									_charLen += i;
								}
							}
							_isBlocked = true;
							break;
						}
						_byteLen += num;
					}
					else
					{
						_byteLen = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						if (_byteLen == 0)
						{
							_isBlocked = true;
							break;
						}
					}
					_isBlocked = _byteLen < tmpByteBuffer.Length;
					if (!IsPreamble())
					{
						if (_detectEncoding && _byteLen >= 2)
						{
							DetectEncoding();
							readToUserBuffer = count >= _maxCharsPerBuffer;
						}
						_charPos = 0;
						if (readToUserBuffer)
						{
							i += _decoder.GetChars(new ReadOnlySpan<byte>(tmpByteBuffer, 0, _byteLen), buffer.Span.Slice(charsRead), flush: false);
							_charLen = 0;
						}
						else
						{
							i = _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, 0);
							_charLen += i;
						}
					}
				}
				while (i == 0);
				if (i == 0)
				{
					break;
				}
			}
			if (i > count)
			{
				i = count;
			}
			if (!readToUserBuffer)
			{
				new Span<char>(_charBuffer, _charPos, i).CopyTo(buffer.Span.Slice(charsRead));
				_charPos += i;
			}
			charsRead += i;
			count -= i;
			if (_isBlocked)
			{
				break;
			}
		}
		return charsRead;
	}

	public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadBlockAsync(buffer, index, count);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		return (Task<int>)(_asyncReadTask = base.ReadBlockAsync(buffer, index, count));
	}

	public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(StreamReader))
		{
			return base.ReadBlockAsync(buffer, cancellationToken);
		}
		ThrowIfDisposed();
		CheckAsyncTaskInProgress();
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		ValueTask<int> result = ReadBlockAsyncInternal(buffer, cancellationToken);
		if (result.IsCompletedSuccessfully)
		{
			return result;
		}
		return new ValueTask<int>((Task<int>)(_asyncReadTask = result.AsTask()));
	}

	private async ValueTask<int> ReadBufferAsync(CancellationToken cancellationToken)
	{
		_charLen = 0;
		_charPos = 0;
		byte[] tmpByteBuffer = _byteBuffer;
		Stream tmpStream = _stream;
		if (!_checkPreamble)
		{
			_byteLen = 0;
		}
		do
		{
			if (_checkPreamble)
			{
				int bytePos = _bytePos;
				int num = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer, bytePos, tmpByteBuffer.Length - bytePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					if (_byteLen > 0)
					{
						_charLen += _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, _charLen);
						_bytePos = 0;
						_byteLen = 0;
					}
					return _charLen;
				}
				_byteLen += num;
			}
			else
			{
				_byteLen = await tmpStream.ReadAsync(new Memory<byte>(tmpByteBuffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (_byteLen == 0)
				{
					return _charLen;
				}
			}
			_isBlocked = _byteLen < tmpByteBuffer.Length;
			if (!IsPreamble())
			{
				if (_detectEncoding && _byteLen >= 2)
				{
					DetectEncoding();
				}
				_charLen += _decoder.GetChars(tmpByteBuffer, 0, _byteLen, _charBuffer, _charLen);
			}
		}
		while (_charLen == 0);
		return _charLen;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			ThrowObjectDisposedException();
		}
		void ThrowObjectDisposedException()
		{
			throw new ObjectDisposedException(GetType().Name, SR.ObjectDisposed_ReaderClosed);
		}
	}
}
