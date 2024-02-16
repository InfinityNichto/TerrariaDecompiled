namespace System.ComponentModel.Design.Serialization;

public interface IDesignerLoaderHost2 : IDesignerLoaderHost, IDesignerHost, IServiceContainer, IServiceProvider
{
	bool IgnoreErrorsDuringReload { get; set; }

	bool CanReloadWithErrors { get; set; }
}
