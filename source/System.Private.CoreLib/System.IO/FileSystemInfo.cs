using System.IO.Enumeration;
using System.Runtime.Serialization;

namespace System.IO;

public abstract class FileSystemInfo : MarshalByRefObject, ISerializable
{
	protected string FullPath;

	protected string OriginalPath;

	internal string _name;

	private string _linkTarget;

	private bool _linkTargetIsValid;

	private Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA _data;

	private int _dataInitialized = -1;

	public virtual string FullName => FullPath;

	public string Extension
	{
		get
		{
			int length = FullPath.Length;
			int num = length;
			while (--num >= 0)
			{
				char c = FullPath[num];
				if (c == '.')
				{
					return FullPath.Substring(num, length - num);
				}
				if (PathInternal.IsDirectorySeparator(c) || c == Path.VolumeSeparatorChar)
				{
					break;
				}
			}
			return string.Empty;
		}
	}

	public virtual string Name => _name;

	public virtual bool Exists
	{
		get
		{
			try
			{
				return ExistsCore;
			}
			catch
			{
				return false;
			}
		}
	}

	public DateTime CreationTime
	{
		get
		{
			return CreationTimeUtc.ToLocalTime();
		}
		set
		{
			CreationTimeUtc = value.ToUniversalTime();
		}
	}

	public DateTime CreationTimeUtc
	{
		get
		{
			return CreationTimeCore.UtcDateTime;
		}
		set
		{
			CreationTimeCore = File.GetUtcDateTimeOffset(value);
		}
	}

	public DateTime LastAccessTime
	{
		get
		{
			return LastAccessTimeUtc.ToLocalTime();
		}
		set
		{
			LastAccessTimeUtc = value.ToUniversalTime();
		}
	}

	public DateTime LastAccessTimeUtc
	{
		get
		{
			return LastAccessTimeCore.UtcDateTime;
		}
		set
		{
			LastAccessTimeCore = File.GetUtcDateTimeOffset(value);
		}
	}

	public DateTime LastWriteTime
	{
		get
		{
			return LastWriteTimeUtc.ToLocalTime();
		}
		set
		{
			LastWriteTimeUtc = value.ToUniversalTime();
		}
	}

	public DateTime LastWriteTimeUtc
	{
		get
		{
			return LastWriteTimeCore.UtcDateTime;
		}
		set
		{
			LastWriteTimeCore = File.GetUtcDateTimeOffset(value);
		}
	}

	public string? LinkTarget
	{
		get
		{
			if (_linkTargetIsValid)
			{
				return _linkTarget;
			}
			_linkTarget = FileSystem.GetLinkTarget(FullPath, this is DirectoryInfo);
			_linkTargetIsValid = true;
			return _linkTarget;
		}
	}

	public FileAttributes Attributes
	{
		get
		{
			EnsureDataInitialized();
			return (FileAttributes)_data.dwFileAttributes;
		}
		set
		{
			FileSystem.SetAttributes(FullPath, value);
			_dataInitialized = -1;
		}
	}

	internal bool ExistsCore
	{
		get
		{
			if (_dataInitialized == -1)
			{
				RefreshCore();
			}
			if (_dataInitialized != 0)
			{
				return false;
			}
			if (_data.dwFileAttributes != -1)
			{
				return this is DirectoryInfo == ((_data.dwFileAttributes & 0x10) == 16);
			}
			return false;
		}
	}

	internal DateTimeOffset CreationTimeCore
	{
		get
		{
			EnsureDataInitialized();
			return _data.ftCreationTime.ToDateTimeOffset();
		}
		set
		{
			FileSystem.SetCreationTime(FullPath, value, this is DirectoryInfo);
			_dataInitialized = -1;
		}
	}

	internal DateTimeOffset LastAccessTimeCore
	{
		get
		{
			EnsureDataInitialized();
			return _data.ftLastAccessTime.ToDateTimeOffset();
		}
		set
		{
			FileSystem.SetLastAccessTime(FullPath, value, this is DirectoryInfo);
			_dataInitialized = -1;
		}
	}

	internal DateTimeOffset LastWriteTimeCore
	{
		get
		{
			EnsureDataInitialized();
			return _data.ftLastWriteTime.ToDateTimeOffset();
		}
		set
		{
			FileSystem.SetLastWriteTime(FullPath, value, this is DirectoryInfo);
			_dataInitialized = -1;
		}
	}

	internal long LengthCore
	{
		get
		{
			EnsureDataInitialized();
			return (long)((ulong)_data.nFileSizeHigh << 32) | ((long)_data.nFileSizeLow & 0xFFFFFFFFL);
		}
	}

	internal string NormalizedPath
	{
		get
		{
			if (!PathInternal.EndsWithPeriodOrSpace(FullPath))
			{
				return FullPath;
			}
			return PathInternal.EnsureExtendedPrefix(FullPath);
		}
	}

	protected FileSystemInfo(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	internal void Invalidate()
	{
		_linkTargetIsValid = false;
		InvalidateCore();
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public abstract void Delete();

	public void CreateAsSymbolicLink(string pathToTarget)
	{
		FileSystem.VerifyValidPath(pathToTarget, "pathToTarget");
		FileSystem.CreateSymbolicLink(OriginalPath, pathToTarget, this is DirectoryInfo);
		Invalidate();
	}

	public FileSystemInfo? ResolveLinkTarget(bool returnFinalTarget)
	{
		return FileSystem.ResolveLinkTarget(FullPath, returnFinalTarget, this is DirectoryInfo);
	}

	public override string ToString()
	{
		return OriginalPath ?? string.Empty;
	}

	protected FileSystemInfo()
	{
	}

	internal unsafe static FileSystemInfo Create(string fullPath, ref FileSystemEntry findData)
	{
		FileSystemInfo fileSystemInfo = (findData.IsDirectory ? ((FileSystemInfo)new DirectoryInfo(fullPath, null, findData.FileName.ToString(), isNormalized: true)) : ((FileSystemInfo)new FileInfo(fullPath, null, findData.FileName.ToString(), isNormalized: true)));
		fileSystemInfo.Init(findData._info);
		return fileSystemInfo;
	}

	internal void InvalidateCore()
	{
		_dataInitialized = -1;
	}

	internal unsafe void Init(Interop.NtDll.FILE_FULL_DIR_INFORMATION* info)
	{
		_data.dwFileAttributes = (int)info->FileAttributes;
		_data.ftCreationTime = *(Interop.Kernel32.FILE_TIME*)(&info->CreationTime);
		_data.ftLastAccessTime = *(Interop.Kernel32.FILE_TIME*)(&info->LastAccessTime);
		_data.ftLastWriteTime = *(Interop.Kernel32.FILE_TIME*)(&info->LastWriteTime);
		_data.nFileSizeHigh = (uint)(info->EndOfFile >> 32);
		_data.nFileSizeLow = (uint)info->EndOfFile;
		_dataInitialized = 0;
	}

	private void EnsureDataInitialized()
	{
		if (_dataInitialized == -1)
		{
			_data = default(Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA);
			RefreshCore();
		}
		if (_dataInitialized != 0)
		{
			throw Win32Marshal.GetExceptionForWin32Error(_dataInitialized, FullPath);
		}
	}

	public void Refresh()
	{
		_linkTargetIsValid = false;
		RefreshCore();
	}

	private void RefreshCore()
	{
		_dataInitialized = FileSystem.FillAttributeInfo(FullPath, ref _data, returnErrorOnNotFound: false);
	}
}
