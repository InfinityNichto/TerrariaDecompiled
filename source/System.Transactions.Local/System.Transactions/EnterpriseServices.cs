namespace System.Transactions;

internal static class EnterpriseServices
{
	internal static bool EnterpriseServicesOk => false;

	internal static bool CreatedServiceDomain { get; }

	internal static void VerifyEnterpriseServicesOk()
	{
		_ = EnterpriseServicesOk;
		ThrowNotSupported();
	}

	internal static Transaction GetContextTransaction(ContextData contextData)
	{
		if (EnterpriseServicesOk)
		{
		}
		return null;
	}

	internal static bool UseServiceDomainForCurrent()
	{
		return false;
	}

	internal static void PushServiceDomain(Transaction newCurrent)
	{
		ThrowNotSupported();
	}

	internal static void LeaveServiceDomain()
	{
		ThrowNotSupported();
	}

	private static void ThrowNotSupported()
	{
		throw new PlatformNotSupportedException(System.SR.EsNotSupported);
	}
}
