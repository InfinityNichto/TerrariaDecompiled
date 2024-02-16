using System.Globalization;

namespace System.Data;

internal sealed class ExprException
{
	private static OverflowException _Overflow(string error)
	{
		OverflowException ex = new OverflowException(error);
		ExceptionBuilder.TraceExceptionAsReturnValue(ex);
		return ex;
	}

	private static InvalidExpressionException _Expr(string error)
	{
		InvalidExpressionException ex = new InvalidExpressionException(error);
		ExceptionBuilder.TraceExceptionAsReturnValue(ex);
		return ex;
	}

	private static SyntaxErrorException _Syntax(string error)
	{
		SyntaxErrorException ex = new SyntaxErrorException(error);
		ExceptionBuilder.TraceExceptionAsReturnValue(ex);
		return ex;
	}

	private static EvaluateException _Eval(string error)
	{
		EvaluateException ex = new EvaluateException(error);
		ExceptionBuilder.TraceExceptionAsReturnValue(ex);
		return ex;
	}

	private static EvaluateException _Eval(string error, Exception innerException)
	{
		EvaluateException ex = new EvaluateException(error);
		ExceptionBuilder.TraceExceptionAsReturnValue(ex);
		return ex;
	}

	public static Exception InvokeArgument()
	{
		return ExceptionBuilder._Argument(System.SR.Expr_InvokeArgument);
	}

	public static Exception NYI(string moreinfo)
	{
		string error = System.SR.Format(System.SR.Expr_NYI, moreinfo);
		return _Expr(error);
	}

