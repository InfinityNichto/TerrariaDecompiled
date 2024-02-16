using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics;

internal sealed class AsyncStreamReader : IDisposable
{
	private readonly Stream _stream;

	private readonly Decoder _decoder;

	private readonly byte[] _byteBuffer;

	private readonly char[] _charBuffer;

	private readonly Action<string> _userCallBack;

	private readonly CancellationTokenSource _cts;

	private Task _readToBufferTask;

	private readonly Queue<string> _messageQueue;

	private StringBuilder _sb;

	private bool _bLastCarriageReturn;

	private bool _cancelOperation;

	private int _currentLinePos;

	internal Task EOF => _readToBufferTask ?? Task.CompletedTask;

	internal AsyncStreamReader(Stream stream, Action<string> callback, Encoding encoding)
	{
		_stream = stream;
		_userCallBack = callback;
		_decoder = encoding.GetDecoder();
		_byteBuffer = new byte[1024];
		int maxCharCount = encoding.GetMaxCharCount(1024);
		_charBuffer = new char[maxCharCount];
		_cts = new CancellationTokenSource();
		_messageQueue = new Queue<string>();
	}

	internal void BeginReadLine()
	{
		_cancelOperation = false;
		if (_sb == null)
		{
			_sb = new StringBuilder(1024);
			_readToBufferTask = Task.Run((Func<Task?>)ReadBufferAsync);
		}
		else
		{
			FlushMessageQueue(rethrowInNewThread: false);
		}
	}

	internal void CancelOperation()
	{
		_cancelOperation = true;
	}

	private async Task ReadBufferAsync()
	{
		while (true)
		{
			try
			{
				int num = await _stream.ReadAsync(new Memory<byte>(_byteBuffer), _cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				if (num == 0)
				{
					break;
				}
				int chars = _decoder.GetChars(_byteBuffer, 0, num, _charBuffer, 0);
				_sb.Append(_charBuffer, 0, chars);
				MoveLinesFromStringBuilderToMessageQueue();
				goto IL_00e2;
			}
			catch (IOException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			break;
			IL_00e2:
			if (FlushMessageQueue(rethrowInNewThread: true))
			{
				return;
			}
		}
		lock (_messageQueue)
		{
			if (_sb.Length != 0)
			{
				_messageQueue.Enqueue(_sb.ToString());
				_sb.Length = 0;
			}
			_messageQueue.Enqueue(null);
		}
		FlushMessageQueue(rethrowInNewThread: true);
	}

	private void MoveLinesFromStringBuilderToMessageQueue()
	{
		int i = _currentLinePos;
		int num = 0;
		int length = _sb.Length;
		if (_bLastCarriageReturn && length > 0 && _sb[0] == '\n')
		{
			i = 1;
			num = 1;
			_bLastCarriageReturn = false;
		}
		for (; i < length; i++)
		{
			char c = _sb[i];
			if (c == '\r' || c == '\n')
			{
				string item = _sb.ToString(num, i - num);
				num = i + 1;
				if (c == '\r' && num < length && _sb[num] == '\n')
				{
					num++;
					i++;
				}
				lock (_messageQueue)
				{
					_messageQueue.Enqueue(item);
				}
			}
		}
		if (length > 0 && _sb[length - 1] == '\r')
		{
			_bLastCarriageReturn = true;
		}
		if (num < length)
		{
			if (num == 0)
			{
				_currentLinePos = i;
				return;
			}
			_sb.Remove(0, num);
			_currentLinePos = 0;
		}
		else
		{
			_sb.Length = 0;
			_currentLinePos = 0;
		}
	}

	private bool FlushMessageQueue(bool rethrowInNewThread)
	{
		try
		{
			while (true)
			{
				string obj;
				lock (_messageQueue)
				{
					if (_messageQueue.Count == 0)
					{
						break;
					}
					obj = _messageQueue.Dequeue();
					goto IL_0038;
				}
				IL_0038:
				if (!_cancelOperation)
				{
					_userCallBack(obj);
				}
			}
			return false;
		}
		catch (Exception source)
		{
			if (rethrowInNewThread)
			{
				ThreadPool.QueueUserWorkItem(delegate(object edi)
				{
					((ExceptionDispatchInfo)edi).Throw();
				}, ExceptionDispatchInfo.Capture(source));
				return true;
			}
			throw;
		}
	}

	public void Dispose()
	{
		_cts.Cancel();
	}
}
