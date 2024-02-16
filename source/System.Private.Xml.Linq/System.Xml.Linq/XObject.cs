using System.Collections.Generic;

namespace System.Xml.Linq;

public abstract class XObject : IXmlLineInfo
{
	internal XContainer parent;

	internal object annotations;

	public string BaseUri
	{
		get
		{
			XObject xObject = this;
			while (true)
			{
				if (xObject != null && xObject.annotations == null)
				{
					xObject = xObject.parent;
					continue;
				}
				if (xObject == null)
				{
					break;
				}
				BaseUriAnnotation baseUriAnnotation = xObject.Annotation<BaseUriAnnotation>();
				if (baseUriAnnotation != null)
				{
					return baseUriAnnotation.baseUri;
				}
				xObject = xObject.parent;
			}
			return string.Empty;
		}
	}

	public XDocument? Document
	{
		get
		{
			XObject xObject = this;
			while (xObject.parent != null)
			{
				xObject = xObject.parent;
			}
			return xObject as XDocument;
		}
	}

	public abstract XmlNodeType NodeType { get; }

	public XElement? Parent => parent as XElement;

	int IXmlLineInfo.LineNumber => Annotation<LineInfoAnnotation>()?.lineNumber ?? 0;

	int IXmlLineInfo.LinePosition => Annotation<LineInfoAnnotation>()?.linePosition ?? 0;

	internal bool HasBaseUri => Annotation<BaseUriAnnotation>() != null;

	public event EventHandler<XObjectChangeEventArgs> Changed
	{
		add
		{
			if (value != null)
			{
				XObjectChangeAnnotation xObjectChangeAnnotation = Annotation<XObjectChangeAnnotation>();
				if (xObjectChangeAnnotation == null)
				{
					xObjectChangeAnnotation = new XObjectChangeAnnotation();
					AddAnnotation(xObjectChangeAnnotation);
				}
				XObjectChangeAnnotation xObjectChangeAnnotation2 = xObjectChangeAnnotation;
				xObjectChangeAnnotation2.changed = (EventHandler<XObjectChangeEventArgs>)Delegate.Combine(xObjectChangeAnnotation2.changed, value);
			}
		}
		remove
		{
			if (value == null)
			{
				return;
			}
			XObjectChangeAnnotation xObjectChangeAnnotation = Annotation<XObjectChangeAnnotation>();
			if (xObjectChangeAnnotation != null)
			{
				xObjectChangeAnnotation.changed = (EventHandler<XObjectChangeEventArgs>)Delegate.Remove(xObjectChangeAnnotation.changed, value);
				if (xObjectChangeAnnotation.changing == null && xObjectChangeAnnotation.changed == null)
				{
					RemoveAnnotations<XObjectChangeAnnotation>();
				}
			}
		}
	}

	public event EventHandler<XObjectChangeEventArgs> Changing
	{
		add
		{
			if (value != null)
			{
				XObjectChangeAnnotation xObjectChangeAnnotation = Annotation<XObjectChangeAnnotation>();
				if (xObjectChangeAnnotation == null)
				{
					xObjectChangeAnnotation = new XObjectChangeAnnotation();
					AddAnnotation(xObjectChangeAnnotation);
				}
				XObjectChangeAnnotation xObjectChangeAnnotation2 = xObjectChangeAnnotation;
				xObjectChangeAnnotation2.changing = (EventHandler<XObjectChangeEventArgs>)Delegate.Combine(xObjectChangeAnnotation2.changing, value);
			}
		}
		remove
		{
			if (value == null)
			{
				return;
			}
			XObjectChangeAnnotation xObjectChangeAnnotation = Annotation<XObjectChangeAnnotation>();
			if (xObjectChangeAnnotation != null)
			{
				xObjectChangeAnnotation.changing = (EventHandler<XObjectChangeEventArgs>)Delegate.Remove(xObjectChangeAnnotation.changing, value);
				if (xObjectChangeAnnotation.changing == null && xObjectChangeAnnotation.changed == null)
				{
					RemoveAnnotations<XObjectChangeAnnotation>();
				}
			}
		}
	}

	internal XObject()
	{
	}

	public void AddAnnotation(object annotation)
	{
		if (annotation == null)
		{
			throw new ArgumentNullException("annotation");
		}
		if (annotations == null)
		{
			annotations = ((!(annotation is object[])) ? annotation : new object[1] { annotation });
			return;
		}
		object[] array = annotations as object[];
		if (array == null)
		{
			annotations = new object[2] { annotations, annotation };
			return;
		}
		int i;
		for (i = 0; i < array.Length && array[i] != null; i++)
		{
		}
		if (i == array.Length)
		{
			Array.Resize(ref array, i * 2);
			annotations = array;
		}
		array[i] = annotation;
	}

	public object? Annotation(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (annotations != null)
		{
			if (!(annotations is object[] array))
			{
				if (XHelper.IsInstanceOfType(annotations, type))
				{
					return annotations;
				}
			}
			else
			{
				foreach (object obj in array)
				{
					if (obj == null)
					{
						break;
					}
					if (XHelper.IsInstanceOfType(obj, type))
					{
						return obj;
					}
				}
			}
		}
		return null;
	}

	private object AnnotationForSealedType(Type type)
	{
		if (annotations != null)
		{
			if (!(annotations is object[] array))
			{
				if (annotations.GetType() == type)
				{
					return annotations;
				}
			}
			else
			{
				foreach (object obj in array)
				{
					if (obj == null)
					{
						break;
					}
					if (obj.GetType() == type)
					{
						return obj;
					}
				}
			}
		}
		return null;
	}

