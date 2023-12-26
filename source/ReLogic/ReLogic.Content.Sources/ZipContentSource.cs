using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ionic.Zip;

namespace ReLogic.Content.Sources;

public class ZipContentSource : ContentSource, IDisposable
{
	private readonly ZipFile _zipFile;

	private readonly Dictionary<string, ZipEntry> _entries = new Dictionary<string, ZipEntry>();

	private readonly string _basePath;

	private bool _isDisposed;

	public int EntryCount => _entries.Count;

	public ZipContentSource(string path)
		: this(path, "")
	{
	}

	public ZipContentSource(string path, string contentDir)
	{
		_zipFile = ZipFile.Read(path);
		if (ZipPathContainsInvalidCharacters(contentDir))
		{
			throw new ArgumentException("Content directory cannot contain \"..\"", "contentDir");
		}
		_basePath = CleanZipPath(contentDir);
		BuildEntryList();
	}

	public ZipContentSource(ZipFile zip, string contentDir)
	{
		_zipFile = zip;
		if (ZipPathContainsInvalidCharacters(contentDir))
		{
			throw new ArgumentException("Content directory cannot contain \"..\"", "contentDir");
		}
		_basePath = CleanZipPath(contentDir);
		BuildEntryList();
	}

	public override Stream OpenStream(string assetName)
	{
		if (!_entries.TryGetValue(assetName, out var value))
		{
			throw AssetLoadException.FromMissingAsset(assetName);
		}
		MemoryStream memoryStream = new MemoryStream((int)value.UncompressedSize);
		lock (_zipFile)
		{
			value.Extract((Stream)memoryStream);
		}
		memoryStream.Position = 0L;
		return memoryStream;
	}

	private void BuildEntryList()
	{
		_entries.Clear();
		foreach (ZipEntry item in _zipFile.Entries.Where((ZipEntry entry) => !entry.IsDirectory && entry.FileName.StartsWith(_basePath)))
		{
			string fileName = item.FileName;
			string path = fileName.Substring(_basePath.Length, fileName.Length - _basePath.Length);
			path = AssetPathHelper.CleanPath(path);
			_entries[path] = item;
		}
		SetAssetNames(_entries.Keys);
	}

	private static bool ZipPathContainsInvalidCharacters(string path)
	{
		if (!path.Contains("../"))
		{
			return path.Contains("..\\");
		}
		return true;
	}

	private static string CleanZipPath(string path)
	{
		path = path.Replace('\\', '/');
		path = Regex.Replace(path, "^[./]+", "");
		if (path.Length != 0 && !path.EndsWith("/"))
		{
			path += "/";
		}
		return path;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			if (disposing)
			{
				_entries.Clear();
				_zipFile.Dispose();
			}
			_isDisposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
