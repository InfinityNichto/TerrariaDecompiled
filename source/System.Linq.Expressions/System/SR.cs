using System.Resources;
using FxResources.System.Linq.Expressions;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ReducibleMustOverrideReduce => GetResourceString("ReducibleMustOverrideReduce");

	internal static string MustReduceToDifferent => GetResourceString("MustReduceToDifferent");

	internal static string ReducedNotCompatible => GetResourceString("ReducedNotCompatible");

	internal static string SetterHasNoParams => GetResourceString("SetterHasNoParams");

	internal static string PropertyCannotHaveRefType => GetResourceString("PropertyCannotHaveRefType");

	internal static string IndexesOfSetGetMustMatch => GetResourceString("IndexesOfSetGetMustMatch");

	internal static string AccessorsCannotHaveVarArgs => GetResourceString("AccessorsCannotHaveVarArgs");

	internal static string AccessorsCannotHaveByRefArgs => GetResourceString("AccessorsCannotHaveByRefArgs");

	internal static string BoundsCannotBeLessThanOne => GetResourceString("BoundsCannotBeLessThanOne");

	internal static string TypeMustNotBeByRef => GetResourceString("TypeMustNotBeByRef");

	internal static string TypeMustNotBePointer => GetResourceString("TypeMustNotBePointer");

	internal static string SetterMustBeVoid => GetResourceString("SetterMustBeVoid");

	internal static string PropertyTypeMustMatchGetter => GetResourceString("PropertyTypeMustMatchGetter");

	internal static string PropertyTypeMustMatchSetter => GetResourceString("PropertyTypeMustMatchSetter");

	internal static string BothAccessorsMustBeStatic => GetResourceString("BothAccessorsMustBeStatic");

	internal static string OnlyStaticFieldsHaveNullInstance => GetResourceString("OnlyStaticFieldsHaveNullInstance");

	internal static string OnlyStaticPropertiesHaveNullInstance => GetResourceString("OnlyStaticPropertiesHaveNullInstance");

	internal static string OnlyStaticMethodsHaveNullInstance => GetResourceString("OnlyStaticMethodsHaveNullInstance");

	internal static string PropertyTypeCannotBeVoid => GetResourceString("PropertyTypeCannotBeVoid");

	internal static string InvalidUnboxType => GetResourceString("InvalidUnboxType");

	internal static string ExpressionMustBeWriteable => GetResourceString("ExpressionMustBeWriteable");

	internal static string ArgumentMustNotHaveValueType => GetResourceString("ArgumentMustNotHaveValueType");

	internal static string MustBeReducible => GetResourceString("MustBeReducible");

	internal static string AllTestValuesMustHaveSameType => GetResourceString("AllTestValuesMustHaveSameType");

	internal static string AllCaseBodiesMustHaveSameType => GetResourceString("AllCaseBodiesMustHaveSameType");

	internal static string DefaultBodyMustBeSupplied => GetResourceString("DefaultBodyMustBeSupplied");

	internal static string LabelMustBeVoidOrHaveExpression => GetResourceString("LabelMustBeVoidOrHaveExpression");

	internal static string LabelTypeMustBeVoid => GetResourceString("LabelTypeMustBeVoid");

	internal static string QuotedExpressionMustBeLambda => GetResourceString("QuotedExpressionMustBeLambda");

	internal static string VariableMustNotBeByRef => GetResourceString("VariableMustNotBeByRef");

	internal static string DuplicateVariable => GetResourceString("DuplicateVariable");

	internal static string StartEndMustBeOrdered => GetResourceString("StartEndMustBeOrdered");

	internal static string FaultCannotHaveCatchOrFinally => GetResourceString("FaultCannotHaveCatchOrFinally");

	internal static string TryMustHaveCatchFinallyOrFault => GetResourceString("TryMustHaveCatchFinallyOrFault");

	internal static string BodyOfCatchMustHaveSameTypeAsBodyOfTry => GetResourceString("BodyOfCatchMustHaveSameTypeAsBodyOfTry");

	internal static string ExtensionNodeMustOverrideProperty => GetResourceString("ExtensionNodeMustOverrideProperty");

	internal static string UserDefinedOperatorMustBeStatic => GetResourceString("UserDefinedOperatorMustBeStatic");

	internal static string UserDefinedOperatorMustNotBeVoid => GetResourceString("UserDefinedOperatorMustNotBeVoid");

	internal static string CoercionOperatorNotDefined => GetResourceString("CoercionOperatorNotDefined");

	internal static string UnaryOperatorNotDefined => GetResourceString("UnaryOperatorNotDefined");

	internal static string BinaryOperatorNotDefined => GetResourceString("BinaryOperatorNotDefined");

	internal static string ReferenceEqualityNotDefined => GetResourceString("ReferenceEqualityNotDefined");

	internal static string OperandTypesDoNotMatchParameters => GetResourceString("OperandTypesDoNotMatchParameters");

	internal static string OverloadOperatorTypeDoesNotMatchConversionType => GetResourceString("OverloadOperatorTypeDoesNotMatchConversionType");

	internal static string ConversionIsNotSupportedForArithmeticTypes => GetResourceString("ConversionIsNotSupportedForArithmeticTypes");

	internal static string ArgumentMustBeArray => GetResourceString("ArgumentMustBeArray");

	internal static string ArgumentMustBeBoolean => GetResourceString("ArgumentMustBeBoolean");

	internal static string EqualityMustReturnBoolean => GetResourceString("EqualityMustReturnBoolean");

	internal static string ArgumentMustBeFieldInfoOrPropertyInfo => GetResourceString("ArgumentMustBeFieldInfoOrPropertyInfo");

	internal static string ArgumentMustBeFieldInfoOrPropertyInfoOrMethod => GetResourceString("ArgumentMustBeFieldInfoOrPropertyInfoOrMethod");

	internal static string ArgumentMustBeInstanceMember => GetResourceString("ArgumentMustBeInstanceMember");

	internal static string ArgumentMustBeInteger => GetResourceString("ArgumentMustBeInteger");

	internal static string ArgumentMustBeArrayIndexType => GetResourceString("ArgumentMustBeArrayIndexType");

	internal static string ArgumentMustBeSingleDimensionalArrayType => GetResourceString("ArgumentMustBeSingleDimensionalArrayType");

	internal static string ArgumentTypesMustMatch => GetResourceString("ArgumentTypesMustMatch");

	internal static string CannotAutoInitializeValueTypeElementThroughProperty => GetResourceString("CannotAutoInitializeValueTypeElementThroughProperty");

	internal static string CannotAutoInitializeValueTypeMemberThroughProperty => GetResourceString("CannotAutoInitializeValueTypeMemberThroughProperty");

	internal static string IncorrectTypeForTypeAs => GetResourceString("IncorrectTypeForTypeAs");

	internal static string CoalesceUsedOnNonNullType => GetResourceString("CoalesceUsedOnNonNullType");

	internal static string ExpressionTypeCannotInitializeArrayType => GetResourceString("ExpressionTypeCannotInitializeArrayType");

	internal static string ArgumentTypeDoesNotMatchMember => GetResourceString("ArgumentTypeDoesNotMatchMember");

	internal static string ArgumentMemberNotDeclOnType => GetResourceString("ArgumentMemberNotDeclOnType");

	internal static string ExpressionTypeDoesNotMatchReturn => GetResourceString("ExpressionTypeDoesNotMatchReturn");

	internal static string ExpressionTypeDoesNotMatchAssignment => GetResourceString("ExpressionTypeDoesNotMatchAssignment");

	internal static string ExpressionTypeDoesNotMatchLabel => GetResourceString("ExpressionTypeDoesNotMatchLabel");

	internal static string ExpressionTypeNotInvocable => GetResourceString("ExpressionTypeNotInvocable");

	internal static string FieldNotDefinedForType => GetResourceString("FieldNotDefinedForType");

	internal static string InstanceFieldNotDefinedForType => GetResourceString("InstanceFieldNotDefinedForType");

	internal static string FieldInfoNotDefinedForType => GetResourceString("FieldInfoNotDefinedForType");

	internal static string IncorrectNumberOfIndexes => GetResourceString("IncorrectNumberOfIndexes");

	internal static string IncorrectNumberOfLambdaDeclarationParameters => GetResourceString("IncorrectNumberOfLambdaDeclarationParameters");

	internal static string IncorrectNumberOfMembersForGivenConstructor => GetResourceString("IncorrectNumberOfMembersForGivenConstructor");

	internal static string IncorrectNumberOfArgumentsForMembers => GetResourceString("IncorrectNumberOfArgumentsForMembers");

	internal static string LambdaTypeMustBeDerivedFromSystemDelegate => GetResourceString("LambdaTypeMustBeDerivedFromSystemDelegate");

	internal static string MemberNotFieldOrProperty => GetResourceString("MemberNotFieldOrProperty");

	internal static string MethodContainsGenericParameters => GetResourceString("MethodContainsGenericParameters");

	internal static string MethodIsGeneric => GetResourceString("MethodIsGeneric");

	internal static string MethodNotPropertyAccessor => GetResourceString("MethodNotPropertyAccessor");

	internal static string PropertyDoesNotHaveGetter => GetResourceString("PropertyDoesNotHaveGetter");

	internal static string PropertyDoesNotHaveSetter => GetResourceString("PropertyDoesNotHaveSetter");

	internal static string PropertyDoesNotHaveAccessor => GetResourceString("PropertyDoesNotHaveAccessor");

	internal static string NotAMemberOfType => GetResourceString("NotAMemberOfType");

	internal static string NotAMemberOfAnyType => GetResourceString("NotAMemberOfAnyType");

	internal static string UnsupportedExpressionType => GetResourceString("UnsupportedExpressionType");

	internal static string ParameterExpressionNotValidAsDelegate => GetResourceString("ParameterExpressionNotValidAsDelegate");

	internal static string PropertyNotDefinedForType => GetResourceString("PropertyNotDefinedForType");

	internal static string InstancePropertyNotDefinedForType => GetResourceString("InstancePropertyNotDefinedForType");

	internal static string InstancePropertyWithoutParameterNotDefinedForType => GetResourceString("InstancePropertyWithoutParameterNotDefinedForType");

	internal static string InstancePropertyWithSpecifiedParametersNotDefinedForType => GetResourceString("InstancePropertyWithSpecifiedParametersNotDefinedForType");

	internal static string InstanceAndMethodTypeMismatch => GetResourceString("InstanceAndMethodTypeMismatch");

	internal static string TypeContainsGenericParameters => GetResourceString("TypeContainsGenericParameters");

	internal static string TypeIsGeneric => GetResourceString("TypeIsGeneric");

	internal static string TypeMissingDefaultConstructor => GetResourceString("TypeMissingDefaultConstructor");

	internal static string ElementInitializerMethodNotAdd => GetResourceString("ElementInitializerMethodNotAdd");

	internal static string ElementInitializerMethodNoRefOutParam => GetResourceString("ElementInitializerMethodNoRefOutParam");

	internal static string ElementInitializerMethodWithZeroArgs => GetResourceString("ElementInitializerMethodWithZeroArgs");

	internal static string ElementInitializerMethodStatic => GetResourceString("ElementInitializerMethodStatic");

	internal static string TypeNotIEnumerable => GetResourceString("TypeNotIEnumerable");

	internal static string UnhandledBinary => GetResourceString("UnhandledBinary");

	internal static string UnhandledBinding => GetResourceString("UnhandledBinding");

	internal static string UnhandledBindingType => GetResourceString("UnhandledBindingType");

	internal static string UnhandledUnary => GetResourceString("UnhandledUnary");

	internal static string UnknownBindingType => GetResourceString("UnknownBindingType");

	internal static string UserDefinedOpMustHaveConsistentTypes => GetResourceString("UserDefinedOpMustHaveConsistentTypes");

	internal static string UserDefinedOpMustHaveValidReturnType => GetResourceString("UserDefinedOpMustHaveValidReturnType");

	internal static string LogicalOperatorMustHaveBooleanOperators => GetResourceString("LogicalOperatorMustHaveBooleanOperators");

	internal static string MethodWithArgsDoesNotExistOnType => GetResourceString("MethodWithArgsDoesNotExistOnType");

	internal static string GenericMethodWithArgsDoesNotExistOnType => GetResourceString("GenericMethodWithArgsDoesNotExistOnType");

	internal static string MethodWithMoreThanOneMatch => GetResourceString("MethodWithMoreThanOneMatch");

	internal static string PropertyWithMoreThanOneMatch => GetResourceString("PropertyWithMoreThanOneMatch");

	internal static string IncorrectNumberOfTypeArgsForFunc => GetResourceString("IncorrectNumberOfTypeArgsForFunc");

	internal static string IncorrectNumberOfTypeArgsForAction => GetResourceString("IncorrectNumberOfTypeArgsForAction");

	internal static string ArgumentCannotBeOfTypeVoid => GetResourceString("ArgumentCannotBeOfTypeVoid");

	internal static string OutOfRange => GetResourceString("OutOfRange");

	internal static string LabelTargetAlreadyDefined => GetResourceString("LabelTargetAlreadyDefined");

	internal static string LabelTargetUndefined => GetResourceString("LabelTargetUndefined");

	internal static string ControlCannotLeaveFinally => GetResourceString("ControlCannotLeaveFinally");

	internal static string ControlCannotLeaveFilterTest => GetResourceString("ControlCannotLeaveFilterTest");

	internal static string AmbiguousJump => GetResourceString("AmbiguousJump");

	internal static string ControlCannotEnterTry => GetResourceString("ControlCannotEnterTry");

	internal static string ControlCannotEnterExpression => GetResourceString("ControlCannotEnterExpression");

	internal static string NonLocalJumpWithValue => GetResourceString("NonLocalJumpWithValue");

	internal static string InvalidLvalue => GetResourceString("InvalidLvalue");

	internal static string UndefinedVariable => GetResourceString("UndefinedVariable");

	internal static string CannotCloseOverByRef => GetResourceString("CannotCloseOverByRef");

	internal static string UnexpectedVarArgsCall => GetResourceString("UnexpectedVarArgsCall");

	internal static string RethrowRequiresCatch => GetResourceString("RethrowRequiresCatch");

	internal static string TryNotAllowedInFilter => GetResourceString("TryNotAllowedInFilter");

	internal static string MustRewriteToSameNode => GetResourceString("MustRewriteToSameNode");

	internal static string MustRewriteChildToSameType => GetResourceString("MustRewriteChildToSameType");

	internal static string MustRewriteWithoutMethod => GetResourceString("MustRewriteWithoutMethod");

	internal static string InvalidNullValue => GetResourceString("InvalidNullValue");

	internal static string InvalidObjectType => GetResourceString("InvalidObjectType");

	internal static string TryNotSupportedForMethodsWithRefArgs => GetResourceString("TryNotSupportedForMethodsWithRefArgs");

	internal static string TryNotSupportedForValueTypeInstances => GetResourceString("TryNotSupportedForValueTypeInstances");

	internal static string EnumerationIsDone => GetResourceString("EnumerationIsDone");

	internal static string TestValueTypeDoesNotMatchComparisonMethodParameter => GetResourceString("TestValueTypeDoesNotMatchComparisonMethodParameter");

	internal static string SwitchValueTypeDoesNotMatchComparisonMethodParameter => GetResourceString("SwitchValueTypeDoesNotMatchComparisonMethodParameter");

	internal static string InvalidArgumentValue_ParamName => GetResourceString("InvalidArgumentValue_ParamName");

	internal static string NonEmptyCollectionRequired => GetResourceString("NonEmptyCollectionRequired");

	internal static string CollectionModifiedWhileEnumerating => GetResourceString("CollectionModifiedWhileEnumerating");

	internal static string ExpressionMustBeReadable => GetResourceString("ExpressionMustBeReadable");

	internal static string ExpressionTypeDoesNotMatchMethodParameter => GetResourceString("ExpressionTypeDoesNotMatchMethodParameter");

	internal static string ExpressionTypeDoesNotMatchParameter => GetResourceString("ExpressionTypeDoesNotMatchParameter");

	internal static string ExpressionTypeDoesNotMatchConstructorParameter => GetResourceString("ExpressionTypeDoesNotMatchConstructorParameter");

	internal static string IncorrectNumberOfMethodCallArguments => GetResourceString("IncorrectNumberOfMethodCallArguments");

	internal static string IncorrectNumberOfLambdaArguments => GetResourceString("IncorrectNumberOfLambdaArguments");

	internal static string IncorrectNumberOfConstructorArguments => GetResourceString("IncorrectNumberOfConstructorArguments");

	internal static string NonStaticConstructorRequired => GetResourceString("NonStaticConstructorRequired");

	internal static string NonAbstractConstructorRequired => GetResourceString("NonAbstractConstructorRequired");

	internal static string FirstArgumentMustBeCallSite => GetResourceString("FirstArgumentMustBeCallSite");

	internal static string NoOrInvalidRuleProduced => GetResourceString("NoOrInvalidRuleProduced");

	internal static string TypeMustBeDerivedFromSystemDelegate => GetResourceString("TypeMustBeDerivedFromSystemDelegate");

	internal static string TypeParameterIsNotDelegate => GetResourceString("TypeParameterIsNotDelegate");

	internal static string ArgumentTypeCannotBeVoid => GetResourceString("ArgumentTypeCannotBeVoid");

	internal static string ArgCntMustBeGreaterThanNameCnt => GetResourceString("ArgCntMustBeGreaterThanNameCnt");

	internal static string BinderNotCompatibleWithCallSite => GetResourceString("BinderNotCompatibleWithCallSite");

	internal static string BindingCannotBeNull => GetResourceString("BindingCannotBeNull");

	internal static string DynamicBinderResultNotAssignable => GetResourceString("DynamicBinderResultNotAssignable");

	internal static string DynamicBindingNeedsRestrictions => GetResourceString("DynamicBindingNeedsRestrictions");

	internal static string DynamicObjectResultNotAssignable => GetResourceString("DynamicObjectResultNotAssignable");

	internal static string InvalidMetaObjectCreated => GetResourceString("InvalidMetaObjectCreated");

	internal static string AmbiguousMatchInExpandoObject => GetResourceString("AmbiguousMatchInExpandoObject");

	internal static string CollectionReadOnly => GetResourceString("CollectionReadOnly");

	internal static string KeyDoesNotExistInExpando => GetResourceString("KeyDoesNotExistInExpando");

	internal static string SameKeyExistsInExpando => GetResourceString("SameKeyExistsInExpando");

	internal static string Arg_KeyNotFoundWithKey => GetResourceString("Arg_KeyNotFoundWithKey");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}

	internal static string Format(string resourceFormat, params object[] args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}
		return resourceFormat;
	}
}
