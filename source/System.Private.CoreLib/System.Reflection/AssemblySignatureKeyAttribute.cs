namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class AssemblySignatureKeyAttribute : Attribute
{
	public string PublicKey { get; }

	public string Countersignature { get; }

	public AssemblySignatureKeyAttribute(string publicKey, string countersignature)
	{
		PublicKey = publicKey;
		Countersignature = countersignature;
	}
}
