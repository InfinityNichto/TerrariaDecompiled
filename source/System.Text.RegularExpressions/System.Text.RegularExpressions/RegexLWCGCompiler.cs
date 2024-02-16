using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace System.Text.RegularExpressions;

internal sealed class RegexLWCGCompiler : RegexCompiler
{
	private static readonly bool s_includePatternInName = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_TEXT_REGULAREXPRESSIONS_PATTERNINNAME") == "1";

	private static readonly Type[] s_paramTypes = new Type[1] { typeof(RegexRunner) };

	private static int s_regexCount;

	public RegexLWCGCompiler()
		: base(persistsAssembly: false)
	{
	}

	public RegexRunnerFactory FactoryInstanceFromCode(string pattern, RegexCode code, RegexOptions options, bool hasTimeout)
	{
		_code = code;
		_codes = code.Codes;
		_strings = code.Strings;
		_leadingCharClasses = code.LeadingCharClasses;
		_boyerMoorePrefix = code.BoyerMoorePrefix;
		_leadingAnchor = code.LeadingAnchor;
		_trackcount = code.TrackCount;
		_options = options;
		_hasTimeout = hasTimeout;
		uint value = (uint)Interlocked.Increment(ref s_regexCount);
		string value2 = string.Empty;
		if (s_includePatternInName)
		{
			value2 = "_" + ((pattern.Length > 100) ? pattern.AsSpan(0, 100) : ((ReadOnlySpan<char>)pattern));
		}
		DynamicMethod goMethod = DefineDynamicMethod($"Regex{value}_Go{value2}", null, typeof(CompiledRegexRunner));
		GenerateGo();
		DynamicMethod findFirstCharMethod = DefineDynamicMethod($"Regex{value}_FindFirstChar{value2}", typeof(bool), typeof(CompiledRegexRunner));
		GenerateFindFirstChar();
		return new CompiledRegexRunnerFactory(goMethod, findFirstCharMethod, _trackcount);
	}

	private DynamicMethod DefineDynamicMethod(string methname, Type returntype, Type hostType)
	{
		DynamicMethod dynamicMethod = new DynamicMethod(methname, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returntype, s_paramTypes, hostType, skipVisibility: false);
		_ilg = dynamicMethod.GetILGenerator();
		return dynamicMethod;
	}
}
