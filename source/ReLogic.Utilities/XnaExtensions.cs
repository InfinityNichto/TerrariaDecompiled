using System;

namespace ReLogic.Utilities;

public static class XnaExtensions
{
	public static T Get<T>(this IServiceProvider services) where T : class
	{
		return services.GetService(typeof(T)) as T;
	}
}
