using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public struct ECParameters
{
	public ECPoint Q;

	public byte[]? D;

	public ECCurve Curve;

	public void Validate()
	{
		bool flag = true;
		if (D != null && Q.Y == null && Q.X == null)
		{
			flag = false;
		}
		if (Q.Y != null && Q.X != null && Q.Y.Length == Q.X.Length)
		{
			flag = false;
		}
		if (!flag)
		{
			if (Curve.IsExplicit)
			{
				flag = D != null && D.Length != Curve.Order.Length;
			}
			else if (Curve.IsNamed && Q.X != null)
			{
				flag = D != null && D.Length != Q.X.Length;
			}
		}
		if (flag)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidCurveKeyParameters);
		}
		Curve.Validate();
	}
}
