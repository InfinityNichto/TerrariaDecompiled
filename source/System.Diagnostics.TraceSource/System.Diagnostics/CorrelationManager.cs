using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.Diagnostics;

public class CorrelationManager
{
	private sealed class StackNode
	{
		internal int Count { get; }

		internal object Value { get; }

		internal StackNode Prev { get; }

		internal StackNode(object value, StackNode prev = null)
		{
			Value = value;
			Prev = prev;
			Count = ((prev == null) ? 1 : (prev.Count + 1));
		}
	}

	private sealed class AsyncLocalStackWrapper : Stack
	{
		private readonly AsyncLocal<StackNode> _stack;

		public override int Count => _stack.Value?.Count ?? 0;

		internal AsyncLocalStackWrapper(AsyncLocal<StackNode> stack)
		{
			_stack = stack;
		}

		public override void Clear()
		{
			_stack.Value = null;
		}

		public override object Clone()
		{
			return new AsyncLocalStackWrapper(_stack);
		}

		public override IEnumerator GetEnumerator()
		{
			return GetEnumerator(_stack.Value);
		}

		public override object Peek()
		{
			return _stack.Value?.Value;
		}

		public override bool Contains(object obj)
		{
			for (StackNode stackNode = _stack.Value; stackNode != null; stackNode = stackNode.Prev)
			{
				if (obj == null)
				{
					if (stackNode.Value == null)
					{
						return true;
					}
				}
				else if (obj.Equals(stackNode.Value))
				{
					return true;
				}
			}
			return false;
		}

		public override void CopyTo(Array array, int index)
		{
			for (StackNode stackNode = _stack.Value; stackNode != null; stackNode = stackNode.Prev)
			{
				array.SetValue(stackNode.Value, index++);
			}
		}

		private IEnumerator GetEnumerator(StackNode n)
		{
			while (n != null)
			{
				yield return n.Value;
				n = n.Prev;
			}
		}

		public override object Pop()
		{
			StackNode value = _stack.Value;
			if (value == null)
			{
				base.Pop();
			}
			_stack.Value = value.Prev;
			return value.Value;
		}

		public override void Push(object obj)
		{
			_stack.Value = new StackNode(obj, _stack.Value);
		}

		public override object[] ToArray()
		{
			StackNode stackNode = _stack.Value;
			if (stackNode == null)
			{
				return Array.Empty<object>();
			}
			List<object> list = new List<object>();
			do
			{
				list.Add(stackNode.Value);
				stackNode = stackNode.Prev;
			}
			while (stackNode != null);
			return list.ToArray();
		}
	}

	private readonly AsyncLocal<Guid> _activityId = new AsyncLocal<Guid>();

	private readonly AsyncLocal<StackNode> _stack = new AsyncLocal<StackNode>();

	private readonly Stack _stackWrapper;

	public Stack LogicalOperationStack => _stackWrapper;

	public Guid ActivityId
	{
		get
		{
			return _activityId.Value;
		}
		set
		{
			_activityId.Value = value;
		}
	}

	internal CorrelationManager()
	{
		_stackWrapper = new AsyncLocalStackWrapper(_stack);
	}

	public void StartLogicalOperation()
	{
		StartLogicalOperation(Guid.NewGuid());
	}

	public void StopLogicalOperation()
	{
		_stackWrapper.Pop();
	}

	public void StartLogicalOperation(object operationId)
	{
		if (operationId == null)
		{
			throw new ArgumentNullException("operationId");
		}
		_stackWrapper.Push(operationId);
	}
}
