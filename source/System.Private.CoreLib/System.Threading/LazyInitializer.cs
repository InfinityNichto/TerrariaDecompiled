using System.Diagnostics.CodeAnalysis;

namespace System.Threading;

public static class LazyInitializer
{
	public static T EnsureInitialized<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>([NotNull] ref T? target) where T : class
	{
		return Volatile.Read(ref target) ?? EnsureInitializedCore(ref target);
	}

	private static T EnsureInitializedCore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>([NotNull] ref T target) where T : class
	{
		try
		{
			Interlocked.CompareExchange(ref target, Activator.CreateInstance<T>(), null);
		}
		catch (MissingMethodException)
		{
			throw new MissingMemberException(SR.Lazy_CreateValue_NoParameterlessCtorForT);
		}
		return target;
	}

	public static T EnsureInitialized<T>([NotNull] ref T? target, Func<T> valueFactory) where T : class
	{
		return Volatile.Read(ref target) ?? EnsureInitializedCore(ref target, valueFactory);
	}

	private static T EnsureInitializedCore<T>([NotNull] ref T target, Func<T> valueFactory) where T : class
	{
		T val = valueFactory();
		if (val == null)
		{
			throw new InvalidOperationException(SR.Lazy_StaticInit_InvalidOperation);
		}
		Interlocked.CompareExchange(ref target, val, null);
		return target;
	}

	public static T EnsureInitialized<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>([AllowNull] ref T target, ref bool initialized, [NotNullIfNotNull("syncLock")] ref object? syncLock)
	{
		if (Volatile.Read(ref initialized))
		{
			return target;
		}
		return EnsureInitializedCore(ref target, ref initialized, ref syncLock);
	}

	private static T EnsureInitializedCore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>([AllowNull] ref T target, ref bool initialized, [NotNull] ref object syncLock)
	{
		lock (EnsureLockInitialized(ref syncLock))
		{
			if (!Volatile.Read(ref initialized))
			{
				try
				{
					target = Activator.CreateInstance<T>();
				}
				catch (MissingMethodException)
				{
					throw new MissingMemberException(SR.Lazy_CreateValue_NoParameterlessCtorForT);
				}
				Volatile.Write(ref initialized, value: true);
			}
		}
		return target;
	}

	public static T EnsureInitialized<T>([AllowNull] ref T target, ref bool initialized, [NotNullIfNotNull("syncLock")] ref object? syncLock, Func<T> valueFactory)
	{
		if (Volatile.Read(ref initialized))
		{
			return target;
		}
		return EnsureInitializedCore(ref target, ref initialized, ref syncLock, valueFactory);
	}

	private static T EnsureInitializedCore<T>([AllowNull] ref T target, ref bool initialized, [NotNull] ref object syncLock, Func<T> valueFactory)
	{
		lock (EnsureLockInitialized(ref syncLock))
		{
			if (!Volatile.Read(ref initialized))
			{
				target = valueFactory();
				Volatile.Write(ref initialized, value: true);
			}
		}
		return target;
	}

	public static T EnsureInitialized<T>([NotNull] ref T? target, [NotNullIfNotNull("syncLock")] ref object? syncLock, Func<T> valueFactory) where T : class
	{
		return Volatile.Read(ref target) ?? EnsureInitializedCore(ref target, ref syncLock, valueFactory);
	}

	private static T EnsureInitializedCore<T>([NotNull] ref T target, [NotNull] ref object syncLock, Func<T> valueFactory) where T : class
	{
		lock (EnsureLockInitialized(ref syncLock))
		{
			if (Volatile.Read(ref target) == null)
			{
				Volatile.Write(ref target, valueFactory());
				if (target == null)
				{
					throw new InvalidOperationException(SR.Lazy_StaticInit_InvalidOperation);
				}
			}
		}
		return target;
	}

	private static object EnsureLockInitialized([NotNull] ref object syncLock)
	{
		return syncLock ?? Interlocked.CompareExchange(ref syncLock, new object(), null) ?? syncLock;
	}
}
