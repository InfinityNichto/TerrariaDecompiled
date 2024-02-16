using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public class CryptoConfig
{
	private static volatile Dictionary<string, string> s_defaultOidHT;

	private static volatile Dictionary<string, object> s_defaultNameHT;

	private static readonly ConcurrentDictionary<string, Type> appNameHT = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

	private static readonly ConcurrentDictionary<string, string> appOidHT = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static bool AllowOnlyFipsAlgorithms => false;

	private static Dictionary<string, string> DefaultOidHT
	{
		get
		{
			if (s_defaultOidHT != null)
			{
				return s_defaultOidHT;
			}
			int capacity = 37;
			Dictionary<string, string> dictionary = new Dictionary<string, string>(capacity, StringComparer.OrdinalIgnoreCase);
			dictionary.Add("SHA", "1.3.14.3.2.26");
			dictionary.Add("SHA1", "1.3.14.3.2.26");
			dictionary.Add("System.Security.Cryptography.SHA1", "1.3.14.3.2.26");
			dictionary.Add("System.Security.Cryptography.SHA1CryptoServiceProvider", "1.3.14.3.2.26");
			dictionary.Add("System.Security.Cryptography.SHA1Cng", "1.3.14.3.2.26");
			dictionary.Add("System.Security.Cryptography.SHA1Managed", "1.3.14.3.2.26");
			dictionary.Add("SHA256", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("System.Security.Cryptography.SHA256", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("System.Security.Cryptography.SHA256CryptoServiceProvider", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("System.Security.Cryptography.SHA256Cng", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("System.Security.Cryptography.SHA256Managed", "2.16.840.1.101.3.4.2.1");
			dictionary.Add("SHA384", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("System.Security.Cryptography.SHA384", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("System.Security.Cryptography.SHA384CryptoServiceProvider", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("System.Security.Cryptography.SHA384Cng", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("System.Security.Cryptography.SHA384Managed", "2.16.840.1.101.3.4.2.2");
			dictionary.Add("SHA512", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("System.Security.Cryptography.SHA512", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("System.Security.Cryptography.SHA512CryptoServiceProvider", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("System.Security.Cryptography.SHA512Cng", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("System.Security.Cryptography.SHA512Managed", "2.16.840.1.101.3.4.2.3");
			dictionary.Add("RIPEMD160", "1.3.36.3.2.1");
			dictionary.Add("System.Security.Cryptography.RIPEMD160", "1.3.36.3.2.1");
			dictionary.Add("System.Security.Cryptography.RIPEMD160Managed", "1.3.36.3.2.1");
			dictionary.Add("MD5", "1.2.840.113549.2.5");
			dictionary.Add("System.Security.Cryptography.MD5", "1.2.840.113549.2.5");
			dictionary.Add("System.Security.Cryptography.MD5CryptoServiceProvider", "1.2.840.113549.2.5");
			dictionary.Add("System.Security.Cryptography.MD5Managed", "1.2.840.113549.2.5");
			dictionary.Add("TripleDESKeyWrap", "1.2.840.113549.1.9.16.3.6");
			dictionary.Add("RC2", "1.2.840.113549.3.2");
			dictionary.Add("System.Security.Cryptography.RC2CryptoServiceProvider", "1.2.840.113549.3.2");
			dictionary.Add("DES", "1.3.14.3.2.7");
			dictionary.Add("System.Security.Cryptography.DESCryptoServiceProvider", "1.3.14.3.2.7");
			dictionary.Add("TripleDES", "1.2.840.113549.3.7");
			dictionary.Add("System.Security.Cryptography.TripleDESCryptoServiceProvider", "1.2.840.113549.3.7");
			s_defaultOidHT = dictionary;
			return s_defaultOidHT;
		}
	}

	private static Dictionary<string, object> DefaultNameHT
	{
		get
		{
			if (s_defaultNameHT != null)
			{
				return s_defaultNameHT;
			}
			Dictionary<string, object> dictionary = new Dictionary<string, object>(89, StringComparer.OrdinalIgnoreCase);
			Type typeFromHandle = typeof(HMACMD5);
			Type typeFromHandle2 = typeof(HMACSHA1);
			Type typeFromHandle3 = typeof(HMACSHA256);
			Type typeFromHandle4 = typeof(HMACSHA384);
			Type typeFromHandle5 = typeof(HMACSHA512);
			Type typeFromHandle6 = typeof(RijndaelManaged);
			Type typeFromHandle7 = typeof(AesManaged);
			Type typeFromHandle8 = typeof(SHA256Managed);
			Type typeFromHandle9 = typeof(SHA384Managed);
			Type typeFromHandle10 = typeof(SHA512Managed);
			string value = "System.Security.Cryptography.SHA1CryptoServiceProvider, System.Security.Cryptography.Csp";
			string value2 = "System.Security.Cryptography.MD5CryptoServiceProvider,System.Security.Cryptography.Csp";
			string value3 = "System.Security.Cryptography.RSACryptoServiceProvider, System.Security.Cryptography.Csp";
			string value4 = "System.Security.Cryptography.DSACryptoServiceProvider, System.Security.Cryptography.Csp";
			string value5 = "System.Security.Cryptography.DESCryptoServiceProvider, System.Security.Cryptography.Csp";
			string value6 = "System.Security.Cryptography.TripleDESCryptoServiceProvider, System.Security.Cryptography.Csp";
			string value7 = "System.Security.Cryptography.RC2CryptoServiceProvider, System.Security.Cryptography.Csp";
			string value8 = "System.Security.Cryptography.RNGCryptoServiceProvider, System.Security.Cryptography.Csp";
			string value9 = "System.Security.Cryptography.AesCryptoServiceProvider, System.Security.Cryptography.Csp";
			string value10 = "System.Security.Cryptography.ECDsaCng, System.Security.Cryptography.Cng";
			dictionary.Add("RandomNumberGenerator", value8);
			dictionary.Add("System.Security.Cryptography.RandomNumberGenerator", value8);
			dictionary.Add("SHA", value);
			dictionary.Add("SHA1", value);
			dictionary.Add("System.Security.Cryptography.SHA1", value);
			dictionary.Add("System.Security.Cryptography.HashAlgorithm", value);
			dictionary.Add("MD5", value2);
			dictionary.Add("System.Security.Cryptography.MD5", value2);
			dictionary.Add("SHA256", typeFromHandle8);
			dictionary.Add("SHA-256", typeFromHandle8);
			dictionary.Add("System.Security.Cryptography.SHA256", typeFromHandle8);
			dictionary.Add("SHA384", typeFromHandle9);
			dictionary.Add("SHA-384", typeFromHandle9);
			dictionary.Add("System.Security.Cryptography.SHA384", typeFromHandle9);
			dictionary.Add("SHA512", typeFromHandle10);
			dictionary.Add("SHA-512", typeFromHandle10);
			dictionary.Add("System.Security.Cryptography.SHA512", typeFromHandle10);
			dictionary.Add("System.Security.Cryptography.HMAC", typeFromHandle2);
			dictionary.Add("System.Security.Cryptography.KeyedHashAlgorithm", typeFromHandle2);
			dictionary.Add("HMACMD5", typeFromHandle);
			dictionary.Add("System.Security.Cryptography.HMACMD5", typeFromHandle);
			dictionary.Add("HMACSHA1", typeFromHandle2);
			dictionary.Add("System.Security.Cryptography.HMACSHA1", typeFromHandle2);
			dictionary.Add("HMACSHA256", typeFromHandle3);
			dictionary.Add("System.Security.Cryptography.HMACSHA256", typeFromHandle3);
			dictionary.Add("HMACSHA384", typeFromHandle4);
			dictionary.Add("System.Security.Cryptography.HMACSHA384", typeFromHandle4);
			dictionary.Add("HMACSHA512", typeFromHandle5);
			dictionary.Add("System.Security.Cryptography.HMACSHA512", typeFromHandle5);
			dictionary.Add("RSA", value3);
			dictionary.Add("System.Security.Cryptography.RSA", value3);
			dictionary.Add("System.Security.Cryptography.AsymmetricAlgorithm", value3);
			if (!OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS())
			{
				dictionary.Add("DSA", value4);
				dictionary.Add("System.Security.Cryptography.DSA", value4);
			}
			if (OperatingSystem.IsWindows())
			{
				dictionary.Add("ECDsa", value10);
			}
			dictionary.Add("ECDsaCng", value10);
			dictionary.Add("System.Security.Cryptography.ECDsaCng", value10);
			dictionary.Add("DES", value5);
			dictionary.Add("System.Security.Cryptography.DES", value5);
			dictionary.Add("3DES", value6);
			dictionary.Add("TripleDES", value6);
			dictionary.Add("Triple DES", value6);
			dictionary.Add("System.Security.Cryptography.TripleDES", value6);
			dictionary.Add("RC2", value7);
			dictionary.Add("System.Security.Cryptography.RC2", value7);
			dictionary.Add("Rijndael", typeFromHandle6);
			dictionary.Add("System.Security.Cryptography.Rijndael", typeFromHandle6);
			dictionary.Add("System.Security.Cryptography.SymmetricAlgorithm", typeFromHandle6);
			dictionary.Add("AES", value9);
			dictionary.Add("AesCryptoServiceProvider", value9);
			dictionary.Add("System.Security.Cryptography.AesCryptoServiceProvider", value9);
			dictionary.Add("AesManaged", typeFromHandle7);
			dictionary.Add("System.Security.Cryptography.AesManaged", typeFromHandle7);
			dictionary.Add("http://www.w3.org/2000/09/xmldsig#sha1", value);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#sha256", typeFromHandle8);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#sha512", typeFromHandle10);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#des-cbc", value5);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#tripledes-cbc", value6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#kw-tripledes", value6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#aes128-cbc", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#kw-aes128", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#aes192-cbc", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#kw-aes192", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#aes256-cbc", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2001/04/xmlenc#kw-aes256", typeFromHandle6);
			dictionary.Add("http://www.w3.org/2000/09/xmldsig#hmac-sha1", typeFromHandle2);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#md5", value2);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#sha384", typeFromHandle9);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-md5", typeFromHandle);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", typeFromHandle3);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", typeFromHandle4);
			dictionary.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", typeFromHandle5);
			dictionary.Add("2.5.29.10", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, System.Security.Cryptography.X509Certificates");
			dictionary.Add("2.5.29.19", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, System.Security.Cryptography.X509Certificates");
			dictionary.Add("2.5.29.14", "System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension, System.Security.Cryptography.X509Certificates");
			dictionary.Add("2.5.29.15", "System.Security.Cryptography.X509Certificates.X509KeyUsageExtension, System.Security.Cryptography.X509Certificates");
			dictionary.Add("2.5.29.37", "System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension, System.Security.Cryptography.X509Certificates");
			dictionary.Add("X509Chain", "System.Security.Cryptography.X509Certificates.X509Chain, System.Security.Cryptography.X509Certificates");
			dictionary.Add("1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType, System.Security.Cryptography.Pkcs");
			dictionary.Add("1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest, System.Security.Cryptography.Pkcs");
			dictionary.Add("1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime, System.Security.Cryptography.Pkcs");
			dictionary.Add("1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName, System.Security.Cryptography.Pkcs");
			dictionary.Add("1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription, System.Security.Cryptography.Pkcs");
			s_defaultNameHT = dictionary;
			return s_defaultNameHT;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public static void AddAlgorithm(Type algorithm, params string[] names)
	{
		if (algorithm == null)
		{
			throw new ArgumentNullException("algorithm");
		}
		if (!algorithm.IsVisible)
		{
			throw new ArgumentException(System.SR.Cryptography_AlgorithmTypesMustBeVisible, "algorithm");
		}
		if (names == null)
		{
			throw new ArgumentNullException("names");
		}
		string[] array = new string[names.Length];
		Array.Copy(names, array, array.Length);
		string[] array2 = array;
		foreach (string value in array2)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(System.SR.Cryptography_AddNullOrEmptyName);
			}
		}
		string[] array3 = array;
		foreach (string key in array3)
		{
			appNameHT[key] = algorithm;
		}
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static object? CreateFromName(string name, params object?[]? args)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		appNameHT.TryGetValue(name, out var value);
		if (value == null && DefaultNameHT.TryGetValue(name, out object value2))
		{
			value = value2 as Type;
			if (value == null && value2 is string typeName)
			{
				value = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
				if (value != null && !value.IsVisible)
				{
					value = null;
				}
				if (value != null)
				{
					appNameHT[name] = value;
				}
			}
		}
		if (value == null && (args == null || args.Length == 1) && name == "ECDsa")
		{
			return ECDsa.Create();
		}
		if (value == null)
		{
			value = Type.GetType(name, throwOnError: false, ignoreCase: false);
			if (value != null && !value.IsVisible)
			{
				value = null;
			}
		}
		if (value == null)
		{
			return null;
		}
		MethodBase[] constructors = value.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance);
		MethodBase[] array = constructors;
		if (array == null)
		{
			return null;
		}
		if (args == null)
		{
			args = Array.Empty<object>();
		}
		List<MethodBase> list = new List<MethodBase>();
		foreach (MethodBase methodBase in array)
		{
			if (methodBase.GetParameters().Length == args.Length)
			{
				list.Add(methodBase);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		array = list.ToArray();
		object state;
		ConstructorInfo constructorInfo = Type.DefaultBinder.BindToMethod(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, array, ref args, null, null, null, out state) as ConstructorInfo;
		if (constructorInfo == null || typeof(Delegate).IsAssignableFrom(constructorInfo.DeclaringType))
		{
			return null;
		}
		object result = constructorInfo.Invoke(BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, Type.DefaultBinder, args, null);
		if (state != null)
		{
			Type.DefaultBinder.ReorderArgumentArray(ref args, state);
		}
		return result;
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static object? CreateFromName(string name)
	{
		return CreateFromName(name, null);
	}

	[UnsupportedOSPlatform("browser")]
	public static void AddOID(string oid, params string[] names)
	{
		if (oid == null)
		{
			throw new ArgumentNullException("oid");
		}
		if (names == null)
		{
			throw new ArgumentNullException("names");
		}
		string[] array = new string[names.Length];
		Array.Copy(names, array, array.Length);
		string[] array2 = array;
		foreach (string value in array2)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(System.SR.Cryptography_AddNullOrEmptyName);
			}
		}
		string[] array3 = array;
		foreach (string key in array3)
		{
			appOidHT[key] = oid;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public static string? MapNameToOID(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		appOidHT.TryGetValue(name, out var value);
		if (string.IsNullOrEmpty(value) && !DefaultOidHT.TryGetValue(name, out value))
		{
			try
			{
				Oid oid = Oid.FromFriendlyName(name, OidGroup.All);
				value = oid.Value;
				return value;
			}
			catch (CryptographicException)
			{
			}
		}
		return value;
	}

	[UnsupportedOSPlatform("browser")]
	[Obsolete("EncodeOID is obsolete. Use the ASN.1 functionality provided in System.Formats.Asn1.", DiagnosticId = "SYSLIB0031", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static byte[] EncodeOID(string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		string[] array = str.Split('.');
		uint[] array2 = new uint[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = (uint)int.Parse(array[i], CultureInfo.InvariantCulture);
		}
		if (array2.Length < 2)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_InvalidOID);
		}
		uint value = array2[0] * 40 + array2[1];
		int index = 2;
		EncodeSingleOidNum(value, null, ref index);
		for (int j = 2; j < array2.Length; j++)
		{
			EncodeSingleOidNum(array2[j], null, ref index);
		}
		byte[] array3 = new byte[index];
		int index2 = 2;
		EncodeSingleOidNum(value, array3, ref index2);
		for (int k = 2; k < array2.Length; k++)
		{
			EncodeSingleOidNum(array2[k], array3, ref index2);
		}
		if (index2 - 2 > 127)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_Config_EncodedOIDError);
		}
		array3[0] = 6;
		array3[1] = (byte)(index2 - 2);
		return array3;
	}

	private static void EncodeSingleOidNum(uint value, byte[] destination, ref int index)
	{
		if ((int)value < 128)
		{
			if (destination != null)
			{
				destination[index++] = (byte)value;
			}
			else
			{
				index++;
			}
		}
		else if (value < 16384)
		{
			if (destination != null)
			{
				destination[index++] = (byte)((value >> 7) | 0x80u);
				destination[index++] = (byte)(value & 0x7Fu);
			}
			else
			{
				index += 2;
			}
		}
		else if (value < 2097152)
		{
			if (destination != null)
			{
				destination[index++] = (byte)((value >> 14) | 0x80u);
				destination[index++] = (byte)((value >> 7) | 0x80u);
				destination[index++] = (byte)(value & 0x7Fu);
			}
			else
			{
				index += 3;
			}
		}
		else if (value < 268435456)
		{
			if (destination != null)
			{
				destination[index++] = (byte)((value >> 21) | 0x80u);
				destination[index++] = (byte)((value >> 14) | 0x80u);
				destination[index++] = (byte)((value >> 7) | 0x80u);
				destination[index++] = (byte)(value & 0x7Fu);
			}
			else
			{
				index += 4;
			}
		}
		else if (destination != null)
		{
			destination[index++] = (byte)((value >> 28) | 0x80u);
			destination[index++] = (byte)((value >> 21) | 0x80u);
			destination[index++] = (byte)((value >> 14) | 0x80u);
			destination[index++] = (byte)((value >> 7) | 0x80u);
			destination[index++] = (byte)(value & 0x7Fu);
		}
		else
		{
			index += 5;
		}
	}
}
