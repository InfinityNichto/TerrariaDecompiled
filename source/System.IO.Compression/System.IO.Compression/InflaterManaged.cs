namespace System.IO.Compression;

internal sealed class InflaterManaged
{
	private static readonly byte[] s_extraLengthBits = new byte[29]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 1, 1,
		1, 1, 2, 2, 2, 2, 3, 3, 3, 3,
		4, 4, 4, 4, 5, 5, 5, 5, 16
	};

	private static readonly int[] s_lengthBase = new int[29]
	{
		3, 4, 5, 6, 7, 8, 9, 10, 11, 13,
		15, 17, 19, 23, 27, 31, 35, 43, 51, 59,
		67, 83, 99, 115, 131, 163, 195, 227, 3
	};

	private static readonly int[] s_distanceBasePosition = new int[32]
	{
		1, 2, 3, 4, 5, 7, 9, 13, 17, 25,
		33, 49, 65, 97, 129, 193, 257, 385, 513, 769,
		1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577,
		32769, 49153
	};

	private static readonly byte[] s_codeOrder = new byte[19]
	{
		16, 17, 18, 0, 8, 7, 9, 6, 10, 5,
		11, 4, 12, 3, 13, 2, 14, 1, 15
	};

	private static readonly byte[] s_staticDistanceTreeTable = new byte[32]
	{
		0, 16, 8, 24, 4, 20, 12, 28, 2, 18,
		10, 26, 6, 22, 14, 30, 1, 17, 9, 25,
		5, 21, 13, 29, 3, 19, 11, 27, 7, 23,
		15, 31
	};

	private readonly OutputWindow _output;

	private readonly InputBuffer _input;

	private HuffmanTree _literalLengthTree;

	private HuffmanTree _distanceTree;

	private InflaterState _state;

	private int _bfinal;

	private BlockType _blockType;

	private readonly byte[] _blockLengthBuffer = new byte[4];

	private int _blockLength;

	private int _length;

	private int _distanceCode;

	private int _extraBits;

	private int _loopCounter;

	private int _literalLengthCodeCount;

	private int _distanceCodeCount;

	private int _codeLengthCodeCount;

	private int _codeArraySize;

	private int _lengthCode;

	private readonly byte[] _codeList;

	private readonly byte[] _codeLengthTreeCodeLength;

	private readonly bool _deflate64;

	private HuffmanTree _codeLengthTree;

	private readonly long _uncompressedSize;

	private long _currentInflatedCount;

	internal InflaterManaged(bool deflate64, long uncompressedSize)
	{
		_output = new OutputWindow();
		_input = new InputBuffer();
		_codeList = new byte[320];
		_codeLengthTreeCodeLength = new byte[19];
		_deflate64 = deflate64;
		_uncompressedSize = uncompressedSize;
		_state = InflaterState.ReadingBFinal;
	}

	public void SetInput(byte[] inputBytes, int offset, int length)
	{
		_input.SetInput(inputBytes, offset, length);
	}

	public bool Finished()
	{
		if (_state != InflaterState.Done)
		{
			return _state == InflaterState.VerifyingFooter;
		}
		return true;
	}

	public int Inflate(Span<byte> bytes)
	{
		int num = 0;
		do
		{
			int num2 = 0;
			if (_uncompressedSize == -1)
			{
				num2 = _output.CopyTo(bytes);
			}
			else if (_uncompressedSize > _currentInflatedCount)
			{
				bytes = bytes.Slice(0, (int)Math.Min(bytes.Length, _uncompressedSize - _currentInflatedCount));
				num2 = _output.CopyTo(bytes);
				_currentInflatedCount += num2;
			}
			else
			{
				_state = InflaterState.Done;
				_output.ClearBytesUsed();
			}
			if (num2 > 0)
			{
				bytes = bytes.Slice(num2, bytes.Length - num2);
				num += num2;
			}
		}
		while (!bytes.IsEmpty && !Finished() && Decode());
		return num;
	}

	private bool Decode()
	{
		bool end_of_block = false;
		bool flag = false;
		if (Finished())
		{
			return true;
		}
		if (_state == InflaterState.ReadingBFinal)
		{
			if (!_input.EnsureBitsAvailable(1))
			{
				return false;
			}
			_bfinal = _input.GetBits(1);
			_state = InflaterState.ReadingBType;
		}
		if (_state == InflaterState.ReadingBType)
		{
			if (!_input.EnsureBitsAvailable(2))
			{
				_state = InflaterState.ReadingBType;
				return false;
			}
			_blockType = (BlockType)_input.GetBits(2);
			if (_blockType == BlockType.Dynamic)
			{
				_state = InflaterState.ReadingNumLitCodes;
			}
			else if (_blockType == BlockType.Static)
			{
				_literalLengthTree = HuffmanTree.StaticLiteralLengthTree;
				_distanceTree = HuffmanTree.StaticDistanceTree;
				_state = InflaterState.DecodeTop;
			}
			else
			{
				if (_blockType != 0)
				{
					throw new InvalidDataException(System.SR.UnknownBlockType);
				}
				_state = InflaterState.UncompressedAligning;
			}
		}
		if (_blockType == BlockType.Dynamic)
		{
			flag = ((_state >= InflaterState.DecodeTop) ? DecodeBlock(out end_of_block) : DecodeDynamicBlockHeader());
		}
		else if (_blockType == BlockType.Static)
		{
			flag = DecodeBlock(out end_of_block);
		}
		else
		{
			if (_blockType != 0)
			{
				throw new InvalidDataException(System.SR.UnknownBlockType);
			}
			flag = DecodeUncompressedBlock(out end_of_block);
		}
		if (end_of_block && _bfinal != 0)
		{
			_state = InflaterState.Done;
		}
		return flag;
	}

	private bool DecodeUncompressedBlock(out bool end_of_block)
	{
		end_of_block = false;
		while (true)
		{
			switch (_state)
			{
			case InflaterState.UncompressedAligning:
				_input.SkipToByteBoundary();
				_state = InflaterState.UncompressedByte1;
				goto case InflaterState.UncompressedByte1;
			case InflaterState.UncompressedByte1:
			case InflaterState.UncompressedByte2:
			case InflaterState.UncompressedByte3:
			case InflaterState.UncompressedByte4:
			{
				int bits = _input.GetBits(8);
				if (bits < 0)
				{
					return false;
				}
				_blockLengthBuffer[(int)(_state - 16)] = (byte)bits;
				if (_state == InflaterState.UncompressedByte4)
				{
					_blockLength = _blockLengthBuffer[0] + _blockLengthBuffer[1] * 256;
					int num2 = _blockLengthBuffer[2] + _blockLengthBuffer[3] * 256;
					if ((ushort)_blockLength != (ushort)(~num2))
					{
						throw new InvalidDataException(System.SR.InvalidBlockLength);
					}
				}
				break;
			}
			case InflaterState.DecodingUncompressed:
			{
				int num = _output.CopyFrom(_input, _blockLength);
				_blockLength -= num;
				if (_blockLength == 0)
				{
					_state = InflaterState.ReadingBFinal;
					end_of_block = true;
					return true;
				}
				if (_output.FreeBytes == 0)
				{
					return true;
				}
				return false;
			}
			default:
				throw new InvalidDataException(System.SR.UnknownState);
			}
			_state++;
		}
	}

	private bool DecodeBlock(out bool end_of_block_code_seen)
	{
		end_of_block_code_seen = false;
		int num = _output.FreeBytes;
		while (num > 65536)
		{
			switch (_state)
			{
			case InflaterState.DecodeTop:
			{
				int nextSymbol = _literalLengthTree.GetNextSymbol(_input);
				if (nextSymbol < 0)
				{
					return false;
				}
				if (nextSymbol < 256)
				{
					_output.Write((byte)nextSymbol);
					num--;
					break;
				}
				if (nextSymbol == 256)
				{
					end_of_block_code_seen = true;
					_state = InflaterState.ReadingBFinal;
					return true;
				}
				nextSymbol -= 257;
				if (nextSymbol < 8)
				{
					nextSymbol += 3;
					_extraBits = 0;
				}
				else if (!_deflate64 && nextSymbol == 28)
				{
					nextSymbol = 258;
					_extraBits = 0;
				}
				else
				{
					if (nextSymbol < 0 || nextSymbol >= s_extraLengthBits.Length)
					{
						throw new InvalidDataException(System.SR.GenericInvalidData);
					}
					_extraBits = s_extraLengthBits[nextSymbol];
				}
				_length = nextSymbol;
				goto case InflaterState.HaveInitialLength;
			}
			case InflaterState.HaveInitialLength:
				if (_extraBits > 0)
				{
					_state = InflaterState.HaveInitialLength;
					int bits2 = _input.GetBits(_extraBits);
					if (bits2 < 0)
					{
						return false;
					}
					if (_length < 0 || _length >= s_lengthBase.Length)
					{
						throw new InvalidDataException(System.SR.GenericInvalidData);
					}
					_length = s_lengthBase[_length] + bits2;
				}
				_state = InflaterState.HaveFullLength;
				goto case InflaterState.HaveFullLength;
			case InflaterState.HaveFullLength:
				if (_blockType == BlockType.Dynamic)
				{
					_distanceCode = _distanceTree.GetNextSymbol(_input);
				}
				else
				{
					_distanceCode = _input.GetBits(5);
					if (_distanceCode >= 0)
					{
						_distanceCode = s_staticDistanceTreeTable[_distanceCode];
					}
				}
				if (_distanceCode < 0)
				{
					return false;
				}
				_state = InflaterState.HaveDistCode;
				goto case InflaterState.HaveDistCode;
			case InflaterState.HaveDistCode:
			{
				int distance;
				if (_distanceCode > 3)
				{
					_extraBits = _distanceCode - 2 >> 1;
					int bits = _input.GetBits(_extraBits);
					if (bits < 0)
					{
						return false;
					}
					distance = s_distanceBasePosition[_distanceCode] + bits;
				}
				else
				{
					distance = _distanceCode + 1;
				}
				_output.WriteLengthDistance(_length, distance);
				num -= _length;
				_state = InflaterState.DecodeTop;
				break;
			}
			default:
				throw new InvalidDataException(System.SR.UnknownState);
			}
		}
		return true;
	}

	private bool DecodeDynamicBlockHeader()
	{
		switch (_state)
		{
		case InflaterState.ReadingNumLitCodes:
			_literalLengthCodeCount = _input.GetBits(5);
			if (_literalLengthCodeCount < 0)
			{
				return false;
			}
			_literalLengthCodeCount += 257;
			_state = InflaterState.ReadingNumDistCodes;
			goto case InflaterState.ReadingNumDistCodes;
		case InflaterState.ReadingNumDistCodes:
			_distanceCodeCount = _input.GetBits(5);
			if (_distanceCodeCount < 0)
			{
				return false;
			}
			_distanceCodeCount++;
			_state = InflaterState.ReadingNumCodeLengthCodes;
			goto case InflaterState.ReadingNumCodeLengthCodes;
		case InflaterState.ReadingNumCodeLengthCodes:
			_codeLengthCodeCount = _input.GetBits(4);
			if (_codeLengthCodeCount < 0)
			{
				return false;
			}
			_codeLengthCodeCount += 4;
			_loopCounter = 0;
			_state = InflaterState.ReadingCodeLengthCodes;
			goto case InflaterState.ReadingCodeLengthCodes;
		case InflaterState.ReadingCodeLengthCodes:
		{
			while (_loopCounter < _codeLengthCodeCount)
			{
				int bits = _input.GetBits(3);
				if (bits < 0)
				{
					return false;
				}
				_codeLengthTreeCodeLength[s_codeOrder[_loopCounter]] = (byte)bits;
				_loopCounter++;
			}
			for (int l = _codeLengthCodeCount; l < s_codeOrder.Length; l++)
			{
				_codeLengthTreeCodeLength[s_codeOrder[l]] = 0;
			}
			_codeLengthTree = new HuffmanTree(_codeLengthTreeCodeLength);
			_codeArraySize = _literalLengthCodeCount + _distanceCodeCount;
			_loopCounter = 0;
			_state = InflaterState.ReadingTreeCodesBefore;
			goto case InflaterState.ReadingTreeCodesBefore;
		}
		case InflaterState.ReadingTreeCodesBefore:
		case InflaterState.ReadingTreeCodesAfter:
		{
			while (_loopCounter < _codeArraySize)
			{
				if (_state == InflaterState.ReadingTreeCodesBefore && (_lengthCode = _codeLengthTree.GetNextSymbol(_input)) < 0)
				{
					return false;
				}
				if (_lengthCode <= 15)
				{
					_codeList[_loopCounter++] = (byte)_lengthCode;
				}
				else if (_lengthCode == 16)
				{
					if (!_input.EnsureBitsAvailable(2))
					{
						_state = InflaterState.ReadingTreeCodesAfter;
						return false;
					}
					if (_loopCounter == 0)
					{
						throw new InvalidDataException();
					}
					byte b = _codeList[_loopCounter - 1];
					int num = _input.GetBits(2) + 3;
					if (_loopCounter + num > _codeArraySize)
					{
						throw new InvalidDataException();
					}
					for (int i = 0; i < num; i++)
					{
						_codeList[_loopCounter++] = b;
					}
				}
				else if (_lengthCode == 17)
				{
					if (!_input.EnsureBitsAvailable(3))
					{
						_state = InflaterState.ReadingTreeCodesAfter;
						return false;
					}
					int num = _input.GetBits(3) + 3;
					if (_loopCounter + num > _codeArraySize)
					{
						throw new InvalidDataException();
					}
					for (int j = 0; j < num; j++)
					{
						_codeList[_loopCounter++] = 0;
					}
				}
				else
				{
					if (!_input.EnsureBitsAvailable(7))
					{
						_state = InflaterState.ReadingTreeCodesAfter;
						return false;
					}
					int num = _input.GetBits(7) + 11;
					if (_loopCounter + num > _codeArraySize)
					{
						throw new InvalidDataException();
					}
					for (int k = 0; k < num; k++)
					{
						_codeList[_loopCounter++] = 0;
					}
				}
				_state = InflaterState.ReadingTreeCodesBefore;
			}
			byte[] array = new byte[288];
			byte[] array2 = new byte[32];
			Array.Copy(_codeList, array, _literalLengthCodeCount);
			Array.Copy(_codeList, _literalLengthCodeCount, array2, 0, _distanceCodeCount);
			if (array[256] == 0)
			{
				throw new InvalidDataException();
			}
			_literalLengthTree = new HuffmanTree(array);
			_distanceTree = new HuffmanTree(array2);
			_state = InflaterState.DecodeTop;
			return true;
		}
		default:
			throw new InvalidDataException(System.SR.UnknownState);
		}
	}

	public void Dispose()
	{
	}
}
