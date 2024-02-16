namespace System.Security.Cryptography;

internal static class Oids
{
	private static volatile Oid _rsaOid;

	private static volatile Oid _ecPublicKeyOid;

	private static volatile Oid _pkcs9ExtensionRequestOid;

	private static volatile Oid _basicConstraints2Oid;

	private static volatile Oid _enhancedKeyUsageOid;

	private static volatile Oid _keyUsageOid;

	private static volatile Oid _subjectKeyIdentifierOid;

	internal static Oid RsaOid => _rsaOid ?? (_rsaOid = InitializeOid("1.2.840.113549.1.1.1"));

	internal static Oid EcPublicKeyOid => _ecPublicKeyOid ?? (_ecPublicKeyOid = InitializeOid("1.2.840.10045.2.1"));

	internal static Oid Pkcs9ExtensionRequestOid => _pkcs9ExtensionRequestOid ?? (_pkcs9ExtensionRequestOid = InitializeOid("1.2.840.113549.1.9.14"));

	internal static Oid BasicConstraints2Oid => _basicConstraints2Oid ?? (_basicConstraints2Oid = InitializeOid("2.5.29.19"));

	internal static Oid EnhancedKeyUsageOid => _enhancedKeyUsageOid ?? (_enhancedKeyUsageOid = InitializeOid("2.5.29.37"));

	internal static Oid KeyUsageOid => _keyUsageOid ?? (_keyUsageOid = InitializeOid("2.5.29.15"));

	internal static Oid SubjectKeyIdentifierOid => _subjectKeyIdentifierOid ?? (_subjectKeyIdentifierOid = InitializeOid("2.5.29.14"));

	private static Oid InitializeOid(string oidValue)
	{
		Oid oid = new Oid(oidValue, null);
		_ = oid.FriendlyName;
		return oid;
	}
}
