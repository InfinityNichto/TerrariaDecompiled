using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Net.Mail;

internal sealed class SmtpReplyReaderFactory
{
	private enum ReadState
	{
		Status0,
		Status1,
		Status2,
		ContinueFlag,
		ContinueCR,
		ContinueLF,
		LastCR,
		LastLF,
		Done
	}

	private sealed class ReadLinesAsyncResult : System.Net.LazyAsyncResult
	{
		private StringBuilder _builder;

		private List<LineInfo> _lines;

		private readonly SmtpReplyReaderFactory _parent;

		private static readonly AsyncCallback s_readCallback = ReadCallback;

		private int _read;

		private int _statusRead;

		private readonly bool _oneLine;

		internal ReadLinesAsyncResult(SmtpReplyReaderFactory parent, AsyncCallback callback, object state)
			: base(null, state, callback)
		{
			_parent = parent;
		}

		internal ReadLinesAsyncResult(SmtpReplyReaderFactory parent, AsyncCallback callback, object state, bool oneLine)
			: base(null, state, callback)
		{
			_oneLine = oneLine;
			_parent = parent;
		}

		internal void Read(SmtpReplyReader caller)
		{
			if (_parent._currentReader != caller || _parent._readState == ReadState.Done)
			{
				InvokeCallback();
				return;
			}
			if (_parent._byteBuffer == null)
			{
				_parent._byteBuffer = new byte[256];
			}
			_builder = new StringBuilder();
			_lines = new List<LineInfo>();
			Read();
		}

		internal static LineInfo[] End(IAsyncResult result)
		{
			ReadLinesAsyncResult readLinesAsyncResult = (ReadLinesAsyncResult)result;
			readLinesAsyncResult.InternalWaitForCompletion();
			return readLinesAsyncResult._lines.ToArray();
		}

		private void Read()
		{
			do
			{
				IAsyncResult asyncResult = _parent._bufferedStream.BeginRead(_parent._byteBuffer, 0, _parent._byteBuffer.Length, s_readCallback, this);
				if (!asyncResult.CompletedSynchronously)
				{
					break;
				}
				_read = _parent._bufferedStream.EndRead(asyncResult);
			}
			while (ProcessRead());
		}

		private static void ReadCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			Exception ex = null;
			ReadLinesAsyncResult readLinesAsyncResult = (ReadLinesAsyncResult)result.AsyncState;
			try
			{
				readLinesAsyncResult._read = readLinesAsyncResult._parent._bufferedStream.EndRead(result);
				if (readLinesAsyncResult.ProcessRead())
				{
					readLinesAsyncResult.Read();
				}
			}
			catch (Exception ex2)
			{
				ex = ex2;
			}
			if (ex != null)
			{
				readLinesAsyncResult.InvokeCallback(ex);
			}
		}

