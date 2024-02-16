using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.NativeCrypto;

internal static class CapiHelper
{
	internal enum CryptGetKeyParamQueryType
	{
		KP_IV = 1,
		KP_MODE = 4,
		KP_MODE_BITS = 5,
		KP_EFFECTIVE_KEYLEN = 19,
		KP_KEYLEN = 9,
		KP_ALGID = 7
	}

	internal enum CspAlgorithmType
	{
		Rsa,
		Dss
	}

	private static ReadOnlySpan<byte> RgbPubKey => new byte[84]
	{
		6, 2, 0, 0, 0, 164, 0, 0, 82, 83,
		65, 49, 0, 2, 0, 0, 1, 0, 0, 0,
		171, 239, 250, 198, 125, 232, 222, 251, 104, 56,
		9, 146, 217, 66, 126, 107, 137, 158, 33, 215,
		82, 28, 153, 60, 23, 72, 78, 58, 68, 2,
		242, 250, 116, 87, 218, 228, 211, 192, 53, 103,
		250, 110, 223, 120, 76, 117, 53, 28, 160, 116,
		73, 227, 32, 19, 113, 53, 101, 223, 18, 32,
		245, 245, 245, 193
	};

	internal static byte[] ToKeyBlob(this DSAParameters dsaParameters)
	{
		if (dsaParameters.P == null || dsaParameters.P.Length == 0 || dsaParameters.Q == null || dsaParameters.Q.Length != 20)
		{
			throw GetBadDataException();
		}
		if (dsaParameters.G == null || dsaParameters.G.Length != dsaParameters.P.Length)
		{
			throw GetBadDataException();
		}
		if (dsaParameters.J != null && dsaParameters.J.Length >= dsaParameters.P.Length)
		{
			throw GetBadDataException();
		}
		if (dsaParameters.Y != null && dsaParameters.Y.Length != dsaParameters.P.Length)
		{
			throw GetBadDataException();
		}
		if (dsaParameters.Seed != null && dsaParameters.Seed.Length != 20)
		{
			throw GetBadDataException();
		}
		bool flag = dsaParameters.X != null && dsaParameters.X.Length != 0;
		if (flag && dsaParameters.X.Length != 20)
		{
			throw GetBadDataException();
		}
		uint value = (uint)(dsaParameters.P.Length * 8);
		uint num = ((dsaParameters.J != null) ? ((uint)(dsaParameters.J.Length * 8)) : 0u);
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		WriteKeyBlobHeader(dsaParameters, binaryWriter, flag, out var isV);
		if (isV)
		{
			binaryWriter.Write(flag ? 877876036 : 861098820);
			binaryWriter.Write(value);
			binaryWriter.Write((uint)(dsaParameters.Q.Length * 8));
			binaryWriter.Write(num);
			if (flag)
			{
				binaryWriter.Write((uint)(dsaParameters.X.Length * 8));
			}
			WriteDSSSeed(dsaParameters, binaryWriter);
			binaryWriter.WriteReversed(dsaParameters.P);
			binaryWriter.WriteReversed(dsaParameters.Q);
			binaryWriter.WriteReversed(dsaParameters.G);
			if (num != 0)
			{
				binaryWriter.WriteReversed(dsaParameters.J);
			}
			binaryWriter.WriteReversed(dsaParameters.Y);
			if (flag)
			{
				binaryWriter.WriteReversed(dsaParameters.X);
			}
		}
		else
		{
			binaryWriter.Write(flag ? 844321604 : 827544388);
			binaryWriter.Write(value);
			binaryWriter.WriteReversed(dsaParameters.P);
			binaryWriter.WriteReversed(dsaParameters.Q);
			binaryWriter.WriteReversed(dsaParameters.G);
			if (flag)
			{
				binaryWriter.WriteReversed(dsaParameters.X);
			}
			else
			{
				binaryWriter.WriteReversed(dsaParameters.Y);
			}
			WriteDSSSeed(dsaParameters, binaryWriter);
		}
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	internal static DSAParameters ToDSAParameters(this byte[] cspBlob, bool includePrivateParameters, byte[] cspPublicBlob)
	{
		try
		{
			using MemoryStream input = new MemoryStream(cspBlob);
			using BinaryReader binaryReader = new BinaryReader(input);
			ReadKeyBlobHeader(binaryReader, out var bVersion);
			DSAParameters dSAParameters = default(DSAParameters);
			if (bVersion > 2)
			{
				binaryReader.ReadInt32();
				int count = (binaryReader.ReadInt32() + 7) / 8;
				int count2 = (binaryReader.ReadInt32() + 7) / 8;
				int num = (binaryReader.ReadInt32() + 7) / 8;
				int count3 = 0;
				if (includePrivateParameters)
				{
					count3 = (binaryReader.ReadInt32() + 7) / 8;
				}
				ReadDSSSeed(dSAParameters, binaryReader, isV3: true);
				dSAParameters.P = binaryReader.ReadReversed(count);
				dSAParameters.Q = binaryReader.ReadReversed(count2);
				dSAParameters.G = binaryReader.ReadReversed(count);
				if (num > 0)
				{
					dSAParameters.J = binaryReader.ReadReversed(num);
				}
				dSAParameters.Y = binaryReader.ReadReversed(count);
				if (includePrivateParameters)
				{
					dSAParameters.X = binaryReader.ReadReversed(count3);
				}
			}
			else
			{
				binaryReader.ReadInt32();
				int count4 = (binaryReader.ReadInt32() + 7) / 8;
				dSAParameters.P = binaryReader.ReadReversed(count4);
				dSAParameters.Q = binaryReader.ReadReversed(20);
				dSAParameters.G = binaryReader.ReadReversed(count4);
				long position = 0L;
				if (includePrivateParameters)
				{
					position = binaryReader.BaseStream.Position;
					dSAParameters.X = binaryReader.ReadReversed(20);
				}
				else
				{
					dSAParameters.Y = binaryReader.ReadReversed(count4);
				}
				ReadDSSSeed(dSAParameters, binaryReader, isV3: false);
				if (includePrivateParameters)
				{
					if (cspPublicBlob == null)
					{
						throw new CryptographicUnexpectedOperationException();
					}
					using MemoryStream input2 = new MemoryStream(cspPublicBlob);
					using BinaryReader binaryReader2 = new BinaryReader(input2);
					binaryReader2.BaseStream.Position = position;
					dSAParameters.Y = binaryReader2.ReadReversed(count4);
				}
			}
			return dSAParameters;
		}
		catch (EndOfStreamException)
		{
			throw GetEFailException();
		}
	}

	private static void ReadKeyBlobHeader(BinaryReader br, out byte bVersion)
	{
		br.ReadByte();
		bVersion = br.ReadByte();
		br.BaseStream.Position += 2L;
		int num = br.ReadInt32();
		if (num != 8704)
		{
			throw new PlatformNotSupportedException();
		}
	}

	private static void WriteKeyBlobHeader(DSAParameters dsaParameters, BinaryWriter bw, bool isPrivate, out bool isV3)
	{
		isV3 = false;
		byte value = 2;
		if ((dsaParameters.Y != null && isPrivate) || (dsaParameters.Y != null && dsaParameters.J != null))
		{
			isV3 = true;
			value = 3;
		}
		bw.Write((byte)(isPrivate ? 7u : 6u));
		bw.Write(value);
		bw.Write((ushort)0);
		bw.Write(8704);
	}

	private static void ReadDSSSeed(DSAParameters dsaParameters, BinaryReader br, bool isV3)
	{
		bool flag = false;
		int num = br.ReadInt32();
		if ((!isV3) ? (num > 0) : (num != -1))
		{
			dsaParameters.Counter = num;
			dsaParameters.Seed = br.ReadReversed(20);
		}
		else
		{
			dsaParameters.Counter = 0;
			dsaParameters.Seed = null;
			br.BaseStream.Position += 20L;
		}
	}

	private static void WriteDSSSeed(DSAParameters dsaParameters, BinaryWriter bw)
	{
		if (dsaParameters.Seed == null || dsaParameters.Seed.Length == 0)
		{
			bw.Write(uint.MaxValue);
			for (int i = 0; i < 20; i += 4)
			{
				bw.Write(uint.MaxValue);
			}
		}
		else
		{
			bw.Write(dsaParameters.Counter);
			bw.WriteReversed(dsaParameters.Seed);
		}
	}

	internal static byte[] ToKeyBlob(this RSAParameters rsaParameters)
	{
		if (rsaParameters.Modulus == null)
		{
			throw GetBadDataException();
		}
		if (rsaParameters.Exponent == null || rsaParameters.Exponent.Length > 4)
		{
			throw GetBadDataException();
		}
		int num = rsaParameters.Modulus.Length;
		int num2 = (num + 1) / 2;
		if (rsaParameters.P != null)
		{
			if (rsaParameters.P.Length != num2)
			{
				throw GetBadDataException();
			}
			if (rsaParameters.Q == null || rsaParameters.Q.Length != num2)
			{
				throw GetBadDataException();
			}
			if (rsaParameters.DP == null || rsaParameters.DP.Length != num2)
			{
				throw GetBadDataException();
			}
			if (rsaParameters.DQ == null || rsaParameters.DQ.Length != num2)
			{
				throw GetBadDataException();
			}
			if (rsaParameters.InverseQ == null || rsaParameters.InverseQ.Length != num2)
			{
				throw GetBadDataException();
			}
			if (rsaParameters.D == null || rsaParameters.D.Length != num)
			{
				throw GetBadDataException();
			}
		}
		bool flag = rsaParameters.P != null && rsaParameters.P.Length != 0;
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)(flag ? 7u : 6u));
		binaryWriter.Write((byte)2);
		binaryWriter.Write((ushort)0);
		binaryWriter.Write(41984u);
		binaryWriter.Write(flag ? 843141970 : 826364754);
		binaryWriter.Write((uint)(num * 8));
		uint num3 = 0u;
		for (int i = 0; i < rsaParameters.Exponent.Length; i++)
		{
			num3 <<= 8;
			num3 |= rsaParameters.Exponent[i];
		}
		binaryWriter.Write(num3);
		binaryWriter.WriteReversed(rsaParameters.Modulus);
		if (flag)
		{
			binaryWriter.WriteReversed(rsaParameters.P);
			binaryWriter.WriteReversed(rsaParameters.Q);
			binaryWriter.WriteReversed(rsaParameters.DP);
			binaryWriter.WriteReversed(rsaParameters.DQ);
			binaryWriter.WriteReversed(rsaParameters.InverseQ);
			binaryWriter.WriteReversed(rsaParameters.D);
		}
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	private static void WriteReversed(this BinaryWriter bw, byte[] bytes)
	{
		byte[] array = bytes.CloneByteArray();
		Array.Reverse(array);
		bw.Write(array);
	}

