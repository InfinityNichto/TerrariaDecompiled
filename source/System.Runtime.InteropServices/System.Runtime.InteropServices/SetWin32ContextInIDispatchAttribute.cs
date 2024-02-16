namespace System.Runtime.InteropServices;

[Obsolete("SetWin32ContextInIDispatchAttribute has been deprecated. Application Domains no longer respect Activation Context boundaries in IDispatch calls.")]
[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class SetWin32ContextInIDispatchAttribute : Attribute
{
}
