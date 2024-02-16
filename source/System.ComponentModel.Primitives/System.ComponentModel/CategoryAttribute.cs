using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public class CategoryAttribute : Attribute
{
	private static volatile CategoryAttribute s_action;

	private static volatile CategoryAttribute s_appearance;

	private static volatile CategoryAttribute s_asynchronous;

	private static volatile CategoryAttribute s_behavior;

	private static volatile CategoryAttribute s_data;

	private static volatile CategoryAttribute s_design;

	private static volatile CategoryAttribute s_dragDrop;

	private static volatile CategoryAttribute s_defAttr;

	private static volatile CategoryAttribute s_focus;

	private static volatile CategoryAttribute s_format;

	private static volatile CategoryAttribute s_key;

	private static volatile CategoryAttribute s_layout;

	private static volatile CategoryAttribute s_mouse;

	private static volatile CategoryAttribute s_windowStyle;

	private bool _localized;

	private readonly object _locker = new object();

	private string _categoryValue;

	public static CategoryAttribute Action => s_action ?? (s_action = new CategoryAttribute("Action"));

	public static CategoryAttribute Appearance => s_appearance ?? (s_appearance = new CategoryAttribute("Appearance"));

	public static CategoryAttribute Asynchronous => s_asynchronous ?? (s_asynchronous = new CategoryAttribute("Asynchronous"));

	public static CategoryAttribute Behavior => s_behavior ?? (s_behavior = new CategoryAttribute("Behavior"));

	public static CategoryAttribute Data => s_data ?? (s_data = new CategoryAttribute("Data"));

	public static CategoryAttribute Default => s_defAttr ?? (s_defAttr = new CategoryAttribute());

	public static CategoryAttribute Design => s_design ?? (s_design = new CategoryAttribute("Design"));

	public static CategoryAttribute DragDrop => s_dragDrop ?? (s_dragDrop = new CategoryAttribute("DragDrop"));

	public static CategoryAttribute Focus => s_focus ?? (s_focus = new CategoryAttribute("Focus"));

	public static CategoryAttribute Format => s_format ?? (s_format = new CategoryAttribute("Format"));

	public static CategoryAttribute Key => s_key ?? (s_key = new CategoryAttribute("Key"));

	public static CategoryAttribute Layout => s_layout ?? (s_layout = new CategoryAttribute("Layout"));

	public static CategoryAttribute Mouse => s_mouse ?? (s_mouse = new CategoryAttribute("Mouse"));

	public static CategoryAttribute WindowStyle => s_windowStyle ?? (s_windowStyle = new CategoryAttribute("WindowStyle"));

	public string Category
	{
		get
		{
			if (!_localized)
			{
				lock (_locker)
				{
					string localizedString = GetLocalizedString(_categoryValue);
					if (localizedString != null)
					{
						_categoryValue = localizedString;
					}
					_localized = true;
				}
			}
			return _categoryValue;
		}
	}

	public CategoryAttribute()
		: this("Default")
	{
	}

	public CategoryAttribute(string category)
	{
		_categoryValue = category;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is CategoryAttribute categoryAttribute)
		{
			return categoryAttribute.Category == Category;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Category?.GetHashCode() ?? 0;
	}

	protected virtual string? GetLocalizedString(string value)
	{
		return value switch
		{
			"Action" => System.SR.PropertyCategoryAction, 
			"Appearance" => System.SR.PropertyCategoryAppearance, 
			"Asynchronous" => System.SR.PropertyCategoryAsynchronous, 
			"Behavior" => System.SR.PropertyCategoryBehavior, 
			"Config" => System.SR.PropertyCategoryConfig, 
			"Data" => System.SR.PropertyCategoryData, 
			"DDE" => System.SR.PropertyCategoryDDE, 
			"Default" => System.SR.PropertyCategoryDefault, 
			"Design" => System.SR.PropertyCategoryDesign, 
			"DragDrop" => System.SR.PropertyCategoryDragDrop, 
			"Focus" => System.SR.PropertyCategoryFocus, 
			"Font" => System.SR.PropertyCategoryFont, 
			"Format" => System.SR.PropertyCategoryFormat, 
			"Key" => System.SR.PropertyCategoryKey, 
			"Layout" => System.SR.PropertyCategoryLayout, 
			"List" => System.SR.PropertyCategoryList, 
			"Mouse" => System.SR.PropertyCategoryMouse, 
			"Position" => System.SR.PropertyCategoryPosition, 
			"Scale" => System.SR.PropertyCategoryScale, 
			"Text" => System.SR.PropertyCategoryText, 
			"WindowStyle" => System.SR.PropertyCategoryWindowStyle, 
			_ => null, 
		};
	}

	public override bool IsDefaultAttribute()
	{
		return Category == Default.Category;
	}
}
