using System.Collections.Generic;
using System.Globalization;

namespace System.Resources;

internal interface IResourceGroveler
{
	ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<string, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists);
}
