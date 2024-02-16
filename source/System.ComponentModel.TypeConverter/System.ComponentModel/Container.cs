using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

public class Container : IContainer, IDisposable
{
	private sealed class Site : ISite, IServiceProvider
	{
		private string _name;

		public IComponent Component { get; }

		public IContainer Container { get; }

		public bool DesignMode => false;

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
					((Container)Container).ValidateName(Component, value);
					_name = value;
				}
			}
		}

		internal Site(IComponent component, Container container, string name)
		{
			Component = component;
			Container = container;
			_name = name;
		}

		public object GetService(Type service)
		{
			if (!(service == typeof(ISite)))
			{
				return ((Container)Container).GetService(service);
			}
			return this;
		}
	}

	private ISite[] _sites;

	private int _siteCount;

	private ComponentCollection _components;

	private ContainerFilterService _filter;

	private bool _checkedFilter;

	private readonly object _syncObj = new object();

	public virtual ComponentCollection Components
	{
		get
		{
			lock (_syncObj)
			{
				if (_components == null)
				{
					IComponent[] array = new IComponent[_siteCount];
					for (int i = 0; i < _siteCount; i++)
					{
						array[i] = _sites[i].Component;
					}
					_components = new ComponentCollection(array);
					if (_filter == null && _checkedFilter)
					{
						_checkedFilter = false;
					}
				}
				if (!_checkedFilter)
				{
					_filter = GetService(typeof(ContainerFilterService)) as ContainerFilterService;
					_checkedFilter = true;
				}
				if (_filter != null)
				{
					ComponentCollection componentCollection = _filter.FilterComponents(_components);
					if (componentCollection != null)
					{
						_components = componentCollection;
					}
				}
				return _components;
			}
		}
	}

	~Container()
	{
		Dispose(disposing: false);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "No name is provided.")]
	public virtual void Add(IComponent? component)
	{
		Add(component, null);
	}

	[RequiresUnreferencedCode("The Type of components in the container cannot be statically discovered to validate the name.")]
	public virtual void Add(IComponent? component, string? name)
	{
		lock (_syncObj)
		{
			if (component == null)
			{
				return;
			}
			ISite site = component.Site;
			if (site != null && site.Container == this)
			{
				return;
			}
			if (_sites == null)
			{
				_sites = new ISite[4];
			}
			else
			{
				ValidateName(component, name);
				if (_sites.Length == _siteCount)
				{
					ISite[] array = new ISite[_siteCount * 2];
					Array.Copy(_sites, array, _siteCount);
					_sites = array;
				}
			}
			site?.Container.Remove(component);
			ISite site2 = CreateSite(component, name);
			_sites[_siteCount++] = site2;
			component.Site = site2;
			_components = null;
		}
	}

	protected virtual ISite CreateSite(IComponent component, string? name)
	{
		return new Site(component, this, name);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}
		lock (_syncObj)
		{
			while (_siteCount > 0)
			{
				ISite site = _sites[--_siteCount];
				site.Component.Site = null;
				site.Component.Dispose();
			}
			_sites = null;
			_components = null;
		}
	}

	protected virtual object? GetService(Type service)
	{
		if (!(service == typeof(IContainer)))
		{
			return null;
		}
		return this;
	}

	public virtual void Remove(IComponent? component)
	{
		Remove(component, preserveSite: false);
	}

	private void Remove(IComponent component, bool preserveSite)
	{
		lock (_syncObj)
		{
			ISite site = component?.Site;
			if (site == null || site.Container != this)
			{
				return;
			}
			if (!preserveSite)
			{
				component.Site = null;
			}
			for (int i = 0; i < _siteCount; i++)
			{
				if (_sites[i] == site)
				{
					_siteCount--;
					Array.Copy(_sites, i + 1, _sites, i, _siteCount - i);
					_sites[_siteCount] = null;
					_components = null;
					break;
				}
			}
		}
	}

	protected void RemoveWithoutUnsiting(IComponent? component)
	{
		Remove(component, preserveSite: true);
	}

	[RequiresUnreferencedCode("The Type of components in the container cannot be statically discovered.")]
	protected virtual void ValidateName(IComponent component, string? name)
	{
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		if (name == null)
		{
			return;
		}
		for (int i = 0; i < Math.Min(_siteCount, _sites.Length); i++)
		{
			ISite site = _sites[i];
			if (site != null && site.Name != null && string.Equals(site.Name, name, StringComparison.OrdinalIgnoreCase) && site.Component != component)
			{
				InheritanceAttribute inheritanceAttribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(site.Component)[typeof(InheritanceAttribute)];
				if (inheritanceAttribute.InheritanceLevel != InheritanceLevel.InheritedReadOnly)
				{
					throw new ArgumentException(System.SR.Format(System.SR.DuplicateComponentName, name));
				}
			}
		}
	}
}
