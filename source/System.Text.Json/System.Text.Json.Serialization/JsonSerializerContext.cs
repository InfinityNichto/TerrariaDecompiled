using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class JsonSerializerContext
{
	private bool? _canUseSerializationLogic;

	internal JsonSerializerOptions _options;

	public JsonSerializerOptions Options
	{
		get
		{
			if (_options == null)
			{
				_options = new JsonSerializerOptions();
				_options._context = this;
			}
			return _options;
		}
	}

	internal bool CanUseSerializationLogic
	{
		get
		{
			if (!_canUseSerializationLogic.HasValue)
			{
				if (GeneratedSerializerOptions == null)
				{
					_canUseSerializationLogic = false;
				}
				else
				{
					_canUseSerializationLogic = Options.Converters.Count == 0 && Options.Encoder == null && (Options.NumberHandling & (JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowNamedFloatingPointLiterals)) == 0 && Options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.None && !Options.IgnoreNullValues && Options.DefaultIgnoreCondition == GeneratedSerializerOptions.DefaultIgnoreCondition && Options.IgnoreReadOnlyFields == GeneratedSerializerOptions.IgnoreReadOnlyFields && Options.IgnoreReadOnlyProperties == GeneratedSerializerOptions.IgnoreReadOnlyProperties && Options.IncludeFields == GeneratedSerializerOptions.IncludeFields && Options.PropertyNamingPolicy == GeneratedSerializerOptions.PropertyNamingPolicy && Options.DictionaryKeyPolicy == GeneratedSerializerOptions.DictionaryKeyPolicy && Options.WriteIndented == GeneratedSerializerOptions.WriteIndented;
				}
			}
			return _canUseSerializationLogic.Value;
		}
	}

	protected abstract JsonSerializerOptions? GeneratedSerializerOptions { get; }

	protected JsonSerializerContext(JsonSerializerOptions? options)
	{
		if (options != null)
		{
			if (options._context != null)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonSerializerOptionsAlreadyBoundToContext();
			}
			_options = options;
			options._context = this;
		}
	}

	public abstract JsonTypeInfo? GetTypeInfo(Type type);
}
