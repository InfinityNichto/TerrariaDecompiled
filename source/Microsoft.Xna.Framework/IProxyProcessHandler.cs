using System;

namespace Microsoft.Xna.Framework;

internal interface IProxyProcessHandler
{
	uint AsyncManagedCallArgument { get; }

	ManagedCallType AsyncManagedCallType { get; }

	uint AsyncHResult { get; }

	IntPtr SharedAsyncDataSafeToWrite { get; }

	IntPtr ProxyProcessWantsToTalk { get; }
}