	internal static RSAParameters ToRSAParameters(this byte[] cspBlob, bool includePrivateParameters)
	{
		try
		{
			BinaryReader binaryReader = new BinaryReader(new MemoryStream(cspBlob));
			binaryReader.ReadByte();
			binaryReader.ReadByte();
			binaryReader.ReadUInt16();
			int num = binaryReader.ReadInt32();
			if (num != 41984 && num != 9216)
			{
				throw new PlatformNotSupportedException();
			}
			binaryReader.ReadInt32();
			int num2 = binaryReader.ReadInt32();
			int num3 = num2 / 8;
			int count = (num3 + 1) / 2;
			uint exponent = binaryReader.ReadUInt32();
			RSAParameters result = default(RSAParameters);
			result.Exponent = ExponentAsBytes(exponent);
			result.Modulus = binaryReader.ReadReversed(num3);
			if (includePrivateParameters)
			{
				result.P = binaryReader.ReadReversed(count);
				result.Q = binaryReader.ReadReversed(count);
				result.DP = binaryReader.ReadReversed(count);
				result.DQ = binaryReader.ReadReversed(count);
				result.InverseQ = binaryReader.ReadReversed(count);
				result.D = binaryReader.ReadReversed(num3);
			}
			return result;
		}
		catch (EndOfStreamException)
		{
			throw GetEFailException();
		}
	}

	internal static byte GetKeyBlobHeaderVersion(byte[] cspBlob)
	{
		if (cspBlob.Length < 8)
		{
			throw new EndOfStreamException();
		}
		return cspBlob[1];
	}

