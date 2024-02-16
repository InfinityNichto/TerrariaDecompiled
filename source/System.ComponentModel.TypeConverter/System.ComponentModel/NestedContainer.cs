using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

public class NestedContainer : Container, INestedContainer, IContainer, IDisposable
{
	private sealed class Site : INestedSite, ISite, IServiceProvider
	{
		private string _name;

		public IComponent Component { get; }

		public IContainer Container { get; }

		public bool DesignMode
		{
			get
			{
				IComponent owner = ((NestedContainer)Container).Owner;
				if (owner != null && owner.Site != null)
				{
					return owner.Site.DesignMode;
				}
				return false;
			}
		}

		public string FullName
		{
			get
			{
				if (_name != null)
				{
					string ownerName = ((NestedContainer)Container).OwnerName;
					string text = _name;
					if (ownerName != null)
					{
						text = ownerName + "." + text;
					}
					return text;
				}
				return _name;
			}
		}

		public string Name
		{
			get
			{
				return _name;
			}
			[RequiresUnreferencedCode("The Type of components in the container cannot be statically discovered to validate the name.")]
			set
			{
				if (value == null || _name == null || !value.Equals(_name))
				{
					((NestedContainer)Container).ValidateName(Component, value);
					_name = value;
				}
			}
		}

		internal Site(IComponent component, NestedContainer container, string name)
		{
			Component = component;
			Container = container;
			_name = name;
		}

		public object GetService(Type service)
		{
			if (!(service == typeof(ISite)))
			{
				return ((NestedContainer)Container).GetService(service);
			}
			return this;
		}
	}

	public IComponent Owner { get; }

	protected virtual string? OwnerName
	{
		get
		{
			string result = null;
			if (Owner != null && Owner.Site != null)
			{
				result = ((!(Owner.Site is INestedSite nestedSite)) ? Owner.Site.Name : nestedSite.FullName);
			}
			return result;
		}
	}

	public NestedContainer(IComponent owner)
	{
		Owner = owner ?? throw new ArgumentNullException("owner");
		Owner.Disposed += OnOwnerDisposed;
	}

	protected override ISite CreateSite(IComponent component, string? name)
	{
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		return new Site(component, this, name);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Owner.Disposed -= OnOwnerDisposed;
		}
		base.Dispose(disposing);
	}

	protected override object? GetService(Type service)
	{
		if (service == typeof(INestedContainer))
		{
			return this;
		}
		return base.GetService(service);
	}

	private void OnOwnerDisposed(object sender, EventArgs e)
	{
		Dispose();
	}
}
