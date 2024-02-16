using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelBoneCollection : ReadOnlyCollection<ModelBone>
{
	public struct Enumerator : IEnumerator<ModelBone>, IDisposable, IEnumerator
	{
		private ModelBone[] wrappedArray;

		private int position;

		public ModelBone Current => wrappedArray[position];

		object IEnumerator.Current => Current;

		internal Enumerator(ModelBone[] wrappedArray)
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

	private ModelBone[] wrappedArray;

	public ModelBone this[string boneName]
	{
		get
		{
			if (!TryGetValue(boneName, out var value))
			{
				throw new KeyNotFoundException();
			}
			return value;
		}
	}

	internal ModelBoneCollection(ModelBone[] bones)
		: base((IList<ModelBone>)bones)
	{
		wrappedArray = bones;
	}

	public bool TryGetValue(string boneName, out ModelBone value)
	{
		if (string.IsNullOrEmpty(boneName))
		{
			throw new ArgumentNullException("boneName");
		}
		int count = base.Items.Count;
		for (int i = 0; i < count; i++)
		{
			ModelBone modelBone = base.Items[i];
			if (string.Compare(modelBone.Name, boneName, StringComparison.Ordinal) == 0)
			{
				value = modelBone;
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