	private static byte[] ExponentAsBytes(uint exponent)
	{
		if (exponent > 255)
		{
			if (exponent > 65535)
			{
				if (exponent > 16777215)
				{
					return new byte[4]
					{
						(byte)(exponent >> 24),
						(byte)(exponent >> 16),
						(byte)(exponent >> 8),
						(byte)exponent
					};
				}
				return new byte[3]
				{
					(byte)(exponent >> 16),
					(byte)(exponent >> 8),
					(byte)exponent
				};
			}
			return new byte[2]
			{
				(byte)(exponent >> 8),
				(byte)exponent
			};
		}
		return new byte[1] { (byte)exponent };
	}

	private static byte[] ReadReversed(this BinaryReader br, int count)
	{
		byte[] array = br.ReadBytes(count);
		Array.Reverse(array);
		return array;
	}

	public static string UpgradeDSS(int dwProvType, string wszProvider)
	{
		string result = null;
		if (string.Equals(wszProvider, "Microsoft Base DSS and Diffie-Hellman Cryptographic Provider", StringComparison.Ordinal))
		{
			if (AcquireCryptContext(out var safeProvHandle, null, "Microsoft Enhanced DSS and Diffie-Hellman Cryptographic Provider", dwProvType, 4026531840u) == 0)
			{
				result = "Microsoft Enhanced DSS and Diffie-Hellman Cryptographic Provider";
			}
			safeProvHandle.Dispose();
		}
		return result;
	}

