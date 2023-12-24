using System;
using System.Collections.Generic;

namespace ReLogic.Peripherals.RGB;

internal class ShaderSelector
{
	private class ConditionalShader
	{
		public readonly ChromaShader Shader;

		public readonly ChromaCondition Condition;

		public float Opacity;

		public bool IsActive { get; private set; }

		public bool IsVisible => Opacity > 0f;

		public ConditionalShader(ChromaShader shader, ChromaCondition condition)
		{
			Shader = shader;
			Condition = condition;
			IsActive = false;
		}

		public void UpdateVisibility(float timeElapsed)
		{
			IsActive = Condition.IsActive();
			if (IsActive)
			{
				Opacity = Math.Min(1f, Opacity + timeElapsed);
			}
			else
			{
				Opacity = Math.Max(0f, Opacity - timeElapsed);
			}
		}
	}

	private class ShaderGroup
	{
		public readonly LinkedList<ConditionalShader> Shaders = new LinkedList<ConditionalShader>();

		public void Add(ChromaShader shader, ChromaCondition condition)
		{
			Shaders.AddLast(new ConditionalShader(shader, condition));
		}

		public void Remove(ChromaShader shader)
		{
			LinkedListNode<ConditionalShader> linkedListNode = Shaders.First;
			while (linkedListNode != null)
			{
				LinkedListNode<ConditionalShader>? next = linkedListNode.Next;
				if (linkedListNode.Value.Shader == shader)
				{
					Shaders.Remove(linkedListNode);
				}
				linkedListNode = next;
			}
		}

		public void UpdateVisibility(float timeElapsed)
		{
			LinkedListNode<ConditionalShader> linkedListNode = Shaders.First;
			while (linkedListNode != null)
			{
				LinkedListNode<ConditionalShader>? next = linkedListNode.Next;
				ConditionalShader value = linkedListNode.Value;
				bool isVisible = value.IsVisible;
				value.UpdateVisibility(timeElapsed);
				if (!isVisible && value.IsVisible)
				{
					Shaders.Remove(linkedListNode);
					Shaders.AddFirst(value);
				}
				linkedListNode = next;
			}
		}

		public bool UpdateShaders(float timeElapsed)
		{
			foreach (ConditionalShader shader in Shaders)
			{
				shader.Shader.Update(timeElapsed);
				if (shader.IsVisible && shader.Opacity >= 1f)
				{
					return !shader.Shader.TransparentAtAnyDetailLevel;
				}
			}
			return false;
		}

		public bool AppendOperations(EffectDetailLevel quality, List<ShaderOperation> operations)
		{
			foreach (ConditionalShader shader in Shaders)
			{
				if (shader.IsVisible)
				{
					bool flag = shader.Shader.IsTransparentAt(quality);
					ShaderBlendState blendState = new ShaderBlendState((!flag) ? BlendMode.GlobalOpacityOnly : BlendMode.PerPixelOpacity, shader.Opacity);
					operations.Add(new ShaderOperation(shader.Shader, blendState, quality));
					if (shader.Opacity >= 1f)
					{
						return !flag;
					}
				}
			}
			return false;
		}
	}

	private readonly List<ShaderGroup> _shaderGroups = new List<ShaderGroup>();

	private readonly List<ShaderOperation>[] _operationsByDetailLevel = new List<ShaderOperation>[2];

	public ShaderSelector()
	{
		for (int i = 0; i < _operationsByDetailLevel.Length; i++)
		{
			_operationsByDetailLevel[i] = new List<ShaderOperation>();
		}
		for (int j = 0; j < 11; j++)
		{
			_shaderGroups.Add(new ShaderGroup());
		}
	}

	public void Register(ChromaShader shader, ChromaCondition condition, ShaderLayer layer)
	{
		_shaderGroups[(int)layer].Add(shader, condition);
	}

	public void Unregister(ChromaShader shader)
	{
		for (int i = 0; i < 11; i++)
		{
			_shaderGroups[i].Remove(shader);
		}
	}

	public ICollection<ShaderOperation> AtDetailLevel(EffectDetailLevel quality)
	{
		return _operationsByDetailLevel[(int)quality];
	}

	public void Update(float timeElapsed)
	{
		UpdateShaderVisibility(timeElapsed);
		UpdateShaders(timeElapsed);
		BuildOperationsList();
	}

	private void UpdateShaderVisibility(float timeElapsed)
	{
		foreach (ShaderGroup shaderGroup in _shaderGroups)
		{
			shaderGroup.UpdateVisibility(timeElapsed);
		}
	}

	private void UpdateShaders(float timeElapsed)
	{
		int num = _shaderGroups.Count - 1;
		while (num >= 0 && !_shaderGroups[num].UpdateShaders(timeElapsed))
		{
			num--;
		}
	}

	private void BuildOperationsList()
	{
		for (int i = 0; i <= 1; i++)
		{
			List<ShaderOperation> list = _operationsByDetailLevel[i];
			list.Clear();
			int num = _shaderGroups.Count - 1;
			while (num >= 0 && !_shaderGroups[num].AppendOperations((EffectDetailLevel)i, list))
			{
				num--;
			}
			list.Reverse();
			if (list.Count > 0)
			{
				list[0] = list[0].WithBlendState(new ShaderBlendState(BlendMode.None));
			}
		}
	}
}
