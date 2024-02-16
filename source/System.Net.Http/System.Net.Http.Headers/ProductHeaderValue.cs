using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers;

public class ProductHeaderValue : ICloneable
{
	private readonly string _name;

	private readonly string _version;

	public string Name => _name;

	public string? Version => _version;

	public ProductHeaderValue(string name)
		: this(name, null)
	{
	}

	public ProductHeaderValue(string name, string? version)
	{
		HeaderUtilities.CheckValidToken(name, "name");
		if (!string.IsNullOrEmpty(version))
		{
			HeaderUtilities.CheckValidToken(version, "version");
			_version = version;
		}
		_name = name;
	}

	private ProductHeaderValue(ProductHeaderValue source)
	{
		_name = source._name;
		_version = source._version;
	}

	public override string ToString()
	{
		if (string.IsNullOrEmpty(_version))
		{
			return _name;
		}
		return _name + "/" + _version;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ProductHeaderValue productHeaderValue))
		{
			return false;
		}
		if (string.Equals(_name, productHeaderValue._name, StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(_version, productHeaderValue._version, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = StringComparer.OrdinalIgnoreCase.GetHashCode(_name);
		if (!string.IsNullOrEmpty(_version))
		{
			num ^= StringComparer.OrdinalIgnoreCase.GetHashCode(_version);
		}
		return num;
	}

	public static ProductHeaderValue Parse(string? input)
	{
		int index = 0;
		return (ProductHeaderValue)GenericHeaderParser.SingleValueProductParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out ProductHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.SingleValueProductParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (ProductHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetProductLength(string input, int startIndex, out ProductHeaderValue parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
		if (tokenLength == 0)
		{
			return 0;
		}
		string name = input.Substring(startIndex, tokenLength);
		int num = startIndex + tokenLength;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		if (num == input.Length || input[num] != '/')
		{
			parsedValue = new ProductHeaderValue(name);
			return num - startIndex;
		}
		num++;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		int tokenLength2 = HttpRuleParser.GetTokenLength(input, num);
		if (tokenLength2 == 0)
		{
			return 0;
		}
		string version = input.Substring(num, tokenLength2);
		num += tokenLength2;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		parsedValue = new ProductHeaderValue(name, version);
		return num - startIndex;
	}

	object ICloneable.Clone()
	{
		return new ProductHeaderValue(this);
	}
}
