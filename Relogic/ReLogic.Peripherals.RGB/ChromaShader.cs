using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReLogic.Peripherals.RGB;

public abstract class ChromaShader
{
	public delegate void Processor(RgbDevice device, Fragment fragment, EffectDetailLevel quality, float time);

	private struct BoundProcessor
	{
		public static readonly BoundProcessor None = new BoundProcessor(null, isTransparent: false);

		public readonly Processor Processor;

		public readonly bool IsTransparent;

		public BoundProcessor(Processor processor, bool isTransparent)
		{
			Processor = processor;
			IsTransparent = isTransparent;
		}
	}

	public readonly bool TransparentAtAnyDetailLevel;

	private readonly List<BoundProcessor> _processors = new List<BoundProcessor>(2);

	protected ChromaShader()
	{
		for (int i = 0; i < _processors.Capacity; i++)
		{
			_processors.Add(BoundProcessor.None);
		}
		BindProcessors();
		for (int j = 0; j < _processors.Count; j++)
		{
			TransparentAtAnyDetailLevel |= _processors[j].Processor != null && _processors[j].IsTransparent;
		}
	}

	public virtual bool IsTransparentAt(EffectDetailLevel quality)
	{
		return _processors[(int)quality].IsTransparent;
	}

	public virtual void Update(float elapsedTime)
	{
	}

	public virtual void Process(RgbDevice device, Fragment fragment, EffectDetailLevel quality, float time)
	{
		if (_processors[(int)quality].Processor != null)
		{
			_processors[(int)quality].Processor(device, fragment, quality, time);
		}
	}

	private void BindProcessors()
	{
		MethodInfo[] methods = GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			RgbProcessorAttribute rgbProcessorAttribute = (RgbProcessorAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(RgbProcessorAttribute));
			if (rgbProcessorAttribute != null)
			{
				Processor processor = (Processor)Delegate.CreateDelegate(typeof(Processor), this, methodInfo, throwOnBindFailure: false);
				if (processor != null)
				{
					BindProcessor(processor, rgbProcessorAttribute);
				}
			}
		}
	}

	private void BindProcessor(Processor processor, RgbProcessorAttribute attribute)
	{
		foreach (EffectDetailLevel supportedDetailLevel in attribute.SupportedDetailLevels)
		{
			_processors[(int)supportedDetailLevel] = new BoundProcessor(processor, attribute.IsTransparent);
		}
	}
}
