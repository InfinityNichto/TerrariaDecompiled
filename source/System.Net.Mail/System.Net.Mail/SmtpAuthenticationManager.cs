using System.Collections.Generic;

namespace System.Net.Mail;

internal static class SmtpAuthenticationManager
{
	private static readonly List<ISmtpAuthenticationModule> s_modules;

	static SmtpAuthenticationManager()
	{
		s_modules = new List<ISmtpAuthenticationModule>();
		Register(new SmtpNegotiateAuthenticationModule());
		Register(new SmtpNtlmAuthenticationModule());
		Register(new SmtpLoginAuthenticationModule());
	}

	internal static void Register(ISmtpAuthenticationModule module)
	{
		if (module == null)
		{
			throw new ArgumentNullException("module");
		}
		lock (s_modules)
		{
			s_modules.Add(module);
		}
	}

	internal static ISmtpAuthenticationModule[] GetModules()
	{
		lock (s_modules)
		{
			ISmtpAuthenticationModule[] array = new ISmtpAuthenticationModule[s_modules.Count];
			s_modules.CopyTo(0, array, 0, s_modules.Count);
			return array;
		}
	}
}
