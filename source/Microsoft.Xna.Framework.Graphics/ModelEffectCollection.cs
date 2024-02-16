using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelEffectCollection : ReadOnlyCollection<Effect>
{
	public struct Enumerator : IEnumerator<Effect>, IDisposable, IEnumerator
	{
		private List<Effect>.Enumerator internalEnumerator;

		public Effect Current => internalEnumerator.Current;

		object IEnumerator.Current => Current;

		internal Enumerator(List<Effect> wrappedList)
		{
			internalEnumerator = wrappedList.GetEnumerator();
		}

		public bool MoveNext()
		{
			return internalEnumerator.MoveNext();
		}

		void IEnumerator.Reset()
		{
			IEnumerator enumerator = internalEnumerator;
			enumerator.Reset();
			internalEnumerator = (List<Effect>.Enumerator)(object)enumerator;
		}

		public void Dispose()
		{
			internalEnumerator.Dispose();
		}
	}

	private List<Effect> wrappedList;

	internal ModelEffectCollection()
		: base((IList<Effect>)new List<Effect>())
	{
		wrappedList = (List<Effect>)base.Items;
	}

	internal void Add(Effect effect)
	{
		base.Items.Add(effect);
	}

	internal void Remove(Effect effect)
	{
		base.Items.Remove(effect);
	}

	public new Enumerator GetEnumerator()
	{
		return new Enumerator(wrappedList);
	}
}
