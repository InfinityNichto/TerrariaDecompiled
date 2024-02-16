using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.IO.Enumeration;

public class FileSystemEnumerable<TResult> : IEnumerable<TResult>, IEnumerable
{
	public delegate bool FindPredicate(ref FileSystemEntry entry);

	public delegate TResult FindTransform(ref FileSystemEntry entry);

	private sealed class DelegateEnumerator : FileSystemEnumerator<TResult>
	{
		private readonly FileSystemEnumerable<TResult> _enumerable;

		public DelegateEnumerator(FileSystemEnumerable<TResult> enumerable, bool isNormalized)
			: base(enumerable._directory, isNormalized, enumerable._options)
		{
			_enumerable = enumerable;
		}

		protected override TResult TransformEntry(ref FileSystemEntry entry)
		{
			return _enumerable._transform(ref entry);
		}

		protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
		{
			return _enumerable.ShouldRecursePredicate?.Invoke(ref entry) ?? true;
		}

		protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
		{
			return _enumerable.ShouldIncludePredicate?.Invoke(ref entry) ?? true;
		}
	}

	private DelegateEnumerator _enumerator;

	private readonly FindTransform _transform;

	private readonly EnumerationOptions _options;

	private readonly string _directory;

	public FindPredicate? ShouldIncludePredicate { get; set; }

	public FindPredicate? ShouldRecursePredicate { get; set; }

	public FileSystemEnumerable(string directory, FindTransform transform, EnumerationOptions? options = null)
		: this(directory, transform, options, isNormalized: false)
	{
	}

	internal FileSystemEnumerable(string directory, FindTransform transform, EnumerationOptions options, bool isNormalized)
	{
		_directory = directory ?? throw new ArgumentNullException("directory");
		_transform = transform ?? throw new ArgumentNullException("transform");
		_options = options ?? EnumerationOptions.Default;
		_enumerator = new DelegateEnumerator(this, isNormalized);
	}

	public IEnumerator<TResult> GetEnumerator()
	{
		return Interlocked.Exchange(ref _enumerator, null) ?? new DelegateEnumerator(this, isNormalized: false);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
