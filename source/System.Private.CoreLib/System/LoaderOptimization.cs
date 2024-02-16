namespace System;

public enum LoaderOptimization
{
	[Obsolete("LoaderOptimization.DisallowBindings has been deprecated and is not supported.")]
	DisallowBindings = 4,
	[Obsolete("LoaderOptimization.DomainMask has been deprecated and is not supported.")]
	DomainMask = 3,
	MultiDomain = 2,
	MultiDomainHost = 3,
	NotSpecified = 0,
	SingleDomain = 1
}
