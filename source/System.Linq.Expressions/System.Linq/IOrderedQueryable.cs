using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

public interface IOrderedQueryable : IQueryable, IEnumerable
{
}
public interface IOrderedQueryable<out T> : IQueryable<T>, IEnumerable<T>, IEnumerable, IQueryable, IOrderedQueryable
{
}
