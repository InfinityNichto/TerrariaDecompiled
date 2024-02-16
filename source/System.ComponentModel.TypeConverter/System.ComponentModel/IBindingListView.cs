using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

public interface IBindingListView : IBindingList, IList, ICollection, IEnumerable
{
	string? Filter
	{
		get; [RequiresUnreferencedCode("Members of types used in the filter expression might be trimmed.")]
		set;
	}

	ListSortDescriptionCollection SortDescriptions { get; }

	bool SupportsAdvancedSorting { get; }

	bool SupportsFiltering { get; }

	void ApplySort(ListSortDescriptionCollection sorts);

	void RemoveFilter();
}
