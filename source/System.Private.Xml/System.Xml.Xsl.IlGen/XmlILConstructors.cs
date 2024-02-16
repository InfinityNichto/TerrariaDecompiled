using System.Diagnostics;
using System.Reflection;
using System.Security;

namespace System.Xml.Xsl.IlGen;

internal static class XmlILConstructors
{
	public static readonly ConstructorInfo DecFromParts = typeof(decimal).GetConstructor(new Type[5]
	{
		typeof(int),
		typeof(int),
		typeof(int),
		typeof(bool),
		typeof(byte)
	});

	public static readonly ConstructorInfo DecFromInt32 = typeof(decimal).GetConstructor(new Type[1] { typeof(int) });

	public static readonly ConstructorInfo DecFromInt64 = typeof(decimal).GetConstructor(new Type[1] { typeof(long) });

	public static readonly ConstructorInfo Debuggable = typeof(DebuggableAttribute).GetConstructor(new Type[1] { typeof(DebuggableAttribute.DebuggingModes) });

	public static readonly ConstructorInfo NonUserCode = typeof(DebuggerNonUserCodeAttribute).GetConstructor(Type.EmptyTypes);

	public static readonly ConstructorInfo QName = typeof(XmlQualifiedName).GetConstructor(new Type[2]
	{
		typeof(string),
		typeof(string)
	});

	public static readonly ConstructorInfo StepThrough = typeof(DebuggerStepThroughAttribute).GetConstructor(Type.EmptyTypes);

	public static readonly ConstructorInfo Transparent = typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes);
}
