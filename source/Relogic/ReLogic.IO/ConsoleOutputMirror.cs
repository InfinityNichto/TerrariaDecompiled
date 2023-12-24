using System;
using System.IO;
using System.Text;

namespace ReLogic.IO;

public class ConsoleOutputMirror : IDisposable
{
	private class DoubleWriter : TextWriter
	{
		private readonly TextWriter _first;

		private readonly TextWriter _second;

		public override Encoding Encoding => _first.Encoding;

		public DoubleWriter(TextWriter first, TextWriter second)
		{
			_first = first;
			_second = second;
		}

		public override void Flush()
		{
			_first.Flush();
			_second.Flush();
		}

		public override void Write(char value)
		{
			_first.Write(value);
			_second.Write(value);
		}
	}

	private static ConsoleOutputMirror _instance;

	private FileStream _fileStream;

	private StreamWriter _fileWriter;

	private TextWriter _newConsoleOutput;

	private readonly TextWriter _oldConsoleOutput;

	private bool _disposedValue;

	public static void ToFile(string path)
	{
		if (_instance != null)
		{
			_instance.Dispose();
			_instance = null;
		}
		try
		{
			_instance = new ConsoleOutputMirror(path);
		}
		catch (Exception arg)
		{
			Console.WriteLine("Unable to bind console output to file: {0}\r\nException: {1}", path, arg);
		}
	}

	private ConsoleOutputMirror(string path)
	{
		_oldConsoleOutput = Console.Out;
		Directory.CreateDirectory(Directory.GetParent(path).FullName);
		_fileStream = File.Create(path);
		_fileWriter = new StreamWriter(_fileStream)
		{
			AutoFlush = true
		};
		_newConsoleOutput = new DoubleWriter(_fileWriter, _oldConsoleOutput);
		Console.SetOut(_newConsoleOutput);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue)
		{
			return;
		}
		if (disposing)
		{
			Console.SetOut(_oldConsoleOutput);
			if (_fileWriter != null)
			{
				_fileWriter.Flush();
				_fileWriter.Close();
				_fileWriter = null;
			}
			if (_fileStream != null)
			{
				_fileStream.Close();
				_fileStream = null;
			}
		}
		_disposedValue = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
