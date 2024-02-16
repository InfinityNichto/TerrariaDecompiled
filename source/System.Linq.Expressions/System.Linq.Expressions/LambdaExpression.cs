using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Linq.Expressions.Compiler;
using System.Linq.Expressions.Interpreter;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

[DebuggerTypeProxy(typeof(LambdaExpressionProxy))]
public abstract class LambdaExpression : Expression, IParameterProvider
{
	private static readonly MethodInfo s_expressionCompileMethodInfo = typeof(Expression<>).GetMethod("Compile", System.Type.EmptyTypes);

	private readonly Expression _body;

	public sealed override Type Type => TypeCore;

	internal abstract Type TypeCore { get; }

	internal abstract Type PublicType { get; }

	public sealed override ExpressionType NodeType => ExpressionType.Lambda;

	public ReadOnlyCollection<ParameterExpression> Parameters => GetOrMakeParameters();

	public string? Name => NameCore;

	internal virtual string? NameCore => null;

	public Expression Body => _body;

	public Type ReturnType => Type.GetInvokeMethod().ReturnType;

	public bool TailCall => TailCallCore;

	internal virtual bool TailCallCore => false;

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	int IParameterProvider.ParameterCount => ParameterCount;

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual int ParameterCount
	{
		get
		{
			throw ContractUtils.Unreachable;
		}
	}

	internal LambdaExpression(Expression body)
	{
		_body = body;
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual ReadOnlyCollection<ParameterExpression> GetOrMakeParameters()
	{
		throw ContractUtils.Unreachable;
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	ParameterExpression IParameterProvider.GetParameter(int index)
	{
		return GetParameter(index);
	}

	[ExcludeFromCodeCoverage(Justification = "Unreachable")]
	internal virtual ParameterExpression GetParameter(int index)
	{
		throw ContractUtils.Unreachable;
	}

	internal static MethodInfo GetCompileMethod(Type lambdaExpressionType)
	{
		if (lambdaExpressionType == typeof(LambdaExpression))
		{
			return typeof(LambdaExpression).GetMethod("Compile", System.Type.EmptyTypes);
		}
		return (MethodInfo)lambdaExpressionType.GetMemberWithSameMetadataDefinitionAs(s_expressionCompileMethodInfo);
	}

	public Delegate Compile()
	{
		return LambdaCompiler.Compile(this);
	}

	public Delegate Compile(bool preferInterpretation)
	{
		if (preferInterpretation)
		{
			return new LightCompiler().CompileTop(this).CreateDelegate();
		}
		return Compile();
	}

	internal abstract LambdaExpression Accept(StackSpiller spiller);

	public Delegate Compile(DebugInfoGenerator debugInfoGenerator)
	{
		return Compile();
	}
}
