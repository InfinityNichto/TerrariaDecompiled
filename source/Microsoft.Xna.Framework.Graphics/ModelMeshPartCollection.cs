using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelMeshPartCollection : ReadOnlyCollection<ModelMeshPart>
{
	public struct Enumerator : IEnumerator<ModelMeshPart>, IDisposable, IEnumerator
	{
		private ModelMeshPart[] wrappedArray;

		private int position;

		public ModelMeshPart Current => wrappedArray[position];

		object IEnumerator.Current => Current;

		internal Enumerator(ModelMeshPart[] wrappedArray)
		{
			this.wrappedArray = wrappedArray;
			position = -1;
		}

		public bool MoveNext()
		{
			position++;
			if (position >= wrappedArray.Length)
			{
				position = wrappedArray.Length;
				return false;
			}
			return true;
		}

		void IEnumerator.Reset()
		{
			position = -1;
		}

		public void Dispose()
		{
		}
	}

	private ModelMeshPart[] wrappedArray;

	internal ModelMeshPartCollection(ModelMeshPart[] parts)
		: base((IList<ModelMeshPart>)parts)
	{
		wrappedArray = parts;
	}

	public new Enumerator GetEnumerator()
	{
		return new Enumerator(wrappedArray);
	}
}
