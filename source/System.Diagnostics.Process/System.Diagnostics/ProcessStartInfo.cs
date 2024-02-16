using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace System.Diagnostics;

[DebuggerDisplay("FileName={FileName}, Arguments={BuildArguments()}, WorkingDirectory={WorkingDirectory}")]
public sealed class ProcessStartInfo
{
	private string _fileName;

	private string _arguments;

	private string _directory;

	private string _userName;

	private string _verb;

	private Collection<string> _argumentList;

	private ProcessWindowStyle _windowStyle;

	internal DictionaryWrapper _environmentVariables;

	private string _domain;

	public string Arguments
	{
		get
		{
			return _arguments ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_arguments = value;
		}
	}

	public Collection<string> ArgumentList => _argumentList ?? (_argumentList = new Collection<string>());

	internal bool HasArgumentList
	{
		get
		{
			if (_argumentList != null)
			{
				return _argumentList.Count != 0;
			}
			return false;
		}
	}

	public bool CreateNoWindow { get; set; }

	[Editor("System.Diagnostics.Design.StringDictionaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
	public StringDictionary EnvironmentVariables => new StringDictionaryWrapper(Environment as DictionaryWrapper);

	public IDictionary<string, string?> Environment
	{
		get
		{
			if (_environmentVariables == null)
			{
				IDictionary environmentVariables = System.Environment.GetEnvironmentVariables();
				_environmentVariables = new DictionaryWrapper(new Dictionary<string, string>(environmentVariables.Count, OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal));
				IDictionaryEnumerator enumerator = environmentVariables.GetEnumerator();
				while (enumerator.MoveNext())
				{
					DictionaryEntry entry = enumerator.Entry;
					_environmentVariables.Add((string)entry.Key, (string)entry.Value);
				}
			}
			return _environmentVariables;
		}
	}

	public bool RedirectStandardInput { get; set; }

	public bool RedirectStandardOutput { get; set; }

	public bool RedirectStandardError { get; set; }

	public Encoding? StandardInputEncoding { get; set; }

	public Encoding? StandardErrorEncoding { get; set; }

	public Encoding? StandardOutputEncoding { get; set; }

	[Editor("System.Diagnostics.Design.StartFileNameEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
	public string FileName
	{
		get
		{
			return _fileName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_fileName = value;
		}
	}

	[Editor("System.Diagnostics.Design.WorkingDirectoryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
	public string WorkingDirectory
	{
		get
		{
			return _directory ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_directory = value;
		}
	}

	public bool ErrorDialog { get; set; }

	public IntPtr ErrorDialogParentHandle { get; set; }

	public string UserName
	{
		get
		{
			return _userName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_userName = value;
		}
	}

	[DefaultValue("")]
	public string Verb
	{
		get
		{
			return _verb ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_verb = value;
		}
	}

	[DefaultValue(ProcessWindowStyle.Normal)]
	public ProcessWindowStyle WindowStyle
	{
		get
		{
			return _windowStyle;
		}
		set
		{
			if (!Enum.IsDefined(typeof(ProcessWindowStyle), value))
			{
				throw new InvalidEnumArgumentException("value", (int)value, typeof(ProcessWindowStyle));
			}
			_windowStyle = value;
		}
	}

	[SupportedOSPlatform("windows")]
	public string? PasswordInClearText { get; set; }

	[SupportedOSPlatform("windows")]
	public string Domain
	{
		get
		{
			return _domain ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_domain = value;
		}
	}

	[SupportedOSPlatform("windows")]
	public bool LoadUserProfile { get; set; }

	[CLSCompliant(false)]
	[SupportedOSPlatform("windows")]
	public SecureString? Password { get; set; }

	public string[] Verbs
	{
		get
		{
			string extension = Path.GetExtension(FileName);
			if (string.IsNullOrEmpty(extension))
			{
				return Array.Empty<string>();
			}
			using RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(extension);
			if (registryKey == null)
			{
				return Array.Empty<string>();
			}
			string text = registryKey.GetValue(string.Empty) as string;
			if (string.IsNullOrEmpty(text))
			{
				return Array.Empty<string>();
			}
			using RegistryKey registryKey2 = Registry.ClassesRoot.OpenSubKey(text + "\\shell");
			if (registryKey2 == null)
			{
				return Array.Empty<string>();
			}
			string[] subKeyNames = registryKey2.GetSubKeyNames();
			List<string> list = new List<string>();
			string[] array = subKeyNames;
			foreach (string text2 in array)
			{
				if (!string.Equals(text2, "new", StringComparison.OrdinalIgnoreCase))
				{
					list.Add(text2);
				}
			}
			return list.ToArray();
		}
	}

	public bool UseShellExecute { get; set; }

	public ProcessStartInfo()
	{
	}

	public ProcessStartInfo(string fileName)
	{
		_fileName = fileName;
	}

	public ProcessStartInfo(string fileName, string arguments)
	{
		_fileName = fileName;
		_arguments = arguments;
	}

	internal string BuildArguments()
	{
		if (HasArgumentList)
		{
			Span<char> initialBuffer = stackalloc char[256];
			System.Text.ValueStringBuilder stringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
			AppendArgumentsTo(ref stringBuilder);
			return stringBuilder.ToString();
		}
		return Arguments;
	}

	internal void AppendArgumentsTo(ref System.Text.ValueStringBuilder stringBuilder)
	{
		if (_argumentList != null && _argumentList.Count > 0)
		{
			foreach (string argument in _argumentList)
			{
				System.PasteArguments.AppendArgument(ref stringBuilder, argument);
			}
			return;
		}
		if (!string.IsNullOrEmpty(Arguments))
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(Arguments);
		}
	}
}
