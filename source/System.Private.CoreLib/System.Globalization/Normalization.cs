using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization;

internal static class Normalization
{
	internal static bool IsNormalized(string strInput, NormalizationForm normalizationForm)
	{
		if (GlobalizationMode.Invariant)
		{
			return true;
		}
		if (!GlobalizationMode.UseNls)
		{
			return IcuIsNormalized(strInput, normalizationForm);
		}
		return NlsIsNormalized(strInput, normalizationForm);
	}

	internal static string Normalize(string strInput, NormalizationForm normalizationForm)
	{
		if (GlobalizationMode.Invariant)
		{
			return strInput;
		}
		if (!GlobalizationMode.UseNls)
		{
			return IcuNormalize(strInput, normalizationForm);
		}
		return NlsNormalize(strInput, normalizationForm);
	}

	private unsafe static bool IcuIsNormalized(string strInput, NormalizationForm normalizationForm)
	{
		ValidateArguments(strInput, normalizationForm);
		int num;
		fixed (char* src = strInput)
		{
			num = Interop.Globalization.IsNormalized(normalizationForm, src, strInput.Length);
		}
		if (num == -1)
		{
			throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "strInput");
		}
		return num == 1;
	}

	private unsafe static string IcuNormalize(string strInput, NormalizationForm normalizationForm)
	{
		ValidateArguments(strInput, normalizationForm);
		char[] array = null;
		try
		{
			Span<char> span = ((strInput.Length > 512) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(strInput.Length))) : stackalloc char[512]);
			Span<char> span2 = span;
			for (int i = 0; i < 2; i++)
			{
				int num;
				fixed (char* src = strInput)
				{
					fixed (char* dstBuffer = &MemoryMarshal.GetReference(span2))
					{
						num = Interop.Globalization.NormalizeString(normalizationForm, src, strInput.Length, dstBuffer, span2.Length);
					}
				}
				if (num == -1)
				{
					throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "strInput");
				}
				if (num <= span2.Length)
				{
					ReadOnlySpan<char> readOnlySpan = span2.Slice(0, num);
					return readOnlySpan.SequenceEqual(strInput) ? strInput : new string(readOnlySpan);
				}
				if (i == 0)
				{
					if (array != null)
					{
						char[] array2 = array;
						array = null;
						ArrayPool<char>.Shared.Return(array2);
					}
					span2 = (array = ArrayPool<char>.Shared.Rent(num));
				}
			}
			throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "strInput");
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}
	}

	private static void ValidateArguments(string strInput, NormalizationForm normalizationForm)
	{
		if (OperatingSystem.IsBrowser())
		{
		}
		if (normalizationForm != NormalizationForm.FormC && normalizationForm != NormalizationForm.FormD && normalizationForm != NormalizationForm.FormKC && normalizationForm != NormalizationForm.FormKD)
		{
			throw new ArgumentException(SR.Argument_InvalidNormalizationForm, "normalizationForm");
		}
		if (HasInvalidUnicodeSequence(strInput))
		{
			throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "strInput");
		}
	}

	private static bool HasInvalidUnicodeSequence(string s)
	{
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (c < '\ud800')
			{
				continue;
			}
			if (c == '\ufffe')
			{
				return true;
			}
			if (char.IsLowSurrogate(c))
			{
				return true;
			}
			if (char.IsHighSurrogate(c))
			{
				if (i + 1 >= s.Length || !char.IsLowSurrogate(s[i + 1]))
				{
					return true;
				}
				i++;
			}
		}
		return false;
	}

	private unsafe static bool NlsIsNormalized(string strInput, NormalizationForm normalizationForm)
	{
		Interop.BOOL bOOL;
		fixed (char* source = strInput)
		{
			bOOL = Interop.Normaliz.IsNormalizedString(normalizationForm, source, strInput.Length);
		}
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		switch (lastPInvokeError)
		{
		case 87:
		case 1113:
			if (normalizationForm != NormalizationForm.FormC && normalizationForm != NormalizationForm.FormD && normalizationForm != NormalizationForm.FormKC && normalizationForm != NormalizationForm.FormKD)
			{
				throw new ArgumentException(SR.Argument_InvalidNormalizationForm, "normalizationForm");
			}
			throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "strInput");
		case 8:
			throw new OutOfMemoryException();
		default:
			throw new InvalidOperationException(SR.Format(SR.UnknownError_Num, lastPInvokeError));
		case 0:
			return bOOL != Interop.BOOL.FALSE;
		}
	}

	private unsafe static string NlsNormalize(string strInput, NormalizationForm normalizationForm)
	{
		if (strInput.Length == 0)
		{
			return string.Empty;
		}
		char[] array = null;
		try
		{
			Span<char> span = ((strInput.Length > 512) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(strInput.Length))) : stackalloc char[512]);
			Span<char> span2 = span;
			while (true)
			{
				int num;
				fixed (char* source = strInput)
				{
					fixed (char* destination = &MemoryMarshal.GetReference(span2))
					{
						num = Interop.Normaliz.NormalizeString(normalizationForm, source, strInput.Length, destination, span2.Length);
					}
				}
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				switch (lastPInvokeError)
				{
				case 0:
				{
					ReadOnlySpan<char> readOnlySpan = span2.Slice(0, num);
					return readOnlySpan.SequenceEqual(strInput) ? strInput : new string(readOnlySpan);
				}
				case 122:
					num = Math.Abs(num);
					if (array != null)
					{
						char[] array2 = array;
						array = null;
						ArrayPool<char>.Shared.Return(array2);
					}
					break;
				case 87:
				case 1113:
					if (normalizationForm != NormalizationForm.FormC && normalizationForm != NormalizationForm.FormD && normalizationForm != NormalizationForm.FormKC && normalizationForm != NormalizationForm.FormKD)
					{
						throw new ArgumentException(SR.Argument_InvalidNormalizationForm, "normalizationForm");
					}
					throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "strInput");
				case 8:
					throw new OutOfMemoryException();
				default:
					throw new InvalidOperationException(SR.Format(SR.UnknownError_Num, lastPInvokeError));
				}
				span2 = (array = ArrayPool<char>.Shared.Rent(num));
			}
		}
		finally
		{
			if (array != null)
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}
	}
}
