using System.Globalization;

namespace System.Reflection;

internal struct MetadataToken
{
	public int Value;

	public bool IsGlobalTypeDefToken => Value == 33554433;

	public MetadataTokenType TokenType => (MetadataTokenType)(Value & 0xFF000000u);

	public bool IsTypeRef => TokenType == MetadataTokenType.TypeRef;

	public bool IsTypeDef => TokenType == MetadataTokenType.TypeDef;

	public bool IsFieldDef => TokenType == MetadataTokenType.FieldDef;

	public bool IsMethodDef => TokenType == MetadataTokenType.MethodDef;

	public bool IsMemberRef => TokenType == MetadataTokenType.MemberRef;

	public bool IsEvent => TokenType == MetadataTokenType.Event;

	public bool IsProperty => TokenType == MetadataTokenType.Property;

	public bool IsParamDef => TokenType == MetadataTokenType.ParamDef;

	public bool IsTypeSpec => TokenType == MetadataTokenType.TypeSpec;

	public bool IsMethodSpec => TokenType == MetadataTokenType.MethodSpec;

	public bool IsString => TokenType == MetadataTokenType.String;

	public bool IsSignature => TokenType == MetadataTokenType.Signature;

	public bool IsGenericPar => TokenType == MetadataTokenType.GenericPar;

	public static implicit operator int(MetadataToken token)
	{
		return token.Value;
	}

	public static implicit operator MetadataToken(int token)
	{
		return new MetadataToken(token);
	}

	public static bool IsNullToken(int token)
	{
		return (token & 0xFFFFFF) == 0;
	}

	public MetadataToken(int token)
	{
		Value = token;
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "0x{0:x8}", Value);
	}
}
