using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.X509Certificates;

public struct X509ChainStatus
{
	private string _statusInformation;

	public X509ChainStatusFlags Status { get; set; }

	public string StatusInformation
	{
		get
		{
			if (_statusInformation == null)
			{
				return string.Empty;
			}
			return _statusInformation;
		}
		[param: AllowNull]
		set
		{
			_statusInformation = value;
		}
	}
}
