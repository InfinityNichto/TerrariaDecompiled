using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal static class HashAlgorithmNames
{
	private static readonly HashSet<string> s_allNames = CreateAllNames();

	public static string ToAlgorithmName(this HashAlgorithm hashAlgorithm)
	{
		if (hashAlgorithm is SHA1)
		{
			return "SHA1";
		}
		if (hashAlgorithm is SHA256)
		{
			return "SHA256";
		}
		if (hashAlgorithm is SHA384)
		{
			return "SHA384";
		}
		if (hashAlgorithm is SHA512)
		{
			return "SHA512";
		}
		if (hashAlgorithm is MD5)
		{
			return "MD5";
		}
		return hashAlgorithm.ToString();
	}

	public static string ToUpper(string hashAlgorithName)
	{
		if (s_allNames.Contains(hashAlgorithName))
		{
			return hashAlgorithName.ToUpperInvariant();
		}
		return hashAlgorithName;
	}

	private static HashSet<string> CreateAllNames()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		hashSet.Add("SHA1");
		hashSet.Add("SHA256");
		hashSet.Add("SHA384");
		hashSet.Add("SHA512");
		hashSet.Add("MD5");
		return hashSet;
	}
}
