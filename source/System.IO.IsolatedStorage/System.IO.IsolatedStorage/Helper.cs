using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace System.IO.IsolatedStorage;

internal static class Helper
{
	private static string s_machineRootDirectory;

	private static string s_roamingUserRootDirectory;

	private static string s_userRootDirectory;

	internal static string GetRootDirectory(IsolatedStorageScope scope)
	{
		if (IsRoaming(scope))
		{
			if (string.IsNullOrEmpty(s_roamingUserRootDirectory))
			{
				s_roamingUserRootDirectory = GetDataDirectory(scope);
			}
			return s_roamingUserRootDirectory;
		}
		if (IsMachine(scope))
		{
			if (string.IsNullOrEmpty(s_machineRootDirectory))
			{
				s_machineRootDirectory = GetRandomDirectory(GetDataDirectory(scope), scope);
			}
			return s_machineRootDirectory;
		}
		if (string.IsNullOrEmpty(s_userRootDirectory))
		{
			s_userRootDirectory = GetRandomDirectory(GetDataDirectory(scope), scope);
		}
		return s_userRootDirectory;
	}

	internal static bool IsMachine(IsolatedStorageScope scope)
	{
		return (scope & IsolatedStorageScope.Machine) != 0;
	}

	internal static bool IsAssembly(IsolatedStorageScope scope)
	{
		return (scope & IsolatedStorageScope.Assembly) != 0;
	}

	internal static bool IsApplication(IsolatedStorageScope scope)
	{
		return (scope & IsolatedStorageScope.Application) != 0;
	}

	internal static bool IsRoaming(IsolatedStorageScope scope)
	{
		return (scope & IsolatedStorageScope.Roaming) != 0;
	}

	internal static bool IsDomain(IsolatedStorageScope scope)
	{
		return (scope & IsolatedStorageScope.Domain) != 0;
	}

	internal static string GetDataDirectory(IsolatedStorageScope scope)
	{
		Environment.SpecialFolder folder = (IsMachine(scope) ? Environment.SpecialFolder.CommonApplicationData : (IsRoaming(scope) ? Environment.SpecialFolder.ApplicationData : Environment.SpecialFolder.LocalApplicationData));
		string folderPath = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create);
		return Path.Combine(folderPath, "IsolatedStorage");
	}

	[UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "Code handles single-file deployment by using the information of the .exe file")]
	internal static void GetDefaultIdentityAndHash(out object identity, out string hash, char separator)
	{
		Assembly entryAssembly = Assembly.GetEntryAssembly();
		string text = null;
		if (entryAssembly != null)
		{
			AssemblyName name = entryAssembly.GetName();
			hash = IdentityHelper.GetNormalizedStrongNameHash(name);
			if (hash != null)
			{
				hash = "StrongName" + separator + hash;
				identity = name;
				return;
			}
			text = entryAssembly.Location;
		}
		if (string.IsNullOrEmpty(text))
		{
			text = Environment.ProcessPath;
		}
		if (string.IsNullOrEmpty(text))
		{
			throw new IsolatedStorageException(System.SR.IsolatedStorage_Init);
		}
		Uri uri = new Uri(text);
		hash = "Url" + separator + IdentityHelper.GetNormalizedUriHash(uri);
		identity = uri;
	}

	internal static string GetRandomDirectory(string rootDirectory, IsolatedStorageScope scope)
	{
		string text = GetExistingRandomDirectory(rootDirectory);
		if (string.IsNullOrEmpty(text))
		{
			using Mutex mutex = CreateMutexNotOwned(rootDirectory);
			if (!mutex.WaitOne())
			{
				throw new IsolatedStorageException(System.SR.IsolatedStorage_Init);
			}
			try
			{
				text = GetExistingRandomDirectory(rootDirectory);
				if (string.IsNullOrEmpty(text))
				{
					text = Path.Combine(rootDirectory, Path.GetRandomFileName(), Path.GetRandomFileName());
					CreateDirectory(text, scope);
				}
			}
			finally
			{
				mutex.ReleaseMutex();
			}
		}
		return text;
	}

	internal static string GetExistingRandomDirectory(string rootDirectory)
	{
		if (!Directory.Exists(rootDirectory))
		{
			return null;
		}
		string[] directories = Directory.GetDirectories(rootDirectory);
		foreach (string path in directories)
		{
			string? fileName = Path.GetFileName(path);
			if (fileName == null || fileName.Length != 12)
			{
				continue;
			}
			string[] directories2 = Directory.GetDirectories(path);
			foreach (string text in directories2)
			{
				string? fileName2 = Path.GetFileName(text);
				if (fileName2 != null && fileName2.Length == 12)
				{
					return text;
				}
			}
		}
		return null;
	}

	private static Mutex CreateMutexNotOwned(string pathName)
	{
		return new Mutex(initiallyOwned: false, "Global\\" + IdentityHelper.GetStrongHashSuitableForObjectName(pathName));
	}

	internal static void CreateDirectory(string path, IsolatedStorageScope scope)
	{
		if (!Directory.Exists(path))
		{
			DirectoryInfo directoryInfo = Directory.CreateDirectory(path);
			if (IsMachine(scope))
			{
				DirectorySecurity directorySecurity = new DirectorySecurity();
				directorySecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
				directorySecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Read | FileSystemRights.Write, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
				directorySecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
				directorySecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null), FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
				directoryInfo.SetAccessControl(directorySecurity);
			}
		}
	}
}