		private bool ProcessRead()
		{
			if (_read == 0)
			{
				throw new IOException(System.SR.Format(System.SR.net_io_readfailure, System.SR.net_io_connectionclosed));
			}
			int num = 0;
			while (num != _read)
			{
				int num2 = _parent.ProcessRead(_parent._byteBuffer, num, _read - num, readLine: true);
				if (_statusRead < 4)
				{
					int num3 = Math.Min(4 - _statusRead, num2);
					_statusRead += num3;
					num += num3;
					num2 -= num3;
					if (num2 == 0)
					{
						continue;
					}
				}
				_builder.Append(Encoding.UTF8.GetString(_parent._byteBuffer, num, num2));
				num += num2;
				if (_parent._readState == ReadState.Status0)
				{
					_lines.Add(new LineInfo(_parent._statusCode, _builder.ToString(0, _builder.Length - 2)));
					_builder = new StringBuilder();
					_statusRead = 0;
					if (_oneLine)
					{
						_parent._bufferedStream.Push(_parent._byteBuffer, num, _read - num);
						InvokeCallback();
						return false;
					}
				}
				else if (_parent._readState == ReadState.Done)
				{
					_lines.Add(new LineInfo(_parent._statusCode, _builder.ToString(0, _builder.Length - 2)));
					_parent._bufferedStream.Push(_parent._byteBuffer, num, _read - num);
					InvokeCallback();
					return false;
				}
			}
			return true;
		}
	}

	private readonly BufferedReadStream _bufferedStream;

	private byte[] _byteBuffer;

	private SmtpReplyReader _currentReader;

	private ReadState _readState;

	private SmtpStatusCode _statusCode;

	internal SmtpReplyReader CurrentReader => _currentReader;

	internal SmtpStatusCode StatusCode => _statusCode;

	internal SmtpReplyReaderFactory(Stream stream)
	{
		_bufferedStream = new BufferedReadStream(stream);
	}

	internal IAsyncResult BeginReadLines(SmtpReplyReader caller, AsyncCallback callback, object state)
	{
		ReadLinesAsyncResult readLinesAsyncResult = new ReadLinesAsyncResult(this, callback, state);
		readLinesAsyncResult.Read(caller);
		return readLinesAsyncResult;
	}

	internal IAsyncResult BeginReadLine(SmtpReplyReader caller, AsyncCallback callback, object state)
	{
		ReadLinesAsyncResult readLinesAsyncResult = new ReadLinesAsyncResult(this, callback, state, oneLine: true);
		readLinesAsyncResult.Read(caller);
		return readLinesAsyncResult;
	}

	internal void Close(SmtpReplyReader caller)
	{
		if (_currentReader != caller)
		{
			return;
		}
		if (_readState != ReadState.Done)
		{
			if (_byteBuffer == null)
			{
				_byteBuffer = new byte[256];
			}
			while (Read(caller, _byteBuffer, 0, _byteBuffer.Length) != 0)
			{
			}
		}
		_currentReader = null;
	}

	internal LineInfo[] EndReadLines(IAsyncResult result)
	{
		return ReadLinesAsyncResult.End(result);
	}

	internal LineInfo EndReadLine(IAsyncResult result)
	{
		LineInfo[] array = ReadLinesAsyncResult.End(result);
		if (array != null && array.Length != 0)
		{
			return array[0];
		}
		return default(LineInfo);
	}

	internal SmtpReplyReader GetNextReplyReader()
	{
		if (_currentReader != null)
		{
			_currentReader.Close();
		}
		_readState = ReadState.Status0;
		_currentReader = new SmtpReplyReader(this);
		return _currentReader;
	}

	private unsafe int ProcessRead(byte[] buffer, int offset, int read, bool readLine)
	{
		if (read == 0)
		{
			throw new IOException(System.SR.Format(System.SR.net_io_readfailure, System.SR.net_io_connectionclosed));
		}
		fixed (byte* ptr = buffer)
		{
			byte* ptr2 = ptr + offset;
			byte* ptr3 = ptr2;
			byte* ptr4 = ptr3 + read;
			switch (_readState)
			{
			case ReadState.Status0:
				if (ptr3 < ptr4)
				{
					byte b = *(ptr3++);
					if (b < 48 && b > 57)
					{
						throw new FormatException(System.SR.SmtpInvalidResponse);
					}
					_statusCode = (SmtpStatusCode)(100 * (b - 48));
					goto case ReadState.Status1;
				}
				_readState = ReadState.Status0;
				break;
			case ReadState.Status1:
				if (ptr3 < ptr4)
				{
					byte b3 = *(ptr3++);
					if (b3 < 48 && b3 > 57)
					{
						throw new FormatException(System.SR.SmtpInvalidResponse);
					}
					_statusCode += 10 * (b3 - 48);
					goto case ReadState.Status2;
				}
				_readState = ReadState.Status1;
				break;
			case ReadState.Status2:
				if (ptr3 < ptr4)
				{
					byte b2 = *(ptr3++);
					if (b2 < 48 && b2 > 57)
					{
						throw new FormatException(System.SR.SmtpInvalidResponse);
					}
					_statusCode += b2 - 48;
					goto case ReadState.ContinueFlag;
				}
				_readState = ReadState.Status2;
				break;
			case ReadState.ContinueFlag:
				if (ptr3 < ptr4)
				{
					byte b4 = *(ptr3++);
					if (b4 != 32)
					{
						if (b4 != 45)
						{
							throw new FormatException(System.SR.SmtpInvalidResponse);
						}
						goto case ReadState.ContinueCR;
					}
					goto case ReadState.LastCR;
				}
				_readState = ReadState.ContinueFlag;
				break;
			case ReadState.ContinueCR:
				while (ptr3 < ptr4)
				{
					if (*(ptr3++) != 13)
					{
						continue;
					}
					goto case ReadState.ContinueLF;
				}
				_readState = ReadState.ContinueCR;
				break;
			case ReadState.ContinueLF:
				if (ptr3 < ptr4)
				{
					if (*(ptr3++) != 10)
					{
						throw new FormatException(System.SR.SmtpInvalidResponse);
					}
					if (readLine)
					{
						_readState = ReadState.Status0;
						return (int)(ptr3 - ptr2);
					}
					goto case ReadState.Status0;
				}
				_readState = ReadState.ContinueLF;
				break;
			case ReadState.LastCR:
				while (ptr3 < ptr4)
				{
					if (*(ptr3++) != 13)
					{
						continue;
					}
					goto case ReadState.LastLF;
				}
				_readState = ReadState.LastCR;
				break;
			case ReadState.LastLF:
				if (ptr3 < ptr4)
				{
					if (*(ptr3++) != 10)
					{
						throw new FormatException(System.SR.SmtpInvalidResponse);
					}
					goto case ReadState.Done;
				}
				_readState = ReadState.LastLF;
				break;
			case ReadState.Done:
			{
				int result = (int)(ptr3 - ptr2);
				_readState = ReadState.Done;
				return result;
			}
			}
			return (int)(ptr3 - ptr2);
		}
	}

	internal int Read(SmtpReplyReader caller, byte[] buffer, int offset, int count)
	{
		if (count == 0 || _currentReader != caller || _readState == ReadState.Done)
		{
			return 0;
		}
		int num = _bufferedStream.Read(buffer, offset, count);
		int num2 = ProcessRead(buffer, offset, num, readLine: false);
		if (num2 < num)
		{
			_bufferedStream.Push(buffer, offset + num2, num - num2);
		}
		return num2;
	}

	internal LineInfo ReadLine(SmtpReplyReader caller)
	{
		LineInfo[] array = ReadLines(caller, oneLine: true);
		if (array != null && array.Length != 0)
		{
			return array[0];
		}
		return default(LineInfo);
	}

	internal LineInfo[] ReadLines(SmtpReplyReader caller)
	{
		return ReadLines(caller, oneLine: false);
	}

	internal LineInfo[] ReadLines(SmtpReplyReader caller, bool oneLine)
	{
		if (caller != _currentReader || _readState == ReadState.Done)
		{
			return Array.Empty<LineInfo>();
		}
		if (_byteBuffer == null)
		{
			_byteBuffer = new byte[256];
		}
		StringBuilder stringBuilder = new StringBuilder();
		List<LineInfo> list = new List<LineInfo>();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		while (true)
		{
			if (num2 == num3)
			{
				num3 = _bufferedStream.Read(_byteBuffer, 0, _byteBuffer.Length);
				num2 = 0;
			}
			int num4 = ProcessRead(_byteBuffer, num2, num3 - num2, readLine: true);
			if (num < 4)
			{
				int num5 = Math.Min(4 - num, num4);
				num += num5;
				num2 += num5;
				num4 -= num5;
				if (num4 == 0)
				{
					continue;
				}
			}
			stringBuilder.Append(Encoding.UTF8.GetString(_byteBuffer, num2, num4));
			num2 += num4;
			if (_readState == ReadState.Status0)
			{
				num = 0;
				list.Add(new LineInfo(_statusCode, stringBuilder.ToString(0, stringBuilder.Length - 2)));
				if (oneLine)
				{
					_bufferedStream.Push(_byteBuffer, num2, num3 - num2);
					return list.ToArray();
				}
				stringBuilder = new StringBuilder();
			}
			else if (_readState == ReadState.Done)
			{
				break;
			}
		}
		list.Add(new LineInfo(_statusCode, stringBuilder.ToString(0, stringBuilder.Length - 2)));
		_bufferedStream.Push(_byteBuffer, num2, num3 - num2);
		return list.ToArray();
	}
}
