using System;
using ReLogic.OS.Linux;
using ReLogic.OS.OSX;
using ReLogic.OS.Windows;
using ReLogic.Utilities;

namespace ReLogic.OS;

public abstract class Platform : IDisposable
{
	public static readonly Platform Current = (OperatingSystem.IsWindows() ? new WindowsPlatform() : (OperatingSystem.IsMacOS() ? ((Platform)new OsxPlatform()) : ((Platform)new LinuxPlatform())));

	public readonly PlatformType Type;

	private TypeInstanceCollection<object> _services = new TypeInstanceCollection<object>();

	private bool _disposedValue;

	public static bool IsWindows => Current.Type == PlatformType.Windows;

	public static bool IsOSX => Current.Type == PlatformType.OSX;

	public static bool IsLinux => Current.Type == PlatformType.Linux;

	protected Platform(PlatformType type)
	{
		Type = type;
	}

	protected void RegisterService<T>(T service) where T : class
	{
		if (_services.Has<T>())
		{
			_services.Remove<T>();
		}
		_services.Register(service);
	}

	public abstract void InitializeClientServices(IntPtr windowHandle);

	public static T Get<T>() where T : class
	{
		return Current._services.Get<T>();
	}

	public static bool Has<T>() where T : class
	{
		return Current._services.Has<T>();
	}

	public static void IfHas<T>(Action<T> callback) where T : class
	{
		Current._services.IfHas(callback);
	}

	public static U IfHas<T, U>(Func<T, U> callback) where T : class
	{
		return Current._services.IfHas(callback);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing && _services != null)
			{
				_services.Dispose();
				_services = null;
			}
			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
