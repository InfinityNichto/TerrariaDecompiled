using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelMeshCollection : ReadOnlyCollection<ModelMesh>
{
	public struct Enumerator : IEnumerator<ModelMesh>, IDisposable, IEnumerator
	{
		private ModelMesh[] wrappedArray;

		private int position;

		public ModelMesh Current => wrappedArray[position];

		object IEnumerator.Current => Current;

		internal Enumerator(ModelMesh[] wrappedArray)
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

	private ModelMesh[] wrappedArray;

	public ModelMesh this[string meshName]
	{
		get
		{
			if (!TryGetValue(meshName, out var value))
			{
				throw new KeyNotFoundException();
			}
			return value;
		}
	}

	internal ModelMeshCollection(ModelMesh[] meshes)
		: base((IList<ModelMesh>)meshes)
	{
		wrappedArray = meshes;
	}

	public bool TryGetValue(string meshName, out ModelMesh value)
	{
		if (string.IsNullOrEmpty(meshName))
		{
			throw new ArgumentNullException("meshName");
		}
		int count = base.Items.Count;
		for (int i = 0; i < count; i++)
		{
			ModelMesh modelMesh = base.Items[i];
			if (string.Compare(modelMesh.Name, meshName, StringComparison.Ordinal) == 0)
			{
				value = modelMesh;
				return true;
			}
		}
		value = null;
		return false;
	}

	public new Enumerator GetEnumerator()
	{
		return new Enumerator(wrappedArray);
	}
}
