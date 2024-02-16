using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class UIHintAttribute : Attribute
{
	internal sealed class UIHintImplementation
	{
		private readonly object[] _inputControlParameters;

		private IDictionary<string, object> _controlParameters;

		public string UIHint { get; }

		public string PresentationLayer { get; }

		public IDictionary<string, object> ControlParameters => _controlParameters ?? (_controlParameters = BuildControlParametersDictionary());

		public UIHintImplementation(string uiHint, string presentationLayer, params object[] controlParameters)
		{
			UIHint = uiHint;
			PresentationLayer = presentationLayer;
			if (controlParameters != null)
			{
				_inputControlParameters = new object[controlParameters.Length];
				Array.Copy(controlParameters, _inputControlParameters, controlParameters.Length);
			}
		}

		public override int GetHashCode()
		{
			string text = UIHint ?? string.Empty;
			string text2 = PresentationLayer ?? string.Empty;
			return text.GetHashCode() ^ text2.GetHashCode();
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (!(obj is UIHintImplementation uIHintImplementation) || UIHint != uIHintImplementation.UIHint || PresentationLayer != uIHintImplementation.PresentationLayer)
			{
				return false;
			}
			IDictionary<string, object> controlParameters;
			IDictionary<string, object> controlParameters2;
			try
			{
				controlParameters = ControlParameters;
				controlParameters2 = uIHintImplementation.ControlParameters;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
			if (controlParameters.Count != controlParameters2.Count)
			{
				return false;
			}
			return controlParameters.OrderBy((KeyValuePair<string, object> p) => p.Key).SequenceEqual(controlParameters2.OrderBy((KeyValuePair<string, object> p) => p.Key));
		}

		private IDictionary<string, object> BuildControlParametersDictionary()
		{
			IDictionary<string, object> dictionary = new Dictionary<string, object>();
			object[] inputControlParameters = _inputControlParameters;
			if (inputControlParameters == null || inputControlParameters.Length == 0)
			{
				return dictionary;
			}
			if (inputControlParameters.Length % 2 != 0)
			{
				throw new InvalidOperationException(System.SR.UIHintImplementation_NeedEvenNumberOfControlParameters);
			}
			for (int i = 0; i < inputControlParameters.Length; i += 2)
			{
				object obj = inputControlParameters[i];
				object value = inputControlParameters[i + 1];
				if (obj == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.UIHintImplementation_ControlParameterKeyIsNull, i));
				}
				if (!(obj is string text))
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.UIHintImplementation_ControlParameterKeyIsNotAString, i, inputControlParameters[i].ToString()));
				}
				if (dictionary.ContainsKey(text))
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.UIHintImplementation_ControlParameterKeyOccursMoreThanOnce, i, text));
				}
				dictionary[text] = value;
			}
			return dictionary;
		}
	}

	private readonly UIHintImplementation _implementation;

	public string UIHint => _implementation.UIHint;

	public string? PresentationLayer => _implementation.PresentationLayer;

	public IDictionary<string, object?> ControlParameters => _implementation.ControlParameters;

	public UIHintAttribute(string uiHint)
		: this(uiHint, null, Array.Empty<object>())
	{
	}

	public UIHintAttribute(string uiHint, string? presentationLayer)
		: this(uiHint, presentationLayer, Array.Empty<object>())
	{
	}

	public UIHintAttribute(string uiHint, string? presentationLayer, params object?[]? controlParameters)
	{
		_implementation = new UIHintImplementation(uiHint, presentationLayer, controlParameters);
	}

	public override int GetHashCode()
	{
		return _implementation.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is UIHintAttribute uIHintAttribute)
		{
			return _implementation.Equals(uIHintAttribute._implementation);
		}
		return false;
	}
}
