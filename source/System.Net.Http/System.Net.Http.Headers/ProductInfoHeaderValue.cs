using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http.Headers;

public class ProductInfoHeaderValue : ICloneable
{
	private ProductHeaderValue _product;

	private string _comment;

	public ProductHeaderValue? Product => _product;

	public string? Comment => _comment;

	public ProductInfoHeaderValue(string productName, string? productVersion)
		: this(new ProductHeaderValue(productName, productVersion))
	{
	}

	public ProductInfoHeaderValue(ProductHeaderValue product)
	{
		if (product == null)
		{
			throw new ArgumentNullException("product");
		}
		_product = product;
	}

	public ProductInfoHeaderValue(string comment)
	{
		HeaderUtilities.CheckValidComment(comment, "comment");
		_comment = comment;
	}

	private ProductInfoHeaderValue(ProductInfoHeaderValue source)
	{
		_product = source._product;
		_comment = source._comment;
	}

	public override string ToString()
	{
		if (_product == null)
		{
			return _comment;
		}
		return _product.ToString();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ProductInfoHeaderValue productInfoHeaderValue))
		{
			return false;
		}
		if (_product == null)
		{
			return string.Equals(_comment, productInfoHeaderValue._comment, StringComparison.Ordinal);
		}
		return _product.Equals(productInfoHeaderValue._product);
	}

	public override int GetHashCode()
	{
		if (_product == null)
		{
			return _comment.GetHashCode();
		}
		return _product.GetHashCode();
	}

	public static ProductInfoHeaderValue Parse(string input)
	{
		int index = 0;
		object obj = ProductInfoHeaderParser.SingleValueParser.ParseValue(input, null, ref index);
		if (index < input.Length)
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, input.Substring(index)));
		}
		return (ProductInfoHeaderValue)obj;
	}

	public static bool TryParse([NotNullWhen(true)] string input, [NotNullWhen(true)] out ProductInfoHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (ProductInfoHeaderParser.SingleValueParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			if (index < input.Length)
			{
				return false;
			}
			parsedValue = (ProductInfoHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetProductInfoLength(string input, int startIndex, out ProductInfoHeaderValue parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int num = startIndex;
		string text = null;
		ProductHeaderValue parsedValue2 = null;
		if (input[num] == '(')
		{
			int length = 0;
			if (HttpRuleParser.GetCommentLength(input, num, out length) != 0)
			{
				return 0;
			}
			text = input.Substring(num, length);
			num += length;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			parsedValue = new ProductInfoHeaderValue(text);
		}
		else
		{
			int productLength = ProductHeaderValue.GetProductLength(input, num, out parsedValue2);
			if (productLength == 0)
			{
				return 0;
			}
			num += productLength;
			parsedValue = new ProductInfoHeaderValue(parsedValue2);
		}
		return num - startIndex;
	}

	object ICloneable.Clone()
	{
		return new ProductInfoHeaderValue(this);
	}
}