	private static void ReverseDsaSignature(byte[] signature, int cbSignature)
	{
		if (cbSignature != 40)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidDSASignatureSize);
		}
		Array.Reverse(signature, 0, 20);
		Array.Reverse(signature, 20, 20);
	}

	public static string UpgradeRSA(int dwProvType, string wszProvider)
	{
		bool flag = string.Equals(wszProvider, "Microsoft Enhanced Cryptographic Provider v1.0", StringComparison.Ordinal);
		bool flag2 = string.Equals(wszProvider, "Microsoft Base Cryptographic Provider v1.0", StringComparison.Ordinal);
		string result = null;
		if (flag2 || flag)
		{
			if (AcquireCryptContext(out var safeProvHandle, null, "Microsoft Enhanced RSA and AES Cryptographic Provider", dwProvType, 4026531840u) == 0)
			{
				result = "Microsoft Enhanced RSA and AES Cryptographic Provider";
			}
			safeProvHandle.Dispose();
		}
		return result;
	}

	internal static string GetDefaultProvider(int dwType)
	{
		int pcbProvName = 0;
		if (!global::Interop.Advapi32.CryptGetDefaultProvider(dwType, IntPtr.Zero, global::Interop.Advapi32.GetDefaultProviderFlags.CRYPT_MACHINE_DEFAULT, null, ref pcbProvName))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		StringBuilder stringBuilder = new StringBuilder(pcbProvName);
		if (!global::Interop.Advapi32.CryptGetDefaultProvider(dwType, IntPtr.Zero, global::Interop.Advapi32.GetDefaultProviderFlags.CRYPT_MACHINE_DEFAULT, stringBuilder, ref pcbProvName))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		string text = stringBuilder.ToString();
		string text2 = null;
		switch (dwType)
		{
		case 1:
			text2 = UpgradeRSA(dwType, text);
			break;
		case 13:
			text2 = UpgradeDSS(dwType, text);
			break;
		}
		if (text2 == null)
		{
			return text;
		}
		return text2;
	}

	private static void CreateCSP(CspParameters parameters, bool randomKeyContainer, out SafeProvHandle safeProvHandle)
	{
		uint num = 8u;
		if (randomKeyContainer)
		{
			num |= 0xF0000000u;
		}
		SafeProvHandle safeProvHandle2;
		int num2 = OpenCSP(parameters, num, out safeProvHandle2);
		if (num2 != 0)
		{
			safeProvHandle2.Dispose();
			throw num2.ToCryptographicException();
		}
		safeProvHandle = safeProvHandle2;
	}

	private static int AcquireCryptContext(out SafeProvHandle safeProvHandle, string keyContainer, string providerName, int providerType, uint flags)
	{
		int result = 0;
		if ((flags & 0xF0000000u) == 4026531840u && (flags & 0x20) == 32)
		{
			flags &= 0xFFFFFFDFu;
		}
		if (!global::Interop.Advapi32.CryptAcquireContext(out safeProvHandle, keyContainer, providerName, providerType, flags))
		{
			result = GetErrorCode();
		}
		return result;
	}

	internal static void AcquireCsp(CspParameters cspParameters, out SafeProvHandle safeProvHandle)
	{
		SafeProvHandle safeProvHandle2;
		int num = OpenCSP(cspParameters, 4026531840u, out safeProvHandle2);
		if (num != 0)
		{
			safeProvHandle2.Dispose();
			throw num.ToCryptographicException();
		}
		safeProvHandle = safeProvHandle2;
	}

	public static int OpenCSP(CspParameters cspParameters, uint flags, out SafeProvHandle safeProvHandle)
	{
		string text = null;
		if (cspParameters == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.CspParameter_invalid, "cspParameters"));
		}
		int providerType = cspParameters.ProviderType;
		string providerName = ((cspParameters.ProviderName != null) ? cspParameters.ProviderName : (cspParameters.ProviderName = GetDefaultProvider(providerType)));
		int flags2 = (int)cspParameters.Flags;
		if (!IsFlagBitSet((uint)flags2, 2u) && cspParameters.KeyContainerName != null)
		{
			text = cspParameters.KeyContainerName;
		}
		flags |= MapCspProviderFlags((int)cspParameters.Flags);
		SafeProvHandle safeProvHandle2;
		int num = AcquireCryptContext(out safeProvHandle2, text, providerName, providerType, flags);
		if (num != 0)
		{
			safeProvHandle2.Dispose();
			safeProvHandle = SafeProvHandle.InvalidHandle;
			return num;
		}
		safeProvHandle2.ContainerName = text;
		safeProvHandle2.ProviderName = providerName;
		safeProvHandle2.Types = providerType;
		safeProvHandle2.Flags = flags;
		if (IsFlagBitSet(flags, 4026531840u))
		{
			safeProvHandle2.PersistKeyInCsp = false;
		}
		safeProvHandle = safeProvHandle2;
		return 0;
	}

	internal static SafeProvHandle CreateProvHandle(CspParameters parameters, bool randomKeyContainer)
	{
		uint flags = 0u;
		SafeProvHandle safeProvHandle;
		uint num = (uint)OpenCSP(parameters, flags, out safeProvHandle);
		if (num != 0)
		{
			safeProvHandle.Dispose();
			if (IsFlagBitSet((uint)parameters.Flags, 8u) || (num != 2148073497u && num != 2148073494u && num != 2147942402u))
			{
				throw ((int)num).ToCryptographicException();
			}
			CreateCSP(parameters, randomKeyContainer, out safeProvHandle);
		}
		if (parameters.ParentWindowHandle != IntPtr.Zero)
		{
			IntPtr pbData = parameters.ParentWindowHandle;
			if (!global::Interop.Advapi32.CryptSetProvParam(safeProvHandle, global::Interop.Advapi32.CryptProvParam.PP_CLIENT_HWND, ref pbData, 0))
			{
				throw GetErrorCode().ToCryptographicException();
			}
		}
		if (parameters.KeyPassword != null)
		{
			IntPtr intPtr = Marshal.SecureStringToCoTaskMemAnsi(parameters.KeyPassword);
			try
			{
				global::Interop.Advapi32.CryptProvParam dwParam = ((parameters.KeyNumber == 2) ? global::Interop.Advapi32.CryptProvParam.PP_SIGNATURE_PIN : global::Interop.Advapi32.CryptProvParam.PP_KEYEXCHANGE_PIN);
				if (!global::Interop.Advapi32.CryptSetProvParam(safeProvHandle, dwParam, intPtr, 0))
				{
					throw GetErrorCode().ToCryptographicException();
				}
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.ZeroFreeCoTaskMemAnsi(intPtr);
				}
			}
		}
		return safeProvHandle;
	}

	internal static bool IsFlagBitSet(uint dwImp, uint flag)
	{
		return (dwImp & flag) == flag;
	}

	internal static int GetProviderParameterWorker(SafeProvHandle safeProvHandle, byte[] impType, ref int cb, global::Interop.Advapi32.CryptProvParam flags)
	{
		int result = 0;
		if (!global::Interop.Advapi32.CryptGetProvParam(safeProvHandle, flags, impType, ref cb))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		if (impType != null && cb == 4)
		{
			result = BitConverter.ToInt32(impType, 0);
		}
		return result;
	}

	public static object GetProviderParameter(SafeProvHandle safeProvHandle, int keyNumber, int keyParam)
	{
		VerifyValidHandle(safeProvHandle);
		byte[] impType = new byte[4];
		int cb = 4;
		SafeKeyHandle safeKeyHandle = SafeKeyHandle.InvalidHandle;
		int num = 0;
		int num2 = 0;
		bool flag = false;
		string result = null;
		try
		{
			switch (keyParam)
			{
			case 3:
				num = GetProviderParameterWorker(safeProvHandle, impType, ref cb, global::Interop.Advapi32.CryptProvParam.PP_IMPTYPE);
				if (!IsFlagBitSet((uint)num, 1u))
				{
					if (!CryptGetUserKey(safeProvHandle, keyNumber, out safeKeyHandle))
					{
						throw GetErrorCode().ToCryptographicException();
					}
					byte[] array = null;
					int num3 = 0;
					array = new byte[4];
					cb = 4;
					if (!global::Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, global::Interop.Advapi32.CryptGetKeyParamFlags.KP_PERMISSIONS, array, ref cb, 0))
					{
						throw GetErrorCode().ToCryptographicException();
					}
					num3 = BitConverter.ToInt32(array, 0);
					flag = IsFlagBitSet((uint)num3, 4u);
				}
				else
				{
					flag = false;
				}
				break;
			case 4:
				num = GetProviderParameterWorker(safeProvHandle, impType, ref cb, global::Interop.Advapi32.CryptProvParam.PP_IMPTYPE);
				flag = IsFlagBitSet((uint)num, 8u);
				break;
			case 5:
			case 7:
				num = GetProviderParameterWorker(safeProvHandle, impType, ref cb, global::Interop.Advapi32.CryptProvParam.PP_IMPTYPE);
				flag = IsFlagBitSet((uint)num, 1u);
				break;
			case 6:
				flag = (CryptGetUserKey(safeProvHandle, keyNumber, out safeKeyHandle) ? true : false);
				break;
			case 8:
			{
				num2 = 1;
				byte[] impType2 = null;
				num = GetProviderParameterWorker(safeProvHandle, impType2, ref cb, global::Interop.Advapi32.CryptProvParam.PP_UNIQUE_CONTAINER);
				impType2 = new byte[cb];
				num = GetProviderParameterWorker(safeProvHandle, impType2, ref cb, global::Interop.Advapi32.CryptProvParam.PP_UNIQUE_CONTAINER);
				result = Encoding.ASCII.GetString(impType2, 0, cb - 1);
				break;
			}
			}
		}
		finally
		{
			safeKeyHandle.Dispose();
		}
		if (num2 != 0)
		{
			return result;
		}
		return flag;
	}

	internal static int GetUserKey(SafeProvHandle safeProvHandle, int keySpec, out SafeKeyHandle safeKeyHandle)
	{
		int num = 0;
		VerifyValidHandle(safeProvHandle);
		if (!CryptGetUserKey(safeProvHandle, keySpec, out safeKeyHandle))
		{
			num = GetErrorCode();
		}
		if (num == 0)
		{
			safeKeyHandle.KeySpec = keySpec;
		}
		return num;
	}

	internal static int GenerateKey(SafeProvHandle safeProvHandle, int algID, int flags, uint keySize, out SafeKeyHandle safeKeyHandle)
	{
		int num = 0;
		VerifyValidHandle(safeProvHandle);
		int dwFlags = MapCspKeyFlags(flags) | (int)(keySize << 16);
		if (!CryptGenKey(safeProvHandle, algID, dwFlags, out safeKeyHandle))
		{
			num = GetErrorCode();
		}
		if (num != 0)
		{
			throw GetErrorCode().ToCryptographicException();
		}
		safeKeyHandle.KeySpec = algID;
		return num;
	}

	internal static int MapCspKeyFlags(int flags)
	{
		int num = 0;
		if (!IsFlagBitSet((uint)flags, 4u))
		{
			num |= 1;
		}
		if (IsFlagBitSet((uint)flags, 16u))
		{
			num |= 0x4000;
		}
		if (IsFlagBitSet((uint)flags, 32u))
		{
			num |= 2;
		}
		return num;
	}

	internal static uint MapCspProviderFlags(int flags)
	{
		uint num = 0u;
		if (IsFlagBitSet((uint)flags, 1u))
		{
			num |= 0x20u;
		}
		if (IsFlagBitSet((uint)flags, 64u))
		{
			num |= 0x40u;
		}
		if (IsFlagBitSet((uint)flags, 128u))
		{
			num |= 0xF0000000u;
		}
		return num;
	}

	internal static void VerifyValidHandle(SafeHandleZeroOrMinusOneIsInvalid handle)
	{
		if (handle.IsInvalid)
		{
			throw new CryptographicException(System.SR.Cryptography_OpenInvalidHandle);
		}
	}

	internal static byte[] GetKeyParameter(SafeKeyHandle safeKeyHandle, int keyParam)
	{
		byte[] array = null;
		int pdwDataLen = 0;
		VerifyValidHandle(safeKeyHandle);
		switch (keyParam)
		{
		case 1:
			if (!global::Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, global::Interop.Advapi32.CryptGetKeyParamFlags.KP_KEYLEN, null, ref pdwDataLen, 0))
			{
				throw GetErrorCode().ToCryptographicException();
			}
			array = new byte[pdwDataLen];
			if (!global::Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, global::Interop.Advapi32.CryptGetKeyParamFlags.KP_KEYLEN, array, ref pdwDataLen, 0))
			{
				throw GetErrorCode().ToCryptographicException();
			}
			break;
		case 2:
			array = new byte[1] { (byte)(safeKeyHandle.PublicOnly ? 1 : 0) };
			break;
		case 9:
			if (!global::Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, global::Interop.Advapi32.CryptGetKeyParamFlags.KP_ALGID, null, ref pdwDataLen, 0))
			{
				throw GetErrorCode().ToCryptographicException();
			}
			array = new byte[pdwDataLen];
			if (!global::Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, global::Interop.Advapi32.CryptGetKeyParamFlags.KP_ALGID, array, ref pdwDataLen, 0))
			{
				throw GetErrorCode().ToCryptographicException();
			}
			break;
		}
		return array;
	}

	internal static void SetKeyParameter(SafeKeyHandle safeKeyHandle, CryptGetKeyParamQueryType keyParam, byte[] value)
	{
		VerifyValidHandle(safeKeyHandle);
		if (keyParam == CryptGetKeyParamQueryType.KP_IV && !global::Interop.Advapi32.CryptSetKeyParam(safeKeyHandle, (int)keyParam, value, 0))
		{
			throw new CryptographicException(System.SR.CryptSetKeyParam_Failed, GetErrorCode().ToString());
		}
	}

	internal static void SetKeyParameter(SafeKeyHandle safeKeyHandle, CryptGetKeyParamQueryType keyParam, int value)
	{
		VerifyValidHandle(safeKeyHandle);
		if (((uint)(keyParam - 4) <= 1u || keyParam == CryptGetKeyParamQueryType.KP_EFFECTIVE_KEYLEN) && !global::Interop.Advapi32.CryptSetKeyParam(safeKeyHandle, (int)keyParam, ref value, 0))
		{
			throw new CryptographicException(System.SR.CryptSetKeyParam_Failed, GetErrorCode().ToString());
		}
	}

	internal static CspParameters SaveCspParameters(CspAlgorithmType keyType, CspParameters userParameters, CspProviderFlags defaultFlags, out bool randomKeyContainer)
	{
		CspParameters cspParameters;
		if (userParameters == null)
		{
			cspParameters = new CspParameters((keyType == CspAlgorithmType.Dss) ? 13 : 24, null, null, defaultFlags);
		}
		else
		{
			ValidateCspFlags(userParameters.Flags);
			cspParameters = new CspParameters(userParameters);
		}
		if (cspParameters.KeyNumber == -1)
		{
			cspParameters.KeyNumber = ((keyType != CspAlgorithmType.Dss) ? 1 : 2);
		}
		else if (cspParameters.KeyNumber == 8704 || cspParameters.KeyNumber == 9216)
		{
			cspParameters.KeyNumber = 2;
		}
		else if (cspParameters.KeyNumber == 41984)
		{
			cspParameters.KeyNumber = 1;
		}
		randomKeyContainer = IsFlagBitSet((uint)cspParameters.Flags, 128u);
		if (cspParameters.KeyContainerName == null && !IsFlagBitSet((uint)cspParameters.Flags, 2u))
		{
			cspParameters.Flags |= CspProviderFlags.CreateEphemeralKey;
			randomKeyContainer = true;
		}
		return cspParameters;
	}

	private static void ValidateCspFlags(CspProviderFlags flags)
	{
		if (IsFlagBitSet((uint)flags, 8u))
		{
			CspProviderFlags cspProviderFlags = CspProviderFlags.UseNonExportableKey | CspProviderFlags.UseArchivableKey | CspProviderFlags.UseUserProtectedKey;
			if ((flags & cspProviderFlags) != 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, flags), "flags");
			}
		}
	}

	internal static SafeKeyHandle GetKeyPairHelper(CspAlgorithmType keyType, CspParameters parameters, int keySize, SafeProvHandle safeProvHandle)
	{
		SafeKeyHandle safeKeyHandle;
		int userKey = GetUserKey(safeProvHandle, parameters.KeyNumber, out safeKeyHandle);
		if (userKey != 0)
		{
			safeKeyHandle.Dispose();
			if (IsFlagBitSet((uint)parameters.Flags, 8u) || userKey != -2146893811)
			{
				throw userKey.ToCryptographicException();
			}
			GenerateKey(safeProvHandle, parameters.KeyNumber, (int)parameters.Flags, (uint)keySize, out safeKeyHandle);
		}
		byte[] keyParameter = GetKeyParameter(safeKeyHandle, 9);
		int num = BinaryPrimitives.ReadInt32LittleEndian(keyParameter);
		if ((keyType == CspAlgorithmType.Rsa && num != 41984 && num != 9216) || (keyType == CspAlgorithmType.Dss && num != 8704))
		{
			safeKeyHandle.Dispose();
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CSP_WrongKeySpec, keyType));
		}
		return safeKeyHandle;
	}

	internal static int GetErrorCode()
	{
		return Marshal.GetLastPInvokeError();
	}

	internal static bool GetPersistKeyInCsp(SafeProvHandle safeProvHandle)
	{
		VerifyValidHandle(safeProvHandle);
		return safeProvHandle.PersistKeyInCsp;
	}

	internal static void SetPersistKeyInCsp(SafeProvHandle safeProvHandle, bool fPersistKeyInCsp)
	{
		VerifyValidHandle(safeProvHandle);
		safeProvHandle.PersistKeyInCsp = fPersistKeyInCsp;
	}

	internal static void DecryptKey(SafeKeyHandle safeKeyHandle, byte[] encryptedData, int encryptedDataLength, bool fOAEP, out byte[] decryptedData)
	{
		VerifyValidHandle(safeKeyHandle);
		byte[] array = new byte[encryptedDataLength];
		Buffer.BlockCopy(encryptedData, 0, array, 0, encryptedDataLength);
		Array.Reverse(array);
		int num = (fOAEP ? 64 : 0);
		int pdwDataLen = encryptedDataLength;
		if (!global::Interop.Advapi32.CryptDecrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, Final: true, num, array, ref pdwDataLen))
		{
			int errorCode = GetErrorCode();
			if ((num & 0x40) == 64)
			{
				switch (errorCode)
				{
				case -2146893815:
					throw new CryptographicException("Cryptography_OAEP_XPPlus_Only");
				default:
					throw new CryptographicException("Cryptography_OAEPDecoding");
				case -2146893821:
					break;
				}
			}
			throw errorCode.ToCryptographicException();
		}
		decryptedData = new byte[pdwDataLen];
		Buffer.BlockCopy(array, 0, decryptedData, 0, pdwDataLen);
	}

	internal static void EncryptKey(SafeKeyHandle safeKeyHandle, byte[] pbKey, int cbKey, bool foep, [NotNull] ref byte[] pbEncryptedKey)
	{
		VerifyValidHandle(safeKeyHandle);
		int dwFlags = (foep ? 64 : 0);
		int pdwDataLen = cbKey;
		if (!global::Interop.Advapi32.CryptEncrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, Final: true, dwFlags, null, ref pdwDataLen, pdwDataLen))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		pbEncryptedKey = new byte[pdwDataLen];
		Buffer.BlockCopy(pbKey, 0, pbEncryptedKey, 0, cbKey);
		if (!global::Interop.Advapi32.CryptEncrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, Final: true, dwFlags, pbEncryptedKey, ref cbKey, pdwDataLen))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		Array.Reverse(pbEncryptedKey);
	}

	internal static int EncryptData(SafeKeyHandle hKey, ReadOnlySpan<byte> input, Span<byte> output, bool isFinal)
	{
		VerifyValidHandle(hKey);
		int pdwDataLen = input.Length;
		if (!global::Interop.Advapi32.CryptEncrypt(hKey, SafeHashHandle.InvalidHandle, isFinal, 0, null, ref pdwDataLen, pdwDataLen))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		byte[] array = new byte[pdwDataLen];
		input.CopyTo(array);
		int pdwDataLen2 = input.Length;
		if (!global::Interop.Advapi32.CryptEncrypt(hKey, SafeHashHandle.InvalidHandle, isFinal, 0, array, ref pdwDataLen2, pdwDataLen))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		int num = (isFinal ? input.Length : pdwDataLen2);
		array.AsSpan(0, num).CopyTo(output);
		return num;
	}

	internal static int DecryptData(SafeKeyHandle hKey, ReadOnlySpan<byte> input, Span<byte> output)
	{
		VerifyValidHandle(hKey);
		byte[] array = new byte[input.Length];
		input.CopyTo(array);
		int pdwDataLen = input.Length;
		if (!global::Interop.Advapi32.CryptDecrypt(hKey, SafeHashHandle.InvalidHandle, Final: false, 0, array, ref pdwDataLen))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		array.AsSpan(0, pdwDataLen).CopyTo(output);
		return pdwDataLen;
	}

	internal static void ImportKeyBlob(SafeProvHandle saveProvHandle, CspProviderFlags flags, bool addNoSaltFlag, byte[] keyBlob, out SafeKeyHandle safeKeyHandle)
	{
		bool flag = keyBlob.Length != 0 && keyBlob[0] == 6;
		int num = MapCspKeyFlags((int)flags);
		if (flag)
		{
			num &= -2;
		}
		if (addNoSaltFlag)
		{
			num |= 0x10;
		}
		if (!CryptImportKey(saveProvHandle, keyBlob, SafeKeyHandle.InvalidHandle, num, out var phKey))
		{
			int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
			phKey.Dispose();
			throw hRForLastWin32Error.ToCryptographicException();
		}
		phKey.PublicOnly = flag;
		safeKeyHandle = phKey;
	}

	internal static byte[] ExportKeyBlob(bool includePrivateParameters, SafeKeyHandle safeKeyHandle)
	{
		VerifyValidHandle(safeKeyHandle);
		int dwDataLen = 0;
		int dwBlobType = (includePrivateParameters ? 7 : 6);
		if (!global::Interop.Advapi32.CryptExportKey(safeKeyHandle, SafeKeyHandle.InvalidHandle, dwBlobType, 0, null, ref dwDataLen))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		byte[] array = new byte[dwDataLen];
		if (!global::Interop.Advapi32.CryptExportKey(safeKeyHandle, SafeKeyHandle.InvalidHandle, dwBlobType, 0, array, ref dwDataLen))
		{
			throw GetErrorCode().ToCryptographicException();
		}
		return array;
	}

	public static int NameOrOidToHashAlgId(string nameOrOid, OidGroup oidGroup)
	{
		if (nameOrOid == null)
		{
			return 32772;
		}
		string text = CryptoConfig.MapNameToOID(nameOrOid);
		if (text == null)
		{
			text = nameOrOid;
		}
		int algIdFromOid = GetAlgIdFromOid(text, oidGroup);
		if (algIdFromOid == 0 || algIdFromOid == -1)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidOID);
		}
		return algIdFromOid;
	}

	public static int ObjToHashAlgId(object hashAlg)
	{
		if (hashAlg == null)
		{
			throw new ArgumentNullException("hashAlg");
		}
		if (hashAlg is string nameOrOid)
		{
			return NameOrOidToHashAlgId(nameOrOid, OidGroup.HashAlgorithm);
		}
		if (hashAlg is HashAlgorithm)
		{
			if (hashAlg is MD5)
			{
				return 32771;
			}
			if (hashAlg is SHA1)
			{
				return 32772;
			}
			if (hashAlg is SHA256)
			{
				return 32780;
			}
			if (hashAlg is SHA384)
			{
				return 32781;
			}
			if (hashAlg is SHA512)
			{
				return 32782;
			}
		}
		else if (hashAlg is Type c)
		{
			if (typeof(MD5).IsAssignableFrom(c))
			{
				return 32771;
			}
			if (typeof(SHA1).IsAssignableFrom(c))
			{
				return 32772;
			}
			if (typeof(SHA256).IsAssignableFrom(c))
			{
				return 32780;
			}
			if (typeof(SHA384).IsAssignableFrom(c))
			{
				return 32781;
			}
			if (typeof(SHA512).IsAssignableFrom(c))
			{
				return 32782;
			}
		}
		throw new ArgumentException(System.SR.Argument_InvalidValue, "hashAlg");
	}

	internal static HashAlgorithm ObjToHashAlgorithm(object hashAlg)
	{
		return ObjToHashAlgId(hashAlg) switch
		{
			32771 => MD5.Create(), 
			32772 => SHA1.Create(), 
			32780 => SHA256.Create(), 
			32781 => SHA384.Create(), 
			32782 => SHA512.Create(), 
			_ => throw new ArgumentException(System.SR.Argument_InvalidValue, "hashAlg"), 
		};
	}

	private static int GetAlgIdFromOid(string oid, OidGroup oidGroup)
	{
		if (string.Equals(oid, "2.16.840.1.101.3.4.2.1", StringComparison.Ordinal))
		{
			return 32780;
		}
		if (string.Equals(oid, "2.16.840.1.101.3.4.2.2", StringComparison.Ordinal))
		{
			return 32781;
		}
		if (string.Equals(oid, "2.16.840.1.101.3.4.2.3", StringComparison.Ordinal))
		{
			return 32782;
		}
		return global::Interop.Crypt32.FindOidInfo(global::Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, oid, oidGroup, fallBackToAllGroups: false).AlgId;
	}

	public static byte[] SignValue(SafeProvHandle hProv, SafeKeyHandle hKey, int keyNumber, int calgKey, int calgHash, byte[] hash)
	{
		using SafeHashHandle hHash = hProv.CreateHashHandle(hash, calgHash);
		int pdwSigLen = 0;
		if (!global::Interop.Advapi32.CryptSignHash(hHash, (global::Interop.Advapi32.KeySpec)keyNumber, null, global::Interop.Advapi32.CryptSignAndVerifyHashFlags.None, null, ref pdwSigLen))
		{
			int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
			throw hRForLastWin32Error.ToCryptographicException();
		}
		byte[] array = new byte[pdwSigLen];
		if (!global::Interop.Advapi32.CryptSignHash(hHash, (global::Interop.Advapi32.KeySpec)keyNumber, null, global::Interop.Advapi32.CryptSignAndVerifyHashFlags.None, array, ref pdwSigLen))
		{
			int hRForLastWin32Error2 = Marshal.GetHRForLastWin32Error();
			throw hRForLastWin32Error2.ToCryptographicException();
		}
		switch (calgKey)
		{
		case 9216:
			Array.Reverse(array);
			break;
		case 8704:
			ReverseDsaSignature(array, pdwSigLen);
			break;
		default:
			throw new InvalidOperationException();
		}
		return array;
	}

	public static bool VerifySign(SafeProvHandle hProv, SafeKeyHandle hKey, int calgKey, int calgHash, byte[] hash, byte[] signature)
	{
		switch (calgKey)
		{
		case 9216:
			signature = signature.CloneByteArray();
			Array.Reverse(signature);
			break;
		case 8704:
			signature = signature.CloneByteArray();
			ReverseDsaSignature(signature, signature.Length);
			break;
		default:
			throw new InvalidOperationException();
		}
		using SafeHashHandle safeHashHandle = hProv.CreateHashHandle(hash, calgHash, throwOnSizeError: false);
		if (safeHashHandle == null)
		{
			return false;
		}
		return global::Interop.Advapi32.CryptVerifySignature(safeHashHandle, signature, signature.Length, hKey, null, global::Interop.Advapi32.CryptSignAndVerifyHashFlags.None);
	}

	public static void DeriveKey(SafeProvHandle hProv, int algid, int algidHash, byte[] password, int cbPassword, int dwFlags, byte[] IV_Out, int cbIV_In, [NotNull] ref byte[] pbKey)
	{
		VerifyValidHandle(hProv);
		SafeHashHandle phHash = null;
		SafeKeyHandle phKey = null;
		try
		{
			if (!CryptCreateHash(hProv, algidHash, SafeKeyHandle.InvalidHandle, global::Interop.Advapi32.CryptCreateHashFlags.None, out phHash))
			{
				int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error.ToCryptographicException();
			}
			if (!global::Interop.Advapi32.CryptHashData(phHash, password, cbPassword, 0))
			{
				int hRForLastWin32Error2 = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error2.ToCryptographicException();
			}
			if (!CryptDeriveKey(hProv, algid, phHash, dwFlags | 1, out phKey))
			{
				int hRForLastWin32Error3 = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error3.ToCryptographicException();
			}
			byte[] key_out = null;
			int cb_out = 0;
			UnloadKey(hProv, phKey, ref key_out, ref cb_out);
			int pdwDataLen = 0;
			if (!global::Interop.Advapi32.CryptGetKeyParam(phKey, global::Interop.Advapi32.CryptGetKeyParamFlags.KP_IV, null, ref pdwDataLen, 0))
			{
				int hRForLastWin32Error4 = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error4.ToCryptographicException();
			}
			byte[] array = new byte[pdwDataLen];
			if (!global::Interop.Advapi32.CryptGetKeyParam(phKey, global::Interop.Advapi32.CryptGetKeyParamFlags.KP_IV, array, ref pdwDataLen, 0))
			{
				int hRForLastWin32Error5 = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error5.ToCryptographicException();
			}
			if (pdwDataLen != cbIV_In)
			{
				throw new CryptographicException(System.SR.Cryptography_PasswordDerivedBytes_InvalidIV);
			}
			Buffer.BlockCopy(array, 0, IV_Out, 0, pdwDataLen);
			pbKey = new byte[cb_out];
			Buffer.BlockCopy(key_out, 0, pbKey, 0, cb_out);
		}
		finally
		{
			phKey?.Dispose();
			phHash?.Dispose();
		}
	}

	private static void UnloadKey(SafeProvHandle hProv, SafeKeyHandle hKey, [NotNull] ref byte[] key_out, ref int cb_out)
	{
		SafeKeyHandle phKey = null;
		try
		{
			if (!CryptImportKey(hProv, RgbPubKey, SafeKeyHandle.InvalidHandle, 0, out phKey))
			{
				int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error.ToCryptographicException();
			}
			int dwDataLen = 0;
			if (!global::Interop.Advapi32.CryptExportKey(hKey, phKey, 1, 0, null, ref dwDataLen))
			{
				int hRForLastWin32Error2 = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error2.ToCryptographicException();
			}
			byte[] array = new byte[dwDataLen];
			if (!global::Interop.Advapi32.CryptExportKey(hKey, phKey, 1, 0, array, ref dwDataLen))
			{
				int hRForLastWin32Error3 = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error3.ToCryptographicException();
			}
			int num = 8;
			int num2 = num + 4;
			int num3 = checked(dwDataLen - num - 4 - 2);
			while (num3 > 0 && array[num3 + num2] != 0)
			{
				num3--;
			}
			key_out = new byte[num3];
			Buffer.BlockCopy(array, num2, key_out, 0, num3);
			Array.Reverse(key_out);
			cb_out = num3;
		}
		finally
		{
			phKey?.Dispose();
		}
	}

	private static SafeHashHandle CreateHashHandle(this SafeProvHandle hProv, byte[] hash, int calgHash)
	{
		return hProv.CreateHashHandle(hash, calgHash, throwOnSizeError: true);
	}

	private static SafeHashHandle CreateHashHandle(this SafeProvHandle hProv, byte[] hash, int calgHash, bool throwOnSizeError)
	{
		if (!CryptCreateHash(hProv, calgHash, SafeKeyHandle.InvalidHandle, global::Interop.Advapi32.CryptCreateHashFlags.None, out var phHash))
		{
			int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
			phHash.Dispose();
			throw hRForLastWin32Error.ToCryptographicException();
		}
		try
		{
			int pbData = 0;
			int pdwDataLen = 4;
			if (!global::Interop.Advapi32.CryptGetHashParam(phHash, global::Interop.Advapi32.CryptHashProperty.HP_HASHSIZE, out pbData, ref pdwDataLen, 0))
			{
				int hRForLastWin32Error2 = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error2.ToCryptographicException();
			}
			if (pbData != hash.Length)
			{
				if (throwOnSizeError)
				{
					throw (-2146893821).ToCryptographicException();
				}
				return null;
			}
			if (!global::Interop.Advapi32.CryptSetHashParam(phHash, global::Interop.Advapi32.CryptHashProperty.HP_HASHVAL, hash, 0))
			{
				int hRForLastWin32Error3 = Marshal.GetHRForLastWin32Error();
				throw hRForLastWin32Error3.ToCryptographicException();
			}
			SafeHashHandle result = phHash;
			phHash = null;
			return result;
		}
		finally
		{
			phHash?.Dispose();
		}
	}

	public static CryptographicException GetBadDataException()
	{
		return (-2146893819).ToCryptographicException();
	}

	public static CryptographicException GetEFailException()
	{
		return (-2147467259).ToCryptographicException();
	}

	public static bool CryptGetUserKey(SafeProvHandle safeProvHandle, int dwKeySpec, out SafeKeyHandle safeKeyHandle)
	{
		bool result = global::Interop.Advapi32.CryptGetUserKey(safeProvHandle, dwKeySpec, out safeKeyHandle);
		safeKeyHandle.SetParent(safeProvHandle);
		return result;
	}

	public static bool CryptGenKey(SafeProvHandle safeProvHandle, int algId, int dwFlags, out SafeKeyHandle safeKeyHandle)
	{
		bool result = global::Interop.Advapi32.CryptGenKey(safeProvHandle, algId, dwFlags, out safeKeyHandle);
		safeKeyHandle.SetParent(safeProvHandle);
		return result;
	}

	public unsafe static bool CryptImportKey(SafeProvHandle hProv, ReadOnlySpan<byte> pbData, SafeKeyHandle hPubKey, int dwFlags, out SafeKeyHandle phKey)
	{
		fixed (byte* pbData2 = pbData)
		{
			bool result = global::Interop.Advapi32.CryptImportKey(hProv, pbData2, pbData.Length, hPubKey, dwFlags, out phKey);
			phKey.SetParent(hProv);
			return result;
		}
	}

	public static bool CryptCreateHash(SafeProvHandle hProv, int algId, SafeKeyHandle hKey, global::Interop.Advapi32.CryptCreateHashFlags dwFlags, out SafeHashHandle phHash)
	{
		bool result = global::Interop.Advapi32.CryptCreateHash(hProv, algId, hKey, dwFlags, out phHash);
		phHash.SetParent(hProv);
		return result;
	}

	public static bool CryptDeriveKey(SafeProvHandle hProv, int algId, SafeHashHandle phHash, int dwFlags, out SafeKeyHandle phKey)
	{
		bool result = global::Interop.Advapi32.CryptDeriveKey(hProv, algId, phHash, dwFlags, out phKey);
		phKey.SetParent(hProv);
		return result;
	}

	internal static byte[] ToPlainTextKeyBlob(int algId, byte[] rawKey)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		WriteKeyBlobHeader(algId, binaryWriter);
		binaryWriter.Write(rawKey.Length);
		binaryWriter.Write(rawKey);
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	private static void WriteKeyBlobHeader(int algId, BinaryWriter bw)
	{
		bw.Write((byte)8);
		bw.Write((byte)2);
		bw.Write((ushort)0);
		bw.Write(algId);
	}
}
