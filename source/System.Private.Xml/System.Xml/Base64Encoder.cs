using System.Threading.Tasks;

namespace System.Xml;

internal abstract class Base64Encoder
{
	private byte[] _leftOverBytes;

	private int _leftOverBytesCount;

	private readonly char[] _charsLine;

	internal Base64Encoder()
	{
		_charsLine = new char[1024];
	}

	internal abstract void WriteChars(char[] chars, int index, int count);

	internal void Encode(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (count > buffer.Length - index)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (_leftOverBytesCount > 0)
		{
			int leftOverBytesCount = _leftOverBytesCount;
			while (leftOverBytesCount < 3 && count > 0)
			{
				_leftOverBytes[leftOverBytesCount++] = buffer[index++];
				count--;
			}
			if (count == 0 && leftOverBytesCount < 3)
			{
				_leftOverBytesCount = leftOverBytesCount;
				return;
			}
			int count2 = Convert.ToBase64CharArray(_leftOverBytes, 0, 3, _charsLine, 0);
			WriteChars(_charsLine, 0, count2);
		}
		_leftOverBytesCount = count % 3;
		if (_leftOverBytesCount > 0)
		{
			count -= _leftOverBytesCount;
			if (_leftOverBytes == null)
			{
				_leftOverBytes = new byte[3];
			}
			for (int i = 0; i < _leftOverBytesCount; i++)
			{
				_leftOverBytes[i] = buffer[index + count + i];
			}
		}
		int num = index + count;
		int num2 = 768;
		while (index < num)
		{
			if (index + num2 > num)
			{
				num2 = num - index;
			}
			int count3 = Convert.ToBase64CharArray(buffer, index, num2, _charsLine, 0);
			WriteChars(_charsLine, 0, count3);
			index += num2;
		}
	}

	internal void Flush()
	{
		if (_leftOverBytesCount > 0)
		{
			int count = Convert.ToBase64CharArray(_leftOverBytes, 0, _leftOverBytesCount, _charsLine, 0);
			WriteChars(_charsLine, 0, count);
			_leftOverBytesCount = 0;
		}
	}

	internal abstract Task WriteCharsAsync(char[] chars, int index, int count);

	internal async Task EncodeAsync(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (count > buffer.Length - index)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (_leftOverBytesCount > 0)
		{
			int leftOverBytesCount = _leftOverBytesCount;
			while (leftOverBytesCount < 3 && count > 0)
			{
				_leftOverBytes[leftOverBytesCount++] = buffer[index++];
				count--;
			}
			if (count == 0 && leftOverBytesCount < 3)
			{
				_leftOverBytesCount = leftOverBytesCount;
				return;
			}
			int count2 = Convert.ToBase64CharArray(_leftOverBytes, 0, 3, _charsLine, 0);
			await WriteCharsAsync(_charsLine, 0, count2).ConfigureAwait(continueOnCapturedContext: false);
		}
		_leftOverBytesCount = count % 3;
		if (_leftOverBytesCount > 0)
		{
			count -= _leftOverBytesCount;
			if (_leftOverBytes == null)
			{
				_leftOverBytes = new byte[3];
			}
			for (int i = 0; i < _leftOverBytesCount; i++)
			{
				_leftOverBytes[i] = buffer[index + count + i];
			}
		}
		int endIndex = index + count;
		int chunkSize = 768;
		while (index < endIndex)
		{
			if (index + chunkSize > endIndex)
			{
				chunkSize = endIndex - index;
			}
			int count3 = Convert.ToBase64CharArray(buffer, index, chunkSize, _charsLine, 0);
			await WriteCharsAsync(_charsLine, 0, count3).ConfigureAwait(continueOnCapturedContext: false);
			index += chunkSize;
		}
	}

	internal async Task FlushAsync()
	{
		if (_leftOverBytesCount > 0)
		{
			int count = Convert.ToBase64CharArray(_leftOverBytes, 0, _leftOverBytesCount, _charsLine, 0);
			await WriteCharsAsync(_charsLine, 0, count).ConfigureAwait(continueOnCapturedContext: false);
			_leftOverBytesCount = 0;
		}
	}
}
