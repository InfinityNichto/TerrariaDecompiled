using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class WeakReference : ISerializable
{
	internal IntPtr m_handle;

	public virtual extern bool IsAlive
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		get;
	}

	public virtual bool TrackResurrection => IsTrackResurrection();

	public virtual extern object? Target
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		get;
		[MethodImpl(MethodImplOptions.InternalCall)]
		set;
	}

	protected WeakReference()
	{
		throw new NotImplementedException();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	extern ~WeakReference();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void Create(object target, bool trackResurrection);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern bool IsTrackResurrection();

	public WeakReference(object? target)
		: this(target, trackResurrection: false)
	{
	}

	public WeakReference(object? target, bool trackResurrection)
	{
		Create(target, trackResurrection);
	}

	protected WeakReference(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		object value = info.GetValue("TrackedObject", typeof(object));
		bool boolean = info.GetBoolean("TrackResurrection");
		Create(value, boolean);
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("TrackedObject", Target, typeof(object));
		info.AddValue("TrackResurrection", IsTrackResurrection());
	}
}
[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class WeakReference<T> : ISerializable where T : class?
{
	internal IntPtr m_handle;

	private extern T Target
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		[return: MaybeNull]
		get;
		[MethodImpl(MethodImplOptions.InternalCall)]
		set;
	}

	public void SetTarget(T target)
	{
		Target = target;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	extern ~WeakReference();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern void Create(T target, bool trackResurrection);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern bool IsTrackResurrection();

	public WeakReference(T target)
		: this(target, trackResurrection: false)
	{
	}

	public WeakReference(T target, bool trackResurrection)
	{
		Create(target, trackResurrection);
	}

	private WeakReference(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		T target = (T)info.GetValue("TrackedObject", typeof(T));
		bool boolean = info.GetBoolean("TrackResurrection");
		Create(target, boolean);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryGetTarget([MaybeNullWhen(false)][NotNullWhen(true)] out T target)
	{
		return (target = Target) != null;
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("TrackedObject", Target, typeof(T));
		info.AddValue("TrackResurrection", IsTrackResurrection());
	}
}