	public static Exception MissingOperand(OperatorInfo before)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_MissingOperand, Operators.ToString(before._op)));
	}

	public static Exception MissingOperator(string token)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_MissingOperand, token));
	}

	public static Exception TypeMismatch(string expr)
	{
		return _Eval(System.SR.Format(System.SR.Expr_TypeMismatch, expr));
	}

	public static Exception FunctionArgumentOutOfRange(string arg, string func)
	{
		return ExceptionBuilder._ArgumentOutOfRange(arg, System.SR.Format(System.SR.Expr_ArgumentOutofRange, func));
	}

	public static Exception ExpressionTooComplex()
	{
		return _Eval(System.SR.Expr_ExpressionTooComplex);
	}

	public static Exception UnboundName(string name)
	{
		return _Eval(System.SR.Format(System.SR.Expr_UnboundName, name));
	}

	public static Exception InvalidString(string str)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_InvalidString, str));
	}

	public static Exception UndefinedFunction(string name)
	{
		return _Eval(System.SR.Format(System.SR.Expr_UndefinedFunction, name));
	}

	public static Exception SyntaxError()
	{
		return _Syntax(System.SR.Expr_Syntax);
	}

	public static Exception FunctionArgumentCount(string name)
	{
		return _Eval(System.SR.Format(System.SR.Expr_FunctionArgumentCount, name));
	}

	public static Exception MissingRightParen()
	{
		return _Syntax(System.SR.Expr_MissingRightParen);
	}

	public static Exception UnknownToken(string token, int position)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_UnknownToken, token, position.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception UnknownToken(Tokens tokExpected, Tokens tokCurr, int position)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_UnknownToken1, tokExpected.ToString(), tokCurr.ToString(), position.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception DatatypeConvertion(Type type1, Type type2)
	{
		return _Eval(System.SR.Format(System.SR.Expr_DatatypeConvertion, type1.ToString(), type2.ToString()));
	}

	public static Exception DatavalueConvertion(object value, Type type, Exception innerException)
	{
		return _Eval(System.SR.Format(System.SR.Expr_DatavalueConvertion, value.ToString(), type.ToString()), innerException);
	}

	public static Exception InvalidName(string name)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_InvalidName, name));
	}

	public static Exception InvalidDate(string date)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_InvalidDate, date));
	}

	public static Exception NonConstantArgument()
	{
		return _Eval(System.SR.Expr_NonConstantArgument);
	}

	public static Exception InvalidPattern(string pat)
	{
		return _Eval(System.SR.Format(System.SR.Expr_InvalidPattern, pat));
	}

	public static Exception InWithoutParentheses()
	{
		return _Syntax(System.SR.Expr_InWithoutParentheses);
	}

	public static Exception InWithoutList()
	{
		return _Syntax(System.SR.Expr_InWithoutList);
	}

	public static Exception InvalidIsSyntax()
	{
		return _Syntax(System.SR.Expr_IsSyntax);
	}

	public static Exception Overflow(Type type)
	{
		return _Overflow(System.SR.Format(System.SR.Expr_Overflow, type.Name));
	}

	public static Exception ArgumentType(string function, int arg, Type type)
	{
		return _Eval(System.SR.Format(System.SR.Expr_ArgumentType, function, arg.ToString(CultureInfo.InvariantCulture), type));
	}

	public static Exception ArgumentTypeInteger(string function, int arg)
	{
		return _Eval(System.SR.Format(System.SR.Expr_ArgumentTypeInteger, function, arg.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception TypeMismatchInBinop(int op, Type type1, Type type2)
	{
		return _Eval(System.SR.Format(System.SR.Expr_TypeMismatchInBinop, Operators.ToString(op), type1, type2));
	}

	public static Exception AmbiguousBinop(int op, Type type1, Type type2)
	{
		return _Eval(System.SR.Format(System.SR.Expr_AmbiguousBinop, Operators.ToString(op), type1, type2));
	}

	public static Exception UnsupportedOperator(int op)
	{
		return _Eval(System.SR.Format(System.SR.Expr_UnsupportedOperator, Operators.ToString(op)));
	}

	public static Exception InvalidNameBracketing(string name)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_InvalidNameBracketing, name));
	}

	public static Exception MissingOperandBefore(string op)
	{
		return _Syntax(System.SR.Format(System.SR.Expr_MissingOperandBefore, op));
	}

	public static Exception TooManyRightParentheses()
	{
		return _Syntax(System.SR.Expr_TooManyRightParentheses);
	}

	public static Exception UnresolvedRelation(string name, string expr)
	{
		return _Eval(System.SR.Format(System.SR.Expr_UnresolvedRelation, name, expr));
	}

	internal static EvaluateException BindFailure(string relationName)
	{
		return _Eval(System.SR.Format(System.SR.Expr_BindFailure, relationName));
	}

	public static Exception AggregateArgument()
	{
		return _Syntax(System.SR.Expr_AggregateArgument);
	}

	public static Exception AggregateUnbound(string expr)
	{
		return _Eval(System.SR.Format(System.SR.Expr_AggregateUnbound, expr));
	}

	public static Exception EvalNoContext()
	{
		return _Eval(System.SR.Expr_EvalNoContext);
	}

	public static Exception ExpressionUnbound(string expr)
	{
		return _Eval(System.SR.Format(System.SR.Expr_ExpressionUnbound, expr));
	}

	public static Exception ComputeNotAggregate(string expr)
	{
		return _Eval(System.SR.Format(System.SR.Expr_ComputeNotAggregate, expr));
	}

	public static Exception FilterConvertion(string expr)
	{
		return _Eval(System.SR.Format(System.SR.Expr_FilterConvertion, expr));
	}

	public static Exception LookupArgument()
	{
		return _Syntax(System.SR.Expr_LookupArgument);
	}

	public static Exception InvalidType(string typeName)
	{
		return _Eval(System.SR.Format(System.SR.Expr_InvalidType, typeName));
	}

	public static Exception InvalidHoursArgument()
	{
		return _Eval(System.SR.Expr_InvalidHoursArgument);
	}

	public static Exception InvalidMinutesArgument()
	{
		return _Eval(System.SR.Expr_InvalidMinutesArgument);
	}

	public static Exception InvalidTimeZoneRange()
	{
		return _Eval(System.SR.Expr_InvalidTimeZoneRange);
	}

	public static Exception MismatchKindandTimeSpan()
	{
		return _Eval(System.SR.Expr_MismatchKindandTimeSpan);
	}

	public static Exception UnsupportedDataType(Type type)
	{
		return ExceptionBuilder._Argument(System.SR.Format(System.SR.Expr_UnsupportedType, type.FullName));
	}
}
