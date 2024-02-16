using System;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal readonly ref struct PemEnumerator
{
	internal ref struct Enumerator
	{
		internal readonly ref struct PemFieldItem
		{
			private readonly ReadOnlySpan<char> _contents;

			private readonly PemFields _pemFields;

			public PemFieldItem(ReadOnlySpan<char> contents, PemFields pemFields)
			{
				_contents = contents;
				_pemFields = pemFields;
			}

			public void Deconstruct(out ReadOnlySpan<char> contents, out PemFields pemFields)
			{
				contents = _contents;
				pemFields = _pemFields;
			}
		}

		private ReadOnlySpan<char> _contents;

		private PemFields _pemFields;

		public PemFieldItem Current => new PemFieldItem(_contents, _pemFields);

		public Enumerator(ReadOnlySpan<char> contents)
		{
			_contents = contents;
			_pemFields = default(PemFields);
		}

		public bool MoveNext()
		{
			ReadOnlySpan<char> contents = _contents;
			_contents = contents[_pemFields.Location.End..];
			return PemEncoding.TryFind(_contents, out _pemFields);
		}
	}

	private readonly ReadOnlySpan<char> _contents;

	public PemEnumerator(ReadOnlySpan<char> contents)
	{
		_contents = contents;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_contents);
	}
}
