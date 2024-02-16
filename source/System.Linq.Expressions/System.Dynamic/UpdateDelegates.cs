using System.Runtime.CompilerServices;

namespace System.Dynamic;

internal static class UpdateDelegates
{
	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute1<T0, TRet>(CallSite site, T0 arg0)
	{
		CallSite<Func<CallSite, T0, TRet>> callSite = (CallSite<Func<CallSite, T0, TRet>>)site;
		Func<CallSite, T0, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, TRet>[] rules;
		Func<CallSite, T0, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[1] { arg0 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch1<T0, TRet>(CallSite site, T0 arg0)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute2<T0, T1, TRet>(CallSite site, T0 arg0, T1 arg1)
	{
		CallSite<Func<CallSite, T0, T1, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, TRet>>)site;
		Func<CallSite, T0, T1, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, TRet>[] rules;
		Func<CallSite, T0, T1, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[2] { arg0, arg1 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch2<T0, T1, TRet>(CallSite site, T0 arg0, T1 arg1)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute3<T0, T1, T2, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
	{
		CallSite<Func<CallSite, T0, T1, T2, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, T2, TRet>>)site;
		Func<CallSite, T0, T1, T2, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, T2, TRet>[] rules;
		Func<CallSite, T0, T1, T2, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1, arg2);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, T2, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1, arg2);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[3] { arg0, arg1, arg2 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1, arg2);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch3<T0, T1, T2, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute4<T0, T1, T2, T3, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, T2, T3, TRet>>)site;
		Func<CallSite, T0, T1, T2, T3, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, T2, T3, TRet>[] rules;
		Func<CallSite, T0, T1, T2, T3, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1, arg2, arg3);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, T2, T3, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[4] { arg0, arg1, arg2, arg3 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch4<T0, T1, T2, T3, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute5<T0, T1, T2, T3, T4, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, TRet>>)site;
		Func<CallSite, T0, T1, T2, T3, T4, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, T2, T3, T4, TRet>[] rules;
		Func<CallSite, T0, T1, T2, T3, T4, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1, arg2, arg3, arg4);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, T2, T3, T4, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[5] { arg0, arg1, arg2, arg3, arg4 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch5<T0, T1, T2, T3, T4, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute6<T0, T1, T2, T3, T4, T5, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, TRet>>)site;
		Func<CallSite, T0, T1, T2, T3, T4, T5, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, T2, T3, T4, T5, TRet>[] rules;
		Func<CallSite, T0, T1, T2, T3, T4, T5, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, T2, T3, T4, T5, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[6] { arg0, arg1, arg2, arg3, arg4, arg5 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch6<T0, T1, T2, T3, T4, T5, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute7<T0, T1, T2, T3, T4, T5, T6, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet>>)site;
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet>[] rules;
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[7] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch7<T0, T1, T2, T3, T4, T5, T6, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute8<T0, T1, T2, T3, T4, T5, T6, T7, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet>>)site;
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet>[] rules;
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[8] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch8<T0, T1, T2, T3, T4, T5, T6, T7, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>>)site;
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>[] rules;
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[9] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet UpdateAndExecute10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>> callSite = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>>)site;
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>[] rules;
		Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet> func;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				func = rules[i];
				if ((object)func != target)
				{
					callSite.Target = func;
					TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return result;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			func = (callSite.Target = rules[j]);
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
					CallSiteOps.MoveRule(ruleCache, func, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		func = null;
		object[] args = new object[10] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 };
		while (true)
		{
			callSite.Target = target;
			func = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				TRet result = func(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return result;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, func);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static TRet NoMatch10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
	{
		site._match = false;
		return default(TRet);
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid1<T0>(CallSite site, T0 arg0)
	{
		CallSite<Action<CallSite, T0>> callSite = (CallSite<Action<CallSite, T0>>)site;
		Action<CallSite, T0> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0>[] rules;
		Action<CallSite, T0> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[1] { arg0 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid1<T0>(CallSite site, T0 arg0)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid2<T0, T1>(CallSite site, T0 arg0, T1 arg1)
	{
		CallSite<Action<CallSite, T0, T1>> callSite = (CallSite<Action<CallSite, T0, T1>>)site;
		Action<CallSite, T0, T1> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1>[] rules;
		Action<CallSite, T0, T1> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[2] { arg0, arg1 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid2<T0, T1>(CallSite site, T0 arg0, T1 arg1)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid3<T0, T1, T2>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
	{
		CallSite<Action<CallSite, T0, T1, T2>> callSite = (CallSite<Action<CallSite, T0, T1, T2>>)site;
		Action<CallSite, T0, T1, T2> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1, T2>[] rules;
		Action<CallSite, T0, T1, T2> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1, arg2);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1, T2>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1, arg2);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[3] { arg0, arg1, arg2 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1, arg2);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid3<T0, T1, T2>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid4<T0, T1, T2, T3>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
	{
		CallSite<Action<CallSite, T0, T1, T2, T3>> callSite = (CallSite<Action<CallSite, T0, T1, T2, T3>>)site;
		Action<CallSite, T0, T1, T2, T3> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1, T2, T3>[] rules;
		Action<CallSite, T0, T1, T2, T3> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1, arg2, arg3);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1, T2, T3>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1, arg2, arg3);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[4] { arg0, arg1, arg2, arg3 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1, arg2, arg3);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid4<T0, T1, T2, T3>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid5<T0, T1, T2, T3, T4>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		CallSite<Action<CallSite, T0, T1, T2, T3, T4>> callSite = (CallSite<Action<CallSite, T0, T1, T2, T3, T4>>)site;
		Action<CallSite, T0, T1, T2, T3, T4> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1, T2, T3, T4>[] rules;
		Action<CallSite, T0, T1, T2, T3, T4> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1, arg2, arg3, arg4);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1, T2, T3, T4>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[5] { arg0, arg1, arg2, arg3, arg4 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid5<T0, T1, T2, T3, T4>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid6<T0, T1, T2, T3, T4, T5>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
	{
		CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5>> callSite = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5>>)site;
		Action<CallSite, T0, T1, T2, T3, T4, T5> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1, T2, T3, T4, T5>[] rules;
		Action<CallSite, T0, T1, T2, T3, T4, T5> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1, arg2, arg3, arg4, arg5);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1, T2, T3, T4, T5>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[6] { arg0, arg1, arg2, arg3, arg4, arg5 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid6<T0, T1, T2, T3, T4, T5>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid7<T0, T1, T2, T3, T4, T5, T6>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
	{
		CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6>> callSite = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6>>)site;
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6>[] rules;
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1, T2, T3, T4, T5, T6>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[7] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid7<T0, T1, T2, T3, T4, T5, T6>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid8<T0, T1, T2, T3, T4, T5, T6, T7>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
	{
		CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7>> callSite = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7>>)site;
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7>[] rules;
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[8] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid8<T0, T1, T2, T3, T4, T5, T6, T7>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
	{
		CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8>> callSite = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8>>)site;
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8>[] rules;
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[9] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
	{
		site._match = false;
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void UpdateAndExecuteVoid10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
	{
		CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>> callSite = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>)site;
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> target = callSite.Target;
		site = callSite.GetMatchmaker();
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>[] rules;
		Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> action;
		if ((rules = CallSiteOps.GetRules(callSite)) != null)
		{
			for (int i = 0; i < rules.Length; i++)
			{
				action = rules[i];
				if ((object)action != target)
				{
					callSite.Target = action;
					action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
					if (CallSiteOps.GetMatch(site))
					{
						CallSiteOps.UpdateRules(callSite, i);
						callSite.ReleaseMatchmaker(site);
						return;
					}
					CallSiteOps.ClearMatch(site);
				}
			}
		}
		RuleCache<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>> ruleCache = CallSiteOps.GetRuleCache(callSite);
		rules = ruleCache.GetRules();
		for (int j = 0; j < rules.Length; j++)
		{
			action = (callSite.Target = rules[j]);
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					return;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
					CallSiteOps.MoveRule(ruleCache, action, j);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
		action = null;
		object[] args = new object[10] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 };
		while (true)
		{
			callSite.Target = target;
			action = (callSite.Target = callSite.Binder.BindCore(callSite, args));
			try
			{
				action(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
				if (CallSiteOps.GetMatch(site))
				{
					callSite.ReleaseMatchmaker(site);
					break;
				}
			}
			finally
			{
				if (CallSiteOps.GetMatch(site))
				{
					CallSiteOps.AddRule(callSite, action);
				}
			}
			CallSiteOps.ClearMatch(site);
		}
	}

	[Obsolete("pregenerated CallSite<T>.Update delegate", true)]
	internal static void NoMatchVoid10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
	{
		site._match = false;
	}
}
