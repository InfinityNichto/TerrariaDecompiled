using System.Collections;

namespace System.Security.Cryptography;

public sealed class AsnEncodedDataEnumerator : IEnumerator
{
	private readonly AsnEncodedDataCollection _asnEncodedDatas;

	private int _current;

	public AsnEncodedData Current => _asnEncodedDatas[_current];

	object IEnumerator.Current => _asnEncodedDatas[_current];

	internal AsnEncodedDataEnumerator(AsnEncodedDataCollection asnEncodedDatas)
	{
		_asnEncodedDatas = asnEncodedDatas;
		_current = -1;
	}

	public bool MoveNext()
	{
		if (_current >= _asnEncodedDatas.Count - 1)
		{
			return false;
		}
		_current++;
		return true;
	}

	public void Reset()
	{
		_current = -1;
	}
}
