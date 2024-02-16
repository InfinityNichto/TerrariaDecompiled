using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace System.Diagnostics;

internal sealed class PerformanceCounterLib
{
	internal sealed class PerformanceMonitor
	{
		private RegistryKey _perfDataKey;

		private readonly string _machineName;

		internal PerformanceMonitor(string machineName)
		{
			_machineName = machineName;
			Init();
		}

		[MemberNotNull("_perfDataKey")]
		private void Init()
		{
			if (ProcessManager.IsRemoteMachine(_machineName))
			{
				_perfDataKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.PerformanceData, _machineName);
			}
			else
			{
				_perfDataKey = Registry.PerformanceData;
			}
		}

		internal byte[] GetData(string item)
		{
			int num = 17;
			int num2 = 0;
			byte[] array = null;
			int num3 = 0;
			while (num > 0)
			{
				try
				{
					return (byte[])_perfDataKey.GetValue(item);
				}
				catch (IOException ex)
				{
					num3 = ex.HResult;
					if (num3 <= 167)
					{
						if (num3 == 6)
						{
							goto IL_0078;
						}
						if (num3 == 21 || num3 == 167)
						{
							goto IL_007e;
						}
					}
					else if (num3 <= 258)
					{
						if (num3 == 170 || num3 == 258)
						{
							goto IL_007e;
						}
					}
					else if (num3 == 1722 || num3 == 1726)
					{
						goto IL_0078;
					}
					throw new Win32Exception(num3);
					IL_007e:
					num--;
					if (num2 == 0)
					{
						num2 = 10;
						continue;
					}
					Thread.Sleep(num2);
					num2 *= 2;
					goto end_IL_0029;
					IL_0078:
					Init();
					goto IL_007e;
					end_IL_0029:;
				}
				catch (InvalidCastException innerException)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.CounterDataCorrupt, _perfDataKey.ToString()), innerException);
				}
			}
			throw new Win32Exception(num3);
		}
	}

	private static string s_computerName;

	private PerformanceMonitor _performanceMonitor;

	private readonly string _machineName;

	private readonly string _perfLcid;

	private static ConcurrentDictionary<(string machineName, string lcidString), PerformanceCounterLib> s_libraryTable;

	private Dictionary<int, string> _nameTable;

	private readonly object _nameTableLock = new object();

	private static object s_internalSyncObject;

	internal static string ComputerName => LazyInitializer.EnsureInitialized(ref s_computerName, ref s_internalSyncObject, () => global::Interop.Kernel32.GetComputerName() ?? "");

	internal Dictionary<int, string> NameTable
	{
		get
		{
			if (_nameTable == null)
			{
				lock (_nameTableLock)
				{
					if (_nameTable == null)
					{
						_nameTable = GetStringTable(isHelp: false);
					}
				}
			}
			return _nameTable;
		}
	}

	internal PerformanceCounterLib(string machineName, string lcid)
	{
		_machineName = machineName;
		_perfLcid = lcid;
	}

	internal string GetCounterName(int index)
	{
		if (!NameTable.TryGetValue(index, out var value))
		{
			return "";
		}
		return value;
	}

	internal static PerformanceCounterLib GetPerformanceCounterLib(string machineName, CultureInfo culture)
	{
		string item = culture.Name.ToLowerInvariant();
		machineName = ((machineName.CompareTo(".") != 0) ? machineName.ToLowerInvariant() : ComputerName.ToLowerInvariant());
		LazyInitializer.EnsureInitialized(ref s_libraryTable, ref s_internalSyncObject, () => new ConcurrentDictionary<(string, string), PerformanceCounterLib>());
		return s_libraryTable.GetOrAdd((machineName, item), ((string machineName, string lcidString) key) => new PerformanceCounterLib(key.machineName, key.lcidString));
	}

	internal byte[] GetPerformanceData(string item)
	{
		if (_performanceMonitor == null)
		{
			lock (LazyInitializer.EnsureInitialized(ref s_internalSyncObject))
			{
				if (_performanceMonitor == null)
				{
					_performanceMonitor = new PerformanceMonitor(_machineName);
				}
			}
		}
		return _performanceMonitor.GetData(item);
	}

	private Dictionary<int, string> GetStringTable(bool isHelp)
	{
		RegistryKey performanceData = Registry.PerformanceData;
		Dictionary<int, string> dictionary;
		try
		{
			string[] array = null;
			int num = 14;
			int num2 = 0;
			while (num > 0)
			{
				try
				{
					array = (isHelp ? ((string[])performanceData.GetValue("Explain " + _perfLcid)) : ((string[])performanceData.GetValue("Counter " + _perfLcid)));
					if (array == null || array.Length == 0)
					{
						num--;
						if (num2 == 0)
						{
							num2 = 10;
							continue;
						}
						Thread.Sleep(num2);
						num2 *= 2;
						continue;
					}
				}
				catch (IOException)
				{
					array = null;
				}
				catch (InvalidCastException)
				{
					array = null;
				}
				break;
			}
			if (array == null)
			{
				dictionary = new Dictionary<int, string>();
			}
			else
			{
				dictionary = new Dictionary<int, string>(array.Length / 2);
				for (int i = 0; i < array.Length / 2; i++)
				{
					string text = array[i * 2 + 1];
					if (text == null)
					{
						text = string.Empty;
					}
					if (!int.TryParse(array[i * 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
					{
						if (isHelp)
						{
							throw new InvalidOperationException(System.SR.Format(System.SR.CategoryHelpCorrupt, array[i * 2]));
						}
						throw new InvalidOperationException(System.SR.Format(System.SR.CounterNameCorrupt, array[i * 2]));
					}
					dictionary[result] = text;
				}
			}
		}
		finally
		{
			performanceData.Dispose();
		}
		return dictionary;
	}
}
