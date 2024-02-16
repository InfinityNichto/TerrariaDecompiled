using System.Collections;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel;

internal sealed class ReflectPropertyDescriptor : PropertyDescriptor
{
	private static readonly object s_noValue = new object();

	private static readonly int s_bitDefaultValueQueried = InterlockedBitVector32.CreateMask();

	private static readonly int s_bitGetQueried = InterlockedBitVector32.CreateMask(s_bitDefaultValueQueried);

	private static readonly int s_bitSetQueried = InterlockedBitVector32.CreateMask(s_bitGetQueried);

	private static readonly int s_bitShouldSerializeQueried = InterlockedBitVector32.CreateMask(s_bitSetQueried);

	private static readonly int s_bitResetQueried = InterlockedBitVector32.CreateMask(s_bitShouldSerializeQueried);

	private static readonly int s_bitChangedQueried = InterlockedBitVector32.CreateMask(s_bitResetQueried);

	private static readonly int s_bitIPropChangedQueried = InterlockedBitVector32.CreateMask(s_bitChangedQueried);

	private static readonly int s_bitReadOnlyChecked = InterlockedBitVector32.CreateMask(s_bitIPropChangedQueried);

	private static readonly int s_bitAmbientValueQueried = InterlockedBitVector32.CreateMask(s_bitReadOnlyChecked);

	private static readonly int s_bitSetOnDemand = InterlockedBitVector32.CreateMask(s_bitAmbientValueQueried);

	private InterlockedBitVector32 _state;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private readonly Type _componentClass;

	private readonly Type _type;

	private object _defaultValue;

	private object _ambientValue;

	private PropertyInfo _propInfo;

	private MethodInfo _getMethod;

	private MethodInfo _setMethod;

	private MethodInfo _shouldSerializeMethod;

	private MethodInfo _resetMethod;

	private EventDescriptor _realChangedEvent;

	private EventDescriptor _realIPropChangedEvent;

	private readonly Type _receiverType;

	private object AmbientValue
	{
		get
		{
			if (!_state[s_bitAmbientValueQueried])
			{
				Attribute attribute = Attributes[typeof(AmbientValueAttribute)];
				if (attribute != null)
				{
					_ambientValue = ((AmbientValueAttribute)attribute).Value;
				}
				else
				{
					_ambientValue = s_noValue;
				}
				_state[s_bitAmbientValueQueried] = true;
			}
			return _ambientValue;
		}
	}

	private EventDescriptor ChangedEventValue
	{
		get
		{
			if (!_state[s_bitChangedQueried])
			{
				_realChangedEvent = TypeDescriptor.GetEvents(_componentClass)[Name + "Changed"];
				_state[s_bitChangedQueried] = true;
			}
			return _realChangedEvent;
		}
	}

	private EventDescriptor IPropChangedEventValue
	{
		get
		{
			if (!_state[s_bitIPropChangedQueried])
			{
				if (typeof(INotifyPropertyChanged).IsAssignableFrom(ComponentType))
				{
					_realIPropChangedEvent = TypeDescriptor.GetEvents(typeof(INotifyPropertyChanged))["PropertyChanged"];
				}
				_state[s_bitIPropChangedQueried] = true;
			}
			return _realIPropChangedEvent;
		}
	}

	public override Type ComponentType => _componentClass;

	private object DefaultValue
	{
		get
		{
			if (!_state[s_bitDefaultValueQueried])
			{
				Attribute attribute = Attributes[typeof(DefaultValueAttribute)];
				if (attribute != null)
				{
					object value = ((DefaultValueAttribute)attribute).Value;
					bool flag = value != null && PropertyType.IsEnum && PropertyType.GetEnumUnderlyingType() == value.GetType();
					_defaultValue = (flag ? Enum.ToObject(PropertyType, value) : value);
				}
				else
				{
					_defaultValue = s_noValue;
				}
				_state[s_bitDefaultValueQueried] = true;
			}
			return _defaultValue;
		}
	}

