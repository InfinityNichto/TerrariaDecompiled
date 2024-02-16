using System.Runtime.CompilerServices;

namespace System.Net;

[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public enum CookieVariant
{
	Unknown = 0,
	Plain = 1,
	Rfc2109 = 2,
	Rfc2965 = 3,
	Default = 2
}