	public T? Annotation<T>() where T : class
	{
		if (annotations != null)
		{
			if (!(annotations is object[] array))
			{
				return annotations as T;
			}
			foreach (object obj in array)
			{
				if (obj == null)
				{
					break;
				}
				if (obj is T result)
				{
					return result;
				}
			}
		}
		return null;
	}

	public IEnumerable<object> Annotations(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		return AnnotationsIterator(type);
	}

	private IEnumerable<object> AnnotationsIterator(Type type)
	{
		if (annotations == null)
		{
			yield break;
		}
		if (!(annotations is object[] a))
		{
			if (XHelper.IsInstanceOfType(annotations, type))
			{
				yield return annotations;
			}
			yield break;
		}
		foreach (object obj in a)
		{
			if (obj != null)
			{
				if (XHelper.IsInstanceOfType(obj, type))
				{
					yield return obj;
				}
				continue;
			}
			break;
		}
	}

	public IEnumerable<T> Annotations<T>() where T : class
	{
		if (annotations == null)
		{
			yield break;
		}
		if (!(annotations is object[] a))
		{
			if (annotations is T val)
			{
				yield return val;
			}
			yield break;
		}
		foreach (object obj in a)
		{
			if (obj != null)
			{
				if (obj is T val2)
				{
					yield return val2;
				}
				continue;
			}
			break;
		}
	}

	public void RemoveAnnotations(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (annotations == null)
		{
			return;
		}
		if (!(annotations is object[] array))
		{
			if (XHelper.IsInstanceOfType(annotations, type))
			{
				annotations = null;
			}
			return;
		}
		int i = 0;
		int num = 0;
		for (; i < array.Length; i++)
		{
			object obj = array[i];
			if (obj == null)
			{
				break;
			}
			if (!XHelper.IsInstanceOfType(obj, type))
			{
				array[num++] = obj;
			}
		}
		if (num == 0)
		{
			annotations = null;
			return;
		}
		while (num < i)
		{
			array[num++] = null;
		}
	}

	public void RemoveAnnotations<T>() where T : class
	{
		if (annotations == null)
		{
			return;
		}
		if (!(annotations is object[] array))
		{
			if (annotations is T)
			{
				annotations = null;
			}
			return;
		}
		int i = 0;
		int num = 0;
		for (; i < array.Length; i++)
		{
			object obj = array[i];
			if (obj == null)
			{
				break;
			}
			if (!(obj is T))
			{
				array[num++] = obj;
			}
		}
		if (num == 0)
		{
			annotations = null;
			return;
		}
		while (num < i)
		{
			array[num++] = null;
		}
	}

	bool IXmlLineInfo.HasLineInfo()
	{
		return Annotation<LineInfoAnnotation>() != null;
	}

	internal bool NotifyChanged(object sender, XObjectChangeEventArgs e)
	{
		bool result = false;
		XObject xObject = this;
		while (true)
		{
			if (xObject != null && xObject.annotations == null)
			{
				xObject = xObject.parent;
				continue;
			}
			if (xObject == null)
			{
				break;
			}
			XObjectChangeAnnotation xObjectChangeAnnotation = xObject.Annotation<XObjectChangeAnnotation>();
			if (xObjectChangeAnnotation != null)
			{
				result = true;
				if (xObjectChangeAnnotation.changed != null)
				{
					xObjectChangeAnnotation.changed(sender, e);
				}
			}
			xObject = xObject.parent;
		}
		return result;
	}

	internal bool NotifyChanging(object sender, XObjectChangeEventArgs e)
	{
		bool result = false;
		XObject xObject = this;
		while (true)
		{
			if (xObject != null && xObject.annotations == null)
			{
				xObject = xObject.parent;
				continue;
			}
			if (xObject == null)
			{
				break;
			}
			XObjectChangeAnnotation xObjectChangeAnnotation = xObject.Annotation<XObjectChangeAnnotation>();
			if (xObjectChangeAnnotation != null)
			{
				result = true;
				if (xObjectChangeAnnotation.changing != null)
				{
					xObjectChangeAnnotation.changing(sender, e);
				}
			}
			xObject = xObject.parent;
		}
		return result;
	}

	internal void SetBaseUri(string baseUri)
	{
		AddAnnotation(new BaseUriAnnotation(baseUri));
	}

	internal void SetLineInfo(int lineNumber, int linePosition)
	{
		AddAnnotation(new LineInfoAnnotation(lineNumber, linePosition));
	}

	internal bool SkipNotify()
	{
		XObject xObject = this;
		while (true)
		{
			if (xObject != null && xObject.annotations == null)
			{
				xObject = xObject.parent;
				continue;
			}
			if (xObject == null)
			{
				return true;
			}
			if (xObject.Annotation<XObjectChangeAnnotation>() != null)
			{
				break;
			}
			xObject = xObject.parent;
		}
		return false;
	}

	internal SaveOptions GetSaveOptionsFromAnnotations()
	{
		XObject xObject = this;
		object obj;
		while (true)
		{
			if (xObject != null && xObject.annotations == null)
			{
				xObject = xObject.parent;
				continue;
			}
			if (xObject == null)
			{
				return SaveOptions.None;
			}
			obj = xObject.AnnotationForSealedType(typeof(SaveOptions));
			if (obj != null)
			{
				break;
			}
			xObject = xObject.parent;
		}
		return (SaveOptions)obj;
	}
}