	private MethodInfo GetMethodValue
	{
		get
		{
			if (!_state[s_bitGetQueried])
			{
				if (_receiverType == null)
				{
					if (_propInfo == null)
					{
						BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty;
						_propInfo = _componentClass.GetProperty(Name, bindingAttr, null, PropertyType, Type.EmptyTypes, Array.Empty<ParameterModifier>());
					}
					if (_propInfo != null)
					{
						_getMethod = _propInfo.GetGetMethod(nonPublic: true);
					}
					if (_getMethod == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.ErrorMissingPropertyAccessors, _componentClass.FullName + "." + Name));
					}
				}
				else
				{
					_getMethod = MemberDescriptor.FindMethod(_componentClass, "Get" + Name, new Type[1] { _receiverType }, _type);
					if (_getMethod == null)
					{
						throw new ArgumentException(System.SR.Format(System.SR.ErrorMissingPropertyAccessors, Name));
					}
				}
				_state[s_bitGetQueried] = true;
			}
			return _getMethod;
		}
	}

	private bool IsExtender => _receiverType != null;

	public override bool IsReadOnly
	{
		get
		{
			if (!(SetMethodValue == null))
			{
				return ((ReadOnlyAttribute)Attributes[typeof(ReadOnlyAttribute)]).IsReadOnly;
			}
			return true;
		}
	}

	public override Type PropertyType => _type;

	private MethodInfo ResetMethodValue
	{
		get
		{
			if (!_state[s_bitResetQueried])
			{
				_resetMethod = MemberDescriptor.FindMethod(args: (!(_receiverType == null)) ? new Type[1] { _receiverType } : Type.EmptyTypes, componentClass: _componentClass, name: "Reset" + Name, returnType: typeof(void), publicOnly: false);
				_state[s_bitResetQueried] = true;
			}
			return _resetMethod;
		}
	}

	private MethodInfo SetMethodValue
	{
		get
		{
			if (!_state[s_bitSetQueried] && _state[s_bitSetOnDemand])
			{
				string name = _propInfo.Name;
				if (_setMethod == null)
				{
					Type baseType = _componentClass.BaseType;
					while (baseType != null && baseType != typeof(object))
					{
						BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
						PropertyInfo property = baseType.GetProperty(name, bindingAttr, null, PropertyType, Type.EmptyTypes, null);
						if (property != null)
						{
							_setMethod = property.GetSetMethod(nonPublic: false);
							if (_setMethod != null)
							{
								break;
							}
						}
						baseType = baseType.BaseType;
					}
				}
				_state[s_bitSetQueried] = true;
			}
			if (!_state[s_bitSetQueried])
			{
				if (_receiverType == null)
				{
					if (_propInfo == null)
					{
						BindingFlags bindingAttr2 = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty;
						_propInfo = _componentClass.GetProperty(Name, bindingAttr2, null, PropertyType, Type.EmptyTypes, Array.Empty<ParameterModifier>());
					}
					if (_propInfo != null)
					{
						_setMethod = _propInfo.GetSetMethod(nonPublic: true);
					}
				}
				else
				{
					_setMethod = MemberDescriptor.FindMethod(_componentClass, "Set" + Name, new Type[2] { _receiverType, _type }, typeof(void));
				}
				_state[s_bitSetQueried] = true;
			}
			return _setMethod;
		}
	}

	private MethodInfo ShouldSerializeMethodValue
	{
		get
		{
			if (!_state[s_bitShouldSerializeQueried])
			{
				_shouldSerializeMethod = MemberDescriptor.FindMethod(args: (!(_receiverType == null)) ? new Type[1] { _receiverType } : Type.EmptyTypes, componentClass: _componentClass, name: "ShouldSerialize" + Name, returnType: typeof(bool), publicOnly: false);
				_state[s_bitShouldSerializeQueried] = true;
			}
			return _shouldSerializeMethod;
		}
	}

	public override bool SupportsChangeEvents
	{
		get
		{
			if (IPropChangedEventValue == null)
			{
				return ChangedEventValue != null;
			}
			return true;
		}
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public ReflectPropertyDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentClass, string name, Type type, Attribute[] attributes)
		: base(name, attributes)
	{
		try
		{
			if (type == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.ErrorInvalidPropertyType, name));
			}
			if (componentClass == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "componentClass"));
			}
			_type = type;
			_componentClass = componentClass;
		}
		catch (Exception)
		{
			throw;
		}
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public ReflectPropertyDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentClass, string name, Type type, PropertyInfo propInfo, MethodInfo getMethod, MethodInfo setMethod, Attribute[] attrs)
		: this(componentClass, name, type, attrs)
	{
		_propInfo = propInfo;
		_getMethod = getMethod;
		_setMethod = setMethod;
		if (getMethod != null && propInfo != null && setMethod == null)
		{
			_state.DangerousSet(s_bitGetQueried | s_bitSetOnDemand, value: true);
		}
		else
		{
			_state.DangerousSet(s_bitGetQueried | s_bitSetQueried, value: true);
		}
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public ReflectPropertyDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentClass, string name, Type type, Type receiverType, MethodInfo getMethod, MethodInfo setMethod, Attribute[] attrs)
		: this(componentClass, name, type, attrs)
	{
		_receiverType = receiverType;
		_getMethod = getMethod;
		_setMethod = setMethod;
		_state.DangerousSet(s_bitGetQueried | s_bitSetQueried, value: true);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public ReflectPropertyDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentClass, PropertyDescriptor oldReflectPropertyDescriptor, Attribute[] attributes)
		: base(oldReflectPropertyDescriptor, attributes)
	{
		_componentClass = componentClass;
		_type = oldReflectPropertyDescriptor.PropertyType;
		if (componentClass == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "componentClass"));
		}
		if (!(oldReflectPropertyDescriptor is ReflectPropertyDescriptor reflectPropertyDescriptor))
		{
			return;
		}
		if (reflectPropertyDescriptor.ComponentType == componentClass)
		{
			_propInfo = reflectPropertyDescriptor._propInfo;
			_getMethod = reflectPropertyDescriptor._getMethod;
			_setMethod = reflectPropertyDescriptor._setMethod;
			_shouldSerializeMethod = reflectPropertyDescriptor._shouldSerializeMethod;
			_resetMethod = reflectPropertyDescriptor._resetMethod;
			_defaultValue = reflectPropertyDescriptor._defaultValue;
			_ambientValue = reflectPropertyDescriptor._ambientValue;
			_state = reflectPropertyDescriptor._state;
		}
		if (attributes == null)
		{
			return;
		}
		foreach (Attribute attribute in attributes)
		{
			if (attribute is DefaultValueAttribute defaultValueAttribute)
			{
				_defaultValue = defaultValueAttribute.Value;
				if (_defaultValue != null && PropertyType.IsEnum && PropertyType.GetEnumUnderlyingType() == _defaultValue.GetType())
				{
					_defaultValue = Enum.ToObject(PropertyType, _defaultValue);
				}
				_state.DangerousSet(s_bitDefaultValueQueried, value: true);
			}
			else if (attribute is AmbientValueAttribute ambientValueAttribute)
			{
				_ambientValue = ambientValueAttribute.Value;
				_state.DangerousSet(s_bitAmbientValueQueried, value: true);
			}
		}
	}

	public override void AddValueChanged(object component, EventHandler handler)
	{
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		EventDescriptor changedEventValue = ChangedEventValue;
		if (changedEventValue != null && changedEventValue.EventType.IsInstanceOfType(handler))
		{
			changedEventValue.AddEventHandler(component, handler);
			return;
		}
		if (GetValueChangedHandler(component) == null)
		{
			IPropChangedEventValue?.AddEventHandler(component, new PropertyChangedEventHandler(OnINotifyPropertyChanged));
		}
		base.AddValueChanged(component, handler);
	}

	internal bool ExtenderCanResetValue(IExtenderProvider provider, object component)
	{
		if (DefaultValue != s_noValue)
		{
			return !object.Equals(ExtenderGetValue(provider, component), _defaultValue);
		}
		MethodInfo resetMethodValue = ResetMethodValue;
		if (resetMethodValue != null)
		{
			MethodInfo shouldSerializeMethodValue = ShouldSerializeMethodValue;
			if (shouldSerializeMethodValue != null)
			{
				try
				{
					IExtenderProvider obj = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);
					return (bool)shouldSerializeMethodValue.Invoke(obj, new object[1] { component });
				}
				catch
				{
				}
			}
			return true;
		}
		return false;
	}

	internal Type ExtenderGetReceiverType()
	{
		return _receiverType;
	}

	internal Type ExtenderGetType(IExtenderProvider provider)
	{
		return PropertyType;
	}

	internal object ExtenderGetValue(IExtenderProvider provider, object component)
	{
		if (provider != null)
		{
			IExtenderProvider obj = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);
			return GetMethodValue.Invoke(obj, new object[1] { component });
		}
		return null;
	}

	internal void ExtenderResetValue(IExtenderProvider provider, object component, PropertyDescriptor notifyDesc)
	{
		if (DefaultValue != s_noValue)
		{
			ExtenderSetValue(provider, component, DefaultValue, notifyDesc);
		}
		else if (AmbientValue != s_noValue)
		{
			ExtenderSetValue(provider, component, AmbientValue, notifyDesc);
		}
		else
		{
			if (!(ResetMethodValue != null))
			{
				return;
			}
			ISite site = MemberDescriptor.GetSite(component);
			IComponentChangeService componentChangeService = null;
			object oldValue = null;
			if (site != null)
			{
				componentChangeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
			}
			if (componentChangeService != null)
			{
				oldValue = ExtenderGetValue(provider, component);
				try
				{
					componentChangeService.OnComponentChanging(component, notifyDesc);
				}
				catch (CheckoutException ex)
				{
					if (ex == CheckoutException.Canceled)
					{
						return;
					}
					throw;
				}
			}
			IExtenderProvider extenderProvider = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);
			if (ResetMethodValue != null)
			{
				ResetMethodValue.Invoke(extenderProvider, new object[1] { component });
				if (componentChangeService != null)
				{
					object newValue = ExtenderGetValue(extenderProvider, component);
					componentChangeService.OnComponentChanged(component, notifyDesc, oldValue, newValue);
				}
			}
		}
	}

	internal void ExtenderSetValue(IExtenderProvider provider, object component, object value, PropertyDescriptor notifyDesc)
	{
		if (provider == null)
		{
			return;
		}
		ISite site = MemberDescriptor.GetSite(component);
		IComponentChangeService componentChangeService = null;
		object oldValue = null;
		if (site != null)
		{
			componentChangeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
		}
		if (componentChangeService != null)
		{
			oldValue = ExtenderGetValue(provider, component);
			try
			{
				componentChangeService.OnComponentChanging(component, notifyDesc);
			}
			catch (CheckoutException ex)
			{
				if (ex == CheckoutException.Canceled)
				{
					return;
				}
				throw;
			}
		}
		IExtenderProvider obj = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);
		if (SetMethodValue != null)
		{
			SetMethodValue.Invoke(obj, new object[2] { component, value });
			componentChangeService?.OnComponentChanged(component, notifyDesc, oldValue, value);
		}
	}

	internal bool ExtenderShouldSerializeValue(IExtenderProvider provider, object component)
	{
		IExtenderProvider extenderProvider = (IExtenderProvider)GetInvocationTarget(_componentClass, provider);
		if (IsReadOnly)
		{
			if (ShouldSerializeMethodValue != null)
			{
				try
				{
					return (bool)ShouldSerializeMethodValue.Invoke(extenderProvider, new object[1] { component });
				}
				catch
				{
				}
			}
			return AttributesContainsDesignerVisibilityContent();
		}
		if (DefaultValue == s_noValue)
		{
			if (ShouldSerializeMethodValue != null)
			{
				try
				{
					return (bool)ShouldSerializeMethodValue.Invoke(extenderProvider, new object[1] { component });
				}
				catch
				{
				}
			}
			return true;
		}
		return !object.Equals(DefaultValue, ExtenderGetValue(extenderProvider, component));
	}

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields, typeof(DesignerSerializationVisibilityAttribute))]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The DynamicDependency ensures the correct members are preserved.")]
	private bool AttributesContainsDesignerVisibilityContent()
	{
		return Attributes.Contains(DesignerSerializationVisibilityAttribute.Content);
	}

	public override bool CanResetValue(object component)
	{
		if (IsExtender || IsReadOnly)
		{
			return false;
		}
		if (DefaultValue != s_noValue)
		{
			return !object.Equals(GetValue(component), DefaultValue);
		}
		if (ResetMethodValue != null)
		{
			if (ShouldSerializeMethodValue != null)
			{
				component = GetInvocationTarget(_componentClass, component);
				try
				{
					return (bool)ShouldSerializeMethodValue.Invoke(component, null);
				}
				catch
				{
				}
			}
			return true;
		}
		if (AmbientValue != s_noValue)
		{
			return ShouldSerializeValue(component);
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern", Justification = "ReflectPropertyDescriptor ctors are all marked as RequiresUnreferencedCode because PropertyType can't be annotated as 'All'.")]
	protected override void FillAttributes(IList attributes)
	{
		foreach (Attribute attribute2 in TypeDescriptor.GetAttributes(PropertyType))
		{
			attributes.Add(attribute2);
		}
		Type type = _componentClass;
		int num = 0;
		while (type != null && type != typeof(object))
		{
			num++;
			type = type.BaseType;
		}
		if (num > 0)
		{
			type = _componentClass;
			Attribute[][] array = new Attribute[num][];
			while (type != null && type != typeof(object))
			{
				MemberInfo memberInfo = null;
				BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				memberInfo = ((!IsExtender) ? ((MemberInfo)type.GetProperty(Name, bindingAttr, null, PropertyType, Type.EmptyTypes, Array.Empty<ParameterModifier>())) : ((MemberInfo)type.GetMethod("Get" + Name, bindingAttr, null, new Type[1] { _receiverType }, null)));
				if (memberInfo != null)
				{
					array[--num] = ReflectTypeDescriptionProvider.ReflectGetAttributes(memberInfo);
				}
				type = type.BaseType;
			}
			Attribute[][] array2 = array;
			foreach (Attribute[] array3 in array2)
			{
				if (array3 == null)
				{
					continue;
				}
				Attribute[] array4 = array3;
				foreach (Attribute attribute in array4)
				{
					if (!(attribute is AttributeProviderAttribute attributeProviderAttribute))
					{
						continue;
					}
					Type type2 = Type.GetType(attributeProviderAttribute.TypeName);
					if (!(type2 != null))
					{
						continue;
					}
					Attribute[] array5 = null;
					if (!string.IsNullOrEmpty(attributeProviderAttribute.PropertyName))
					{
						MemberInfo[] member = type2.GetMember(attributeProviderAttribute.PropertyName);
						if (member.Length != 0 && member[0] != null)
						{
							array5 = ReflectTypeDescriptionProvider.ReflectGetAttributes(member[0]);
						}
					}
					else
					{
						array5 = ReflectTypeDescriptionProvider.ReflectGetAttributes(type2);
					}
					if (array5 != null)
					{
						Attribute[] array6 = array5;
						foreach (Attribute value2 in array6)
						{
							attributes.Add(value2);
						}
					}
				}
			}
			Attribute[][] array7 = array;
			foreach (Attribute[] array8 in array7)
			{
				if (array8 != null)
				{
					Attribute[] array9 = array8;
					foreach (Attribute value3 in array9)
					{
						attributes.Add(value3);
					}
				}
			}
		}
		base.FillAttributes(attributes);
		if (SetMethodValue == null)
		{
			attributes.Add(ReadOnlyAttribute.Yes);
		}
	}

	public override object GetValue(object component)
	{
		if (IsExtender)
		{
			return null;
		}
		if (component != null)
		{
			component = GetInvocationTarget(_componentClass, component);
			try
			{
				return GetMethodValue.Invoke(component, null);
			}
			catch (Exception innerException)
			{
				string text = null;
				ISite site = ((component is IComponent component2) ? component2.Site : null);
				if (site != null && site.Name != null)
				{
					text = site.Name;
				}
				if (text == null)
				{
					text = component.GetType().FullName;
				}
				if (innerException is TargetInvocationException)
				{
					innerException = innerException.InnerException;
				}
				string p = innerException.Message ?? innerException.GetType().Name;
				throw new TargetInvocationException(System.SR.Format(System.SR.ErrorPropertyAccessorException, Name, text, p), innerException);
			}
		}
		return null;
	}

	internal void OnINotifyPropertyChanged(object component, PropertyChangedEventArgs e)
	{
		if (string.IsNullOrEmpty(e.PropertyName) || string.Compare(e.PropertyName, Name, ignoreCase: true, CultureInfo.InvariantCulture) == 0)
		{
			OnValueChanged(component, e);
		}
	}

	protected override void OnValueChanged(object component, EventArgs e)
	{
		if (_state[s_bitChangedQueried] && _realChangedEvent == null)
		{
			base.OnValueChanged(component, e);
		}
	}

	public override void RemoveValueChanged(object component, EventHandler handler)
	{
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		EventDescriptor changedEventValue = ChangedEventValue;
		if (changedEventValue != null && changedEventValue.EventType.IsInstanceOfType(handler))
		{
			changedEventValue.RemoveEventHandler(component, handler);
			return;
		}
		base.RemoveValueChanged(component, handler);
		if (GetValueChangedHandler(component) == null)
		{
			IPropChangedEventValue?.RemoveEventHandler(component, new PropertyChangedEventHandler(OnINotifyPropertyChanged));
		}
	}

	public override void ResetValue(object component)
	{
		object invocationTarget = GetInvocationTarget(_componentClass, component);
		if (DefaultValue != s_noValue)
		{
			SetValue(component, DefaultValue);
		}
		else if (AmbientValue != s_noValue)
		{
			SetValue(component, AmbientValue);
		}
		else
		{
			if (!(ResetMethodValue != null))
			{
				return;
			}
			ISite site = MemberDescriptor.GetSite(component);
			IComponentChangeService componentChangeService = null;
			object oldValue = null;
			if (site != null)
			{
				componentChangeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
			}
			if (componentChangeService != null)
			{
				oldValue = GetMethodValue.Invoke(invocationTarget, null);
				try
				{
					componentChangeService.OnComponentChanging(component, this);
				}
				catch (CheckoutException ex)
				{
					if (ex == CheckoutException.Canceled)
					{
						return;
					}
					throw;
				}
			}
			if (ResetMethodValue != null)
			{
				ResetMethodValue.Invoke(invocationTarget, null);
				if (componentChangeService != null)
				{
					object newValue = GetMethodValue.Invoke(invocationTarget, null);
					componentChangeService.OnComponentChanged(component, this, oldValue, newValue);
				}
			}
		}
	}

	public override void SetValue(object component, object value)
	{
		if (component == null)
		{
			return;
		}
		ISite site = MemberDescriptor.GetSite(component);
		object obj = null;
		object invocationTarget = GetInvocationTarget(_componentClass, component);
		if (IsReadOnly)
		{
			return;
		}
		IComponentChangeService componentChangeService = null;
		if (site != null)
		{
			componentChangeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
		}
		if (componentChangeService != null)
		{
			obj = GetMethodValue.Invoke(invocationTarget, null);
			try
			{
				componentChangeService.OnComponentChanging(component, this);
			}
			catch (CheckoutException ex)
			{
				if (ex == CheckoutException.Canceled)
				{
					return;
				}
				throw;
			}
		}
		try
		{
			SetMethodValue.Invoke(invocationTarget, new object[1] { value });
			OnValueChanged(invocationTarget, EventArgs.Empty);
		}
		catch (Exception ex2)
		{
			value = obj;
			if (ex2 is TargetInvocationException && ex2.InnerException != null)
			{
				throw ex2.InnerException;
			}
			throw;
		}
		finally
		{
			componentChangeService?.OnComponentChanged(component, this, obj, value);
		}
	}

	public override bool ShouldSerializeValue(object component)
	{
		component = GetInvocationTarget(_componentClass, component);
		if (IsReadOnly)
		{
			if (ShouldSerializeMethodValue != null)
			{
				try
				{
					return (bool)ShouldSerializeMethodValue.Invoke(component, null);
				}
				catch
				{
				}
			}
			return AttributesContainsDesignerVisibilityContent();
		}
		if (DefaultValue == s_noValue)
		{
			if (ShouldSerializeMethodValue != null)
			{
				try
				{
					return (bool)ShouldSerializeMethodValue.Invoke(component, null);
				}
				catch
				{
				}
			}
			return true;
		}
		return !object.Equals(DefaultValue, GetValue(component));
	}
}
