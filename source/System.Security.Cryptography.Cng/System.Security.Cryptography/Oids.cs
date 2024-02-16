namespace System.Security.Cryptography;

internal static class Oids
{
	private static volatile Oid _secp256r1Oid;

	private static volatile Oid _secp384r1Oid;

	private static volatile Oid _secp521r1Oid;

	internal static Oid secp256r1Oid => _secp256r1Oid ?? (_secp256r1Oid = new Oid("1.2.840.10045.3.1.7", "nistP256"));

	internal static Oid secp384r1Oid => _secp384r1Oid ?? (_secp384r1Oid = new Oid("1.3.132.0.34", "nistP384"));

	internal static Oid secp521r1Oid => _secp521r1Oid ?? (_secp521r1Oid = new Oid("1.3.132.0.35", "nistP521"));
}
