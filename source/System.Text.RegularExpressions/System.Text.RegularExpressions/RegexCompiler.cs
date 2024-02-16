using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions;

internal abstract class RegexCompiler
{
	private sealed class BacktrackNote
	{
		internal int _codepos;

		internal int _flags;

		internal Label _label;

		public BacktrackNote(int flags, Label label, int codepos)
		{
			_codepos = codepos;
			_flags = flags;
			_label = label;
		}
	}

	private struct RentedLocalBuilder : IDisposable
	{
		private Stack<LocalBuilder> _pool;

		private LocalBuilder _local;

		internal RentedLocalBuilder(Stack<LocalBuilder> pool, LocalBuilder local)
		{
			_local = local;
			_pool = pool;
		}

		public static implicit operator LocalBuilder(RentedLocalBuilder local)
		{
			return local._local;
		}

		public void Dispose()
		{
			_pool.Push(_local);
			this = default(RentedLocalBuilder);
		}
	}

	private static readonly FieldInfo s_runtextbegField = RegexRunnerField("runtextbeg");

	private static readonly FieldInfo s_runtextendField = RegexRunnerField("runtextend");

	private static readonly FieldInfo s_runtextstartField = RegexRunnerField("runtextstart");

	private static readonly FieldInfo s_runtextposField = RegexRunnerField("runtextpos");

	private static readonly FieldInfo s_runtextField = RegexRunnerField("runtext");

	private static readonly FieldInfo s_runtrackposField = RegexRunnerField("runtrackpos");

	private static readonly FieldInfo s_runtrackField = RegexRunnerField("runtrack");

	private static readonly FieldInfo s_runstackposField = RegexRunnerField("runstackpos");

	private static readonly FieldInfo s_runstackField = RegexRunnerField("runstack");

	protected static readonly FieldInfo s_runtrackcountField = RegexRunnerField("runtrackcount");

	private static readonly MethodInfo s_doubleStackMethod = RegexRunnerMethod("DoubleStack");

	private static readonly MethodInfo s_doubleTrackMethod = RegexRunnerMethod("DoubleTrack");

	private static readonly MethodInfo s_captureMethod = RegexRunnerMethod("Capture");

	private static readonly MethodInfo s_transferCaptureMethod = RegexRunnerMethod("TransferCapture");

	private static readonly MethodInfo s_uncaptureMethod = RegexRunnerMethod("Uncapture");

	private static readonly MethodInfo s_isMatchedMethod = RegexRunnerMethod("IsMatched");

	private static readonly MethodInfo s_matchLengthMethod = RegexRunnerMethod("MatchLength");

	private static readonly MethodInfo s_matchIndexMethod = RegexRunnerMethod("MatchIndex");

	private static readonly MethodInfo s_isBoundaryMethod = RegexRunnerMethod("IsBoundary");

	private static readonly MethodInfo s_isECMABoundaryMethod = RegexRunnerMethod("IsECMABoundary");

	private static readonly MethodInfo s_crawlposMethod = RegexRunnerMethod("Crawlpos");

	private static readonly MethodInfo s_charInClassMethod = RegexRunnerMethod("CharInClass");

	private static readonly MethodInfo s_checkTimeoutMethod = RegexRunnerMethod("CheckTimeout");

	private static readonly MethodInfo s_charIsDigitMethod = typeof(char).GetMethod("IsDigit", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charIsWhiteSpaceMethod = typeof(char).GetMethod("IsWhiteSpace", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charGetUnicodeInfo = typeof(char).GetMethod("GetUnicodeCategory", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_charToLowerInvariantMethod = typeof(char).GetMethod("ToLowerInvariant", new Type[1] { typeof(char) });

	private static readonly MethodInfo s_cultureInfoGetCurrentCultureMethod = typeof(CultureInfo).GetMethod("get_CurrentCulture");

	private static readonly MethodInfo s_cultureInfoGetTextInfoMethod = typeof(CultureInfo).GetMethod("get_TextInfo");

	private static readonly MethodInfo s_spanGetItemMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Item", new Type[1] { typeof(int) });

	private static readonly MethodInfo s_spanGetLengthMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Length");

	private static readonly MethodInfo s_memoryMarshalGetReference = typeof(MemoryMarshal).GetMethod("GetReference", new Type[1] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) }).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOf = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[3]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanIndexOfAnyCharCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[4]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0),
		Type.MakeGenericMethodParameter(0)
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_spanSliceIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[1] { typeof(int) });

	private static readonly MethodInfo s_spanSliceIntIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[2]
	{
		typeof(int),
		typeof(int)
	});

	private static readonly MethodInfo s_spanStartsWith = typeof(MemoryExtensions).GetMethod("StartsWith", new Type[2]
	{
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
		typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
	}).MakeGenericMethod(typeof(char));

	private static readonly MethodInfo s_stringAsSpanMethod = typeof(MemoryExtensions).GetMethod("AsSpan", new Type[1] { typeof(string) });

	private static readonly MethodInfo s_stringAsSpanIntIntMethod = typeof(MemoryExtensions).GetMethod("AsSpan", new Type[3]
	{
		typeof(string),
		typeof(int),
		typeof(int)
	});

	private static readonly MethodInfo s_stringGetCharsMethod = typeof(string).GetMethod("get_Chars", new Type[1] { typeof(int) });

	private static readonly MethodInfo s_stringIndexOfCharInt = typeof(string).GetMethod("IndexOf", new Type[2]
	{
		typeof(char),
		typeof(int)
	});

	private static readonly MethodInfo s_textInfoToLowerMethod = typeof(TextInfo).GetMethod("ToLower", new Type[1] { typeof(char) });

	protected ILGenerator _ilg;

	private readonly bool _persistsAssembly;

	private LocalBuilder _runtextbegLocal;

	private LocalBuilder _runtextendLocal;

	private LocalBuilder _runtextposLocal;

	private LocalBuilder _runtextLocal;

	private LocalBuilder _runtrackposLocal;

	private LocalBuilder _runtrackLocal;

	private LocalBuilder _runstackposLocal;

	private LocalBuilder _runstackLocal;

	private LocalBuilder _textInfoLocal;

	private LocalBuilder _loopTimeoutCounterLocal;

	protected RegexOptions _options;

	protected RegexCode _code;

	protected int[] _codes;

	protected string[] _strings;

	protected (string CharClass, bool CaseInsensitive)[] _leadingCharClasses;

	protected RegexBoyerMoore _boyerMoorePrefix;

	protected int _leadingAnchor;

	protected bool _hasTimeout;

	private Label[] _labels;

	private BacktrackNote[] _notes;

	private int _notecount;

	protected int _trackcount;

	private Label _backtrack;

	private Stack<LocalBuilder> _int32LocalsPool;

	private Stack<LocalBuilder> _readOnlySpanCharLocalsPool;

	private int _regexopcode;

	private int _codepos;

	private int _backpos;

	private int[] _uniquenote;

	private int[] _goto;

	private bool UseToLowerInvariant
	{
		get
		{
			if (_textInfoLocal != null)
			{
				return (_options & RegexOptions.CultureInvariant) != 0;
			}
			return true;
		}
	}

	protected RegexCompiler(bool persistsAssembly)
	{
		_persistsAssembly = persistsAssembly;
	}

	private static FieldInfo RegexRunnerField(string fieldname)
	{
		return typeof(RegexRunner).GetField(fieldname, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	private static MethodInfo RegexRunnerMethod(string methname)
	{
		return typeof(RegexRunner).GetMethod(methname, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
	}

	internal static RegexRunnerFactory Compile(string pattern, RegexCode code, RegexOptions options, bool hasTimeout)
	{
		return new RegexLWCGCompiler().FactoryInstanceFromCode(pattern, code, options, hasTimeout);
	}

	private int AddBacktrackNote(int flags, Label l, int codepos)
	{
		if (_notes == null || _notecount >= _notes.Length)
		{
			BacktrackNote[] array = new BacktrackNote[(_notes == null) ? 16 : (_notes.Length * 2)];
			if (_notes != null)
			{
				Array.Copy(_notes, array, _notecount);
			}
			_notes = array;
		}
		_notes[_notecount] = new BacktrackNote(flags, l, codepos);
		return _notecount++;
	}

	private int AddTrack()
	{
		return AddTrack(128);
	}

	private int AddTrack(int flags)
	{
		return AddBacktrackNote(flags, DefineLabel(), _codepos);
	}

	private int AddGoto(int destpos)
	{
		if (_goto[destpos] == -1)
		{
			_goto[destpos] = AddBacktrackNote(0, _labels[destpos], destpos);
		}
		return _goto[destpos];
	}

	private int AddUniqueTrack(int i)
	{
		return AddUniqueTrack(i, 128);
	}

	private int AddUniqueTrack(int i, int flags)
	{
		if (_uniquenote[i] == -1)
		{
			_uniquenote[i] = AddTrack(flags);
		}
		return _uniquenote[i];
	}

	private Label DefineLabel()
	{
		return _ilg.DefineLabel();
	}

	private void MarkLabel(Label l)
	{
		_ilg.MarkLabel(l);
	}

	private int Operand(int i)
	{
		return _codes[_codepos + i + 1];
	}

	private bool IsRightToLeft()
	{
		return (_regexopcode & 0x40) != 0;
	}

	private bool IsCaseInsensitive()
	{
		return (_regexopcode & 0x200) != 0;
	}

	private int Code()
	{
		return _regexopcode & 0x3F;
	}

	protected void Ldstr(string str)
	{
		_ilg.Emit(OpCodes.Ldstr, str);
	}

	protected void Ldc(int i)
	{
		_ilg.Emit(OpCodes.Ldc_I4, i);
	}

	protected void LdcI8(long i)
	{
		_ilg.Emit(OpCodes.Ldc_I8, i);
	}

	protected void Ret()
	{
		_ilg.Emit(OpCodes.Ret);
	}

	protected void Dup()
	{
		_ilg.Emit(OpCodes.Dup);
	}

	private void RemUn()
	{
		_ilg.Emit(OpCodes.Rem_Un);
	}

	private void Ceq()
	{
		_ilg.Emit(OpCodes.Ceq);
	}

	private void CgtUn()
	{
		_ilg.Emit(OpCodes.Cgt_Un);
	}

	private void CltUn()
	{
		_ilg.Emit(OpCodes.Clt_Un);
	}

	private void Pop()
	{
		_ilg.Emit(OpCodes.Pop);
	}

	private void Add()
	{
		_ilg.Emit(OpCodes.Add);
	}

	private void Add(bool negate)
	{
		_ilg.Emit(negate ? OpCodes.Sub : OpCodes.Add);
	}

	private void Sub()
	{
		_ilg.Emit(OpCodes.Sub);
	}

	private void Sub(bool negate)
	{
		_ilg.Emit(negate ? OpCodes.Add : OpCodes.Sub);
	}

	private void Neg()
	{
		_ilg.Emit(OpCodes.Neg);
	}

	private void Mul()
	{
		_ilg.Emit(OpCodes.Mul);
	}

	private void And()
	{
		_ilg.Emit(OpCodes.And);
	}

	private void Or()
	{
		_ilg.Emit(OpCodes.Or);
	}

	private void Shl()
	{
		_ilg.Emit(OpCodes.Shl);
	}

	private void Shr()
	{
		_ilg.Emit(OpCodes.Shr);
	}

	private void Ldloc(LocalBuilder lt)
	{
		_ilg.Emit(OpCodes.Ldloc, lt);
	}

	private void Ldloca(LocalBuilder lt)
	{
		_ilg.Emit(OpCodes.Ldloca, lt);
	}

	private void LdindU2()
	{
		_ilg.Emit(OpCodes.Ldind_U2);
	}

	private void LdindI4()
	{
		_ilg.Emit(OpCodes.Ldind_I4);
	}

	private void LdindI8()
	{
		_ilg.Emit(OpCodes.Ldind_I8);
	}

	private void Unaligned(byte alignment)
	{
		_ilg.Emit(OpCodes.Unaligned, alignment);
	}

	private void Stloc(LocalBuilder lt)
	{
		_ilg.Emit(OpCodes.Stloc, lt);
	}

	protected void Ldthis()
	{
		_ilg.Emit(OpCodes.Ldarg_0);
	}

	protected void Ldthisfld(FieldInfo ft)
	{
		Ldthis();
		Ldfld(ft);
	}

	private void Mvfldloc(FieldInfo ft, LocalBuilder lt)
	{
		Ldthisfld(ft);
		Stloc(lt);
	}

	private void Mvlocfld(LocalBuilder lt, FieldInfo ft)
	{
		Ldthis();
		Ldloc(lt);
		Stfld(ft);
	}

	private void Ldfld(FieldInfo ft)
	{
		_ilg.Emit(OpCodes.Ldfld, ft);
	}

	protected void Stfld(FieldInfo ft)
	{
		_ilg.Emit(OpCodes.Stfld, ft);
	}

	protected void Callvirt(MethodInfo mt)
	{
		_ilg.Emit(OpCodes.Callvirt, mt);
	}

	protected void Call(MethodInfo mt)
	{
		_ilg.Emit(OpCodes.Call, mt);
	}

	private void BrfalseFar(Label l)
	{
		_ilg.Emit(OpCodes.Brfalse, l);
	}

	private void BrtrueFar(Label l)
	{
		_ilg.Emit(OpCodes.Brtrue, l);
	}

	private void BrFar(Label l)
	{
		_ilg.Emit(OpCodes.Br, l);
	}

	private void BleFar(Label l)
	{
		_ilg.Emit(OpCodes.Ble, l);
	}

	private void BltFar(Label l)
	{
		_ilg.Emit(OpCodes.Blt, l);
	}

	private void BltUnFar(Label l)
	{
		_ilg.Emit(OpCodes.Blt_Un, l);
	}

	private void BgeFar(Label l)
	{
		_ilg.Emit(OpCodes.Bge, l);
	}

	private void BgeUnFar(Label l)
	{
		_ilg.Emit(OpCodes.Bge_Un, l);
	}

	private void BgtFar(Label l)
	{
		_ilg.Emit(OpCodes.Bgt, l);
	}

	private void BneFar(Label l)
	{
		_ilg.Emit(OpCodes.Bne_Un, l);
	}

	private void BeqFar(Label l)
	{
		_ilg.Emit(OpCodes.Beq, l);
	}

	private void Brfalse(Label l)
	{
		_ilg.Emit(OpCodes.Brfalse_S, l);
	}

	private void Brtrue(Label l)
	{
		_ilg.Emit(OpCodes.Brtrue_S, l);
	}

	private void Br(Label l)
	{
		_ilg.Emit(OpCodes.Br_S, l);
	}

	private void Ble(Label l)
	{
		_ilg.Emit(OpCodes.Ble_S, l);
	}

	private void Blt(Label l)
	{
		_ilg.Emit(OpCodes.Blt_S, l);
	}

	private void Bge(Label l)
	{
		_ilg.Emit(OpCodes.Bge_S, l);
	}

	private void BgeUn(Label l)
	{
		_ilg.Emit(OpCodes.Bge_Un_S, l);
	}

	private void Bgt(Label l)
	{
		_ilg.Emit(OpCodes.Bgt_S, l);
	}

	private void BgtUn(Label l)
	{
		_ilg.Emit(OpCodes.Bgt_Un_S, l);
	}

	private void Bne(Label l)
	{
		_ilg.Emit(OpCodes.Bne_Un_S, l);
	}

	private void Beq(Label l)
	{
		_ilg.Emit(OpCodes.Beq_S, l);
	}

	private void Ldlen()
	{
		_ilg.Emit(OpCodes.Ldlen);
	}

	private void LdelemI4()
	{
		_ilg.Emit(OpCodes.Ldelem_I4);
	}

	private void StelemI4()
	{
		_ilg.Emit(OpCodes.Stelem_I4);
	}

	private void Switch(Label[] table)
	{
		_ilg.Emit(OpCodes.Switch, table);
	}

	private LocalBuilder DeclareInt32()
	{
		return _ilg.DeclareLocal(typeof(int));
	}

	private LocalBuilder DeclareTextInfo()
	{
		return _ilg.DeclareLocal(typeof(TextInfo));
	}

	private LocalBuilder DeclareInt32Array()
	{
		return _ilg.DeclareLocal(typeof(int[]));
	}

	private LocalBuilder DeclareString()
	{
		return _ilg.DeclareLocal(typeof(string));
	}

	private LocalBuilder DeclareReadOnlySpanChar()
	{
		return _ilg.DeclareLocal(typeof(ReadOnlySpan<char>));
	}

	private RentedLocalBuilder RentInt32Local()
	{
		LocalBuilder result;
		return new RentedLocalBuilder(_int32LocalsPool ?? (_int32LocalsPool = new Stack<LocalBuilder>()), _int32LocalsPool.TryPop(out result) ? result : DeclareInt32());
	}

	private RentedLocalBuilder RentReadOnlySpanCharLocal()
	{
		LocalBuilder result;
		return new RentedLocalBuilder(_readOnlySpanCharLocalsPool ?? (_readOnlySpanCharLocalsPool = new Stack<LocalBuilder>(1)), _readOnlySpanCharLocalsPool.TryPop(out result) ? result : DeclareReadOnlySpanChar());
	}

	private void Rightchar()
	{
		Ldloc(_runtextLocal);
		Ldloc(_runtextposLocal);
		Call(s_stringGetCharsMethod);
	}

	private void Rightcharnext()
	{
		Ldloc(_runtextLocal);
		Ldloc(_runtextposLocal);
		Call(s_stringGetCharsMethod);
		Ldloc(_runtextposLocal);
		Ldc(1);
		Add();
		Stloc(_runtextposLocal);
	}

	private void Leftchar()
	{
		Ldloc(_runtextLocal);
		Ldloc(_runtextposLocal);
		Ldc(1);
		Sub();
		Call(s_stringGetCharsMethod);
	}

	private void Leftcharnext()
	{
		Ldloc(_runtextposLocal);
		Ldc(1);
		Sub();
		Stloc(_runtextposLocal);
		Ldloc(_runtextLocal);
		Ldloc(_runtextposLocal);
		Call(s_stringGetCharsMethod);
	}

	private void Track()
	{
		ReadyPushTrack();
		Ldc(AddTrack());
		DoPush();
	}

	private void Trackagain()
	{
		ReadyPushTrack();
		Ldc(_backpos);
		DoPush();
	}

	private void PushTrack(LocalBuilder lt)
	{
		ReadyPushTrack();
		Ldloc(lt);
		DoPush();
	}

	private void TrackUnique(int i)
	{
		ReadyPushTrack();
		Ldc(AddUniqueTrack(i));
		DoPush();
	}

	private void TrackUnique2(int i)
	{
		ReadyPushTrack();
		Ldc(AddUniqueTrack(i, 256));
		DoPush();
	}

	private void ReadyPushTrack()
	{
		Ldloc(_runtrackposLocal);
		Ldc(1);
		Sub();
		Stloc(_runtrackposLocal);
		Ldloc(_runtrackLocal);
		Ldloc(_runtrackposLocal);
	}

	private void PopTrack()
	{
		Ldloc(_runtrackLocal);
		Ldloc(_runtrackposLocal);
		LdelemI4();
		using RentedLocalBuilder rentedLocalBuilder = RentInt32Local();
		Stloc(rentedLocalBuilder);
		Ldloc(_runtrackposLocal);
		Ldc(1);
		Add();
		Stloc(_runtrackposLocal);
		Ldloc(rentedLocalBuilder);
	}

	private void TopTrack()
	{
		Ldloc(_runtrackLocal);
		Ldloc(_runtrackposLocal);
		LdelemI4();
	}

	private void PushStack(LocalBuilder lt)
	{
		ReadyPushStack();
		Ldloc(lt);
		DoPush();
	}

	internal void ReadyReplaceStack(int i)
	{
		Ldloc(_runstackLocal);
		Ldloc(_runstackposLocal);
		if (i != 0)
		{
			Ldc(i);
			Add();
		}
	}

	private void ReadyPushStack()
	{
		Ldloc(_runstackposLocal);
		Ldc(1);
		Sub();
		Stloc(_runstackposLocal);
		Ldloc(_runstackLocal);
		Ldloc(_runstackposLocal);
	}

	private void TopStack()
	{
		Ldloc(_runstackLocal);
		Ldloc(_runstackposLocal);
		LdelemI4();
	}

	private void PopStack()
	{
		using RentedLocalBuilder rentedLocalBuilder = RentInt32Local();
		Ldloc(_runstackLocal);
		Ldloc(_runstackposLocal);
		LdelemI4();
		Stloc(rentedLocalBuilder);
		Ldloc(_runstackposLocal);
		Ldc(1);
		Add();
		Stloc(_runstackposLocal);
		Ldloc(rentedLocalBuilder);
	}

	private void PopDiscardStack()
	{
		PopDiscardStack(1);
	}

	private void PopDiscardStack(int i)
	{
		Ldloc(_runstackposLocal);
		Ldc(i);
		Add();
		Stloc(_runstackposLocal);
	}

	private void DoReplace()
	{
		StelemI4();
	}

	private void DoPush()
	{
		StelemI4();
	}

	private void Back()
	{
		BrFar(_backtrack);
	}

	private void Goto(int i)
	{
		if (i < _codepos)
		{
			Label l = DefineLabel();
			Ldloc(_runtrackposLocal);
			Ldc(_trackcount * 4);
			Ble(l);
			Ldloc(_runstackposLocal);
			Ldc(_trackcount * 3);
			BgtFar(_labels[i]);
			MarkLabel(l);
			ReadyPushTrack();
			Ldc(AddGoto(i));
			DoPush();
			BrFar(_backtrack);
		}
		else
		{
			BrFar(_labels[i]);
		}
	}

	private int NextCodepos()
	{
		return _codepos + RegexCode.OpcodeSize(_codes[_codepos]);
	}

	private Label AdvanceLabel()
	{
		return _labels[NextCodepos()];
	}

	private void Advance()
	{
		BrFar(AdvanceLabel());
	}

	private void InitLocalCultureInfo()
	{
		Call(s_cultureInfoGetCurrentCultureMethod);
		Callvirt(s_cultureInfoGetTextInfoMethod);
		Stloc(_textInfoLocal);
	}

	private void CallToLower()
	{
		if (UseToLowerInvariant)
		{
			Call(s_charToLowerInvariantMethod);
			return;
		}
		using RentedLocalBuilder rentedLocalBuilder = RentInt32Local();
		Stloc(rentedLocalBuilder);
		Ldloc(_textInfoLocal);
		Ldloc(rentedLocalBuilder);
		Callvirt(s_textInfoToLowerMethod);
	}

	private static bool ParticipatesInCaseConversion(int comparison)
	{
		switch (char.GetUnicodeCategory((char)comparison))
		{
		case UnicodeCategory.DecimalDigitNumber:
		case UnicodeCategory.OtherNumber:
		case UnicodeCategory.SpaceSeparator:
		case UnicodeCategory.LineSeparator:
		case UnicodeCategory.ParagraphSeparator:
		case UnicodeCategory.Control:
		case UnicodeCategory.ConnectorPunctuation:
		case UnicodeCategory.DashPunctuation:
		case UnicodeCategory.OpenPunctuation:
		case UnicodeCategory.ClosePunctuation:
		case UnicodeCategory.InitialQuotePunctuation:
		case UnicodeCategory.FinalQuotePunctuation:
		case UnicodeCategory.OtherPunctuation:
			return false;
		default:
			return true;
		}
	}

	private void GenerateForwardSection()
	{
		_uniquenote = new int[10];
		_labels = new Label[_codes.Length];
		_goto = new int[_codes.Length];
		Array.Fill(_uniquenote, -1);
		for (int i = 0; i < _codes.Length; i += RegexCode.OpcodeSize(_codes[i]))
		{
			_goto[i] = -1;
			_labels[i] = DefineLabel();
		}
		Mvfldloc(s_runtextField, _runtextLocal);
		Mvfldloc(s_runtextbegField, _runtextbegLocal);
		Mvfldloc(s_runtextendField, _runtextendLocal);
		Mvfldloc(s_runtextposField, _runtextposLocal);
		Mvfldloc(s_runtrackField, _runtrackLocal);
		Mvfldloc(s_runtrackposField, _runtrackposLocal);
		Mvfldloc(s_runstackField, _runstackLocal);
		Mvfldloc(s_runstackposField, _runstackposLocal);
		_backpos = -1;
		for (int j = 0; j < _codes.Length; j += RegexCode.OpcodeSize(_codes[j]))
		{
			MarkLabel(_labels[j]);
			_codepos = j;
			_regexopcode = _codes[j];
			GenerateOneCode();
		}
	}

	private void GenerateMiddleSection()
	{
		using RentedLocalBuilder rentedLocalBuilder = RentInt32Local();
		Label l = DefineLabel();
		Label l2 = DefineLabel();
		MarkLabel(_backtrack);
		Ldthisfld(s_runtrackcountField);
		Ldc(4);
		Mul();
		Stloc(rentedLocalBuilder);
		Ldloc(_runstackposLocal);
		Ldloc(rentedLocalBuilder);
		Bge(l);
		Mvlocfld(_runstackposLocal, s_runstackposField);
		Ldthis();
		Call(s_doubleStackMethod);
		Mvfldloc(s_runstackposField, _runstackposLocal);
		Mvfldloc(s_runstackField, _runstackLocal);
		MarkLabel(l);
		Ldloc(_runtrackposLocal);
		Ldloc(rentedLocalBuilder);
		Bge(l2);
		Mvlocfld(_runtrackposLocal, s_runtrackposField);
		Ldthis();
		Call(s_doubleTrackMethod);
		Mvfldloc(s_runtrackposField, _runtrackposLocal);
		Mvfldloc(s_runtrackField, _runtrackLocal);
		MarkLabel(l2);
		PopTrack();
		Label[] array = new Label[_notecount];
		for (int i = 0; i < _notecount; i++)
		{
			array[i] = _notes[i]._label;
		}
		Switch(array);
	}

	private void GenerateBacktrackSection()
	{
		for (int i = 0; i < _notecount; i++)
		{
			BacktrackNote backtrackNote = _notes[i];
			if (backtrackNote._flags != 0)
			{
				MarkLabel(backtrackNote._label);
				_codepos = backtrackNote._codepos;
				_backpos = i;
				_regexopcode = _codes[backtrackNote._codepos] | backtrackNote._flags;
				GenerateOneCode();
			}
		}
	}

	protected void GenerateFindFirstChar()
	{
		_int32LocalsPool?.Clear();
		_readOnlySpanCharLocalsPool?.Clear();
		_runtextposLocal = DeclareInt32();
		_runtextendLocal = DeclareInt32();
		if (_code.RightToLeft)
		{
			_runtextbegLocal = DeclareInt32();
		}
		_runtextLocal = DeclareString();
		_textInfoLocal = null;
		if (!_options.HasFlag(RegexOptions.CultureInvariant))
		{
			bool flag = _options.HasFlag(RegexOptions.IgnoreCase) || (_boyerMoorePrefix?.CaseInsensitive ?? false);
			if (!flag && _leadingCharClasses != null)
			{
				for (int i = 0; i < _leadingCharClasses.Length; i++)
				{
					if (_leadingCharClasses[i].CaseInsensitive)
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				_textInfoLocal = DeclareTextInfo();
				InitLocalCultureInfo();
			}
		}
		Mvfldloc(s_runtextposField, _runtextposLocal);
		Mvfldloc(s_runtextendField, _runtextendLocal);
		if (_code.RightToLeft)
		{
			Mvfldloc(s_runtextbegField, _runtextbegLocal);
		}
		int minRequiredLength = _code.Tree.MinRequiredLength;
		Label l = DefineLabel();
		Label l2 = DefineLabel();
		if (!_code.RightToLeft)
		{
			Ldloc(_runtextposLocal);
			Ldloc(_runtextendLocal);
			if (minRequiredLength > 0)
			{
				Ldc(minRequiredLength);
				Sub();
			}
			Ble(l2);
			MarkLabel(l);
			Ldthis();
			Ldloc(_runtextendLocal);
		}
		else
		{
			Ldloc(_runtextposLocal);
			if (minRequiredLength > 0)
			{
				Ldc(minRequiredLength);
				Sub();
			}
			Ldloc(_runtextbegLocal);
			Bge(l2);
			MarkLabel(l);
			Ldthis();
			Ldloc(_runtextbegLocal);
		}
		Stfld(s_runtextposField);
		Ldc(0);
		Ret();
		MarkLabel(l2);
		if (((uint)_leadingAnchor & 0x37u) != 0)
		{
			switch (_leadingAnchor)
			{
			case 1:
			{
				Label l5 = DefineLabel();
				Ldloc(_runtextposLocal);
				if (!_code.RightToLeft)
				{
					Ldthisfld(s_runtextbegField);
					Ble(l5);
					Br(l);
				}
				else
				{
					Ldloc(_runtextbegLocal);
					Ble(l5);
					Ldthis();
					Ldloc(_runtextbegLocal);
					Stfld(s_runtextposField);
				}
				MarkLabel(l5);
				Ldc(1);
				Ret();
				return;
			}
			case 4:
			{
				Label l4 = DefineLabel();
				Ldloc(_runtextposLocal);
				Ldthisfld(s_runtextstartField);
				if (!_code.RightToLeft)
				{
					Ble(l4);
				}
				else
				{
					Bge(l4);
				}
				Br(l);
				MarkLabel(l4);
				Ldc(1);
				Ret();
				return;
			}
			case 16:
			{
				Label l7 = DefineLabel();
				if (!_code.RightToLeft)
				{
					Ldloc(_runtextposLocal);
					Ldloc(_runtextendLocal);
					Ldc(1);
					Sub();
					Bge(l7);
					Ldthis();
					Ldloc(_runtextendLocal);
					Ldc(1);
					Sub();
					Stfld(s_runtextposField);
					MarkLabel(l7);
				}
				else
				{
					Label l8 = DefineLabel();
					Ldloc(_runtextposLocal);
					Ldloc(_runtextendLocal);
					Ldc(1);
					Sub();
					Blt(l7);
					Ldloc(_runtextposLocal);
					Ldloc(_runtextendLocal);
					Beq(l8);
					Ldthisfld(s_runtextField);
					Ldloc(_runtextposLocal);
					Call(s_stringGetCharsMethod);
					Ldc(10);
					Beq(l8);
					MarkLabel(l7);
					BrFar(l);
					MarkLabel(l8);
				}
				Ldc(1);
				Ret();
				return;
			}
			case 32:
				if (minRequiredLength == 0)
				{
					Label l6 = DefineLabel();
					Ldloc(_runtextposLocal);
					Ldloc(_runtextendLocal);
					if (!_code.RightToLeft)
					{
						Bge(l6);
						Ldthis();
						Ldloc(_runtextendLocal);
						Stfld(s_runtextposField);
					}
					else
					{
						Bge(l6);
						Br(l);
					}
					MarkLabel(l6);
					Ldc(1);
					Ret();
					return;
				}
				break;
			case 2:
				if (!_code.RightToLeft)
				{
					Label l3 = DefineLabel();
					Ldloc(_runtextposLocal);
					Ldthisfld(s_runtextbegField);
					Ble(l3);
					Ldthisfld(s_runtextField);
					Ldloc(_runtextposLocal);
					Ldc(1);
					Sub();
					Call(s_stringGetCharsMethod);
					Ldc(10);
					Beq(l3);
					Ldthisfld(s_runtextField);
					Ldc(10);
					Ldloc(_runtextposLocal);
					Call(s_stringIndexOfCharInt);
					using (RentedLocalBuilder rentedLocalBuilder = RentInt32Local())
					{
						Stloc(rentedLocalBuilder);
						Ldloc(rentedLocalBuilder);
						Ldc(-1);
						Beq(l);
						Ldloc(rentedLocalBuilder);
						Ldc(1);
						Add();
						Ldloc(_runtextendLocal);
						Bgt(l);
						Ldloc(rentedLocalBuilder);
						Ldc(1);
						Add();
						Stloc(_runtextposLocal);
					}
					MarkLabel(l3);
				}
				break;
			}
		}
		if (_boyerMoorePrefix != null && _boyerMoorePrefix.NegativeUnicode == null)
		{
			LocalBuilder lt;
			int num;
			int index;
			if (!_code.RightToLeft)
			{
				lt = _runtextendLocal;
				num = -1;
				index = _boyerMoorePrefix.Pattern.Length - 1;
			}
			else
			{
				lt = _runtextbegLocal;
				num = _boyerMoorePrefix.Pattern.Length;
				index = 0;
			}
			int i2 = _boyerMoorePrefix.Pattern[index];
			Mvfldloc(s_runtextField, _runtextLocal);
			Ldloc(_runtextposLocal);
			if (!_code.RightToLeft)
			{
				Ldc(_boyerMoorePrefix.Pattern.Length - 1);
				Add();
			}
			else
			{
				Ldc(_boyerMoorePrefix.Pattern.Length);
				Sub();
			}
			Stloc(_runtextposLocal);
			Label l9 = DefineLabel();
			Br(l9);
			Label l10 = DefineLabel();
			MarkLabel(l10);
			Ldc(_code.RightToLeft ? (-_boyerMoorePrefix.Pattern.Length) : _boyerMoorePrefix.Pattern.Length);
			Label l11 = DefineLabel();
			MarkLabel(l11);
			Ldloc(_runtextposLocal);
			Add();
			Stloc(_runtextposLocal);
			MarkLabel(l9);
			Ldloc(_runtextposLocal);
			Ldloc(lt);
			if (!_code.RightToLeft)
			{
				BgeFar(l);
			}
			else
			{
				BltFar(l);
			}
			Rightchar();
			if (_boyerMoorePrefix.CaseInsensitive)
			{
				CallToLower();
			}
			Label l12 = DefineLabel();
			using (RentedLocalBuilder rentedLocalBuilder2 = RentInt32Local())
			{
				Stloc(rentedLocalBuilder2);
				Ldloc(rentedLocalBuilder2);
				Ldc(i2);
				BeqFar(l12);
				Ldloc(rentedLocalBuilder2);
				Ldc(_boyerMoorePrefix.LowASCII);
				Sub();
				Stloc(rentedLocalBuilder2);
				Ldloc(rentedLocalBuilder2);
				Ldc(_boyerMoorePrefix.HighASCII - _boyerMoorePrefix.LowASCII);
				BgtUn(l10);
				int num2 = _boyerMoorePrefix.HighASCII - _boyerMoorePrefix.LowASCII + 1;
				if (num2 > 1)
				{
					string str = string.Create(num2, (this, num), delegate(Span<char> span, (RegexCompiler thisRef, int beforefirst) state)
					{
						for (int k = 0; k < span.Length; k++)
						{
							int num8 = state.thisRef._boyerMoorePrefix.NegativeASCII[k + state.thisRef._boyerMoorePrefix.LowASCII];
							if (num8 == state.beforefirst)
							{
								num8 = state.thisRef._boyerMoorePrefix.Pattern.Length;
							}
							else if (state.thisRef._code.RightToLeft)
							{
								num8 = -num8;
							}
							span[k] = (char)num8;
						}
					});
					Ldstr(str);
					Ldloc(rentedLocalBuilder2);
					Call(s_stringGetCharsMethod);
					if (_code.RightToLeft)
					{
						Neg();
					}
				}
				else
				{
					int num3 = _boyerMoorePrefix.NegativeASCII[_boyerMoorePrefix.LowASCII];
					if (num3 == num)
					{
						num3 = (_code.RightToLeft ? (-_boyerMoorePrefix.Pattern.Length) : _boyerMoorePrefix.Pattern.Length);
					}
					Ldc(num3);
				}
				BrFar(l11);
			}
			MarkLabel(l12);
			Ldloc(_runtextposLocal);
			using RentedLocalBuilder rentedLocalBuilder3 = RentInt32Local();
			Stloc(rentedLocalBuilder3);
			int num4 = int.MaxValue;
			Label l13 = default(Label);
			for (int num5 = _boyerMoorePrefix.Pattern.Length - 2; num5 >= 0; num5--)
			{
				int num6 = (_code.RightToLeft ? (_boyerMoorePrefix.Pattern.Length - 1 - num5) : num5);
				Ldloc(_runtextLocal);
				Ldloc(rentedLocalBuilder3);
				Ldc(1);
				Sub(_code.RightToLeft);
				Stloc(rentedLocalBuilder3);
				Ldloc(rentedLocalBuilder3);
				Call(s_stringGetCharsMethod);
				if (_boyerMoorePrefix.CaseInsensitive && ParticipatesInCaseConversion(_boyerMoorePrefix.Pattern[num6]))
				{
					CallToLower();
				}
				Ldc(_boyerMoorePrefix.Pattern[num6]);
				if (num4 == _boyerMoorePrefix.Positive[num6])
				{
					BneFar(l13);
				}
				else
				{
					Label l14 = DefineLabel();
					Beq(l14);
					l13 = DefineLabel();
					num4 = _boyerMoorePrefix.Positive[num6];
					MarkLabel(l13);
					Ldc(num4);
					BrFar(l11);
					MarkLabel(l14);
				}
			}
			Ldthis();
			Ldloc(rentedLocalBuilder3);
			if (_code.RightToLeft)
			{
				Ldc(1);
				Add();
			}
			Stfld(s_runtextposField);
			Ldc(1);
			Ret();
			return;
		}
		if (_leadingCharClasses == null)
		{
			Ldc(1);
			Ret();
			return;
		}
		if (_code.RightToLeft)
		{
			using (RentedLocalBuilder rentedLocalBuilder4 = RentInt32Local())
			{
				Label l15 = DefineLabel();
				Label l16 = DefineLabel();
				Label l17 = DefineLabel();
				Label l18 = DefineLabel();
				Label l19 = DefineLabel();
				Mvfldloc(s_runtextField, _runtextLocal);
				Ldloc(_runtextposLocal);
				Ldloc(_runtextbegLocal);
				Sub();
				Stloc(rentedLocalBuilder4);
				if (minRequiredLength == 0)
				{
					Ldloc(rentedLocalBuilder4);
					Ldc(0);
					BleFar(l18);
				}
				MarkLabel(l15);
				Ldloc(rentedLocalBuilder4);
				Ldc(1);
				Sub();
				Stloc(rentedLocalBuilder4);
				Leftcharnext();
				if (!RegexCharClass.IsSingleton(_leadingCharClasses[0].CharClass))
				{
					EmitMatchCharacterClass(_leadingCharClasses[0].CharClass, _leadingCharClasses[0].CaseInsensitive);
					Brtrue(l16);
				}
				else
				{
					Ldc(RegexCharClass.SingletonChar(_leadingCharClasses[0].CharClass));
					Beq(l16);
				}
				MarkLabel(l19);
				Ldloc(rentedLocalBuilder4);
				Ldc(0);
				if (!RegexCharClass.IsSingleton(_leadingCharClasses[0].CharClass))
				{
					BgtFar(l15);
				}
				else
				{
					Bgt(l15);
				}
				Ldc(0);
				Br(l17);
				MarkLabel(l16);
				Ldloc(_runtextposLocal);
				Ldc(1);
				Sub(_code.RightToLeft);
				Stloc(_runtextposLocal);
				Ldc(1);
				MarkLabel(l17);
				Mvlocfld(_runtextposLocal, s_runtextposField);
				Ret();
				MarkLabel(l18);
				Ldc(0);
				Ret();
				return;
			}
		}
		if (minRequiredLength < _leadingCharClasses.Length)
		{
			Ldloc(_runtextendLocal);
			if (_leadingCharClasses.Length > 1)
			{
				Ldc(_leadingCharClasses.Length - 1);
				Sub();
			}
			Ldloc(_runtextposLocal);
			BleFar(l);
		}
		using RentedLocalBuilder rentedLocalBuilder6 = RentInt32Local();
		using RentedLocalBuilder rentedLocalBuilder5 = RentReadOnlySpanCharLocal();
		Ldthisfld(s_runtextField);
		Ldloc(_runtextposLocal);
		Ldloc(_runtextendLocal);
		Ldloc(_runtextposLocal);
		Sub();
		Call(s_stringAsSpanIntIntMethod);
		Stloc(rentedLocalBuilder5);
		Span<char> chars = stackalloc char[3];
		int num7 = 0;
		int j = 0;
		bool flag2 = !_leadingCharClasses[0].CaseInsensitive && (num7 = RegexCharClass.GetSetChars(_leadingCharClasses[0].CharClass, chars)) > 0 && !RegexCharClass.IsNegated(_leadingCharClasses[0].CharClass);
		bool flag3 = !flag2 || _leadingCharClasses.Length > 1;
		Label l20 = default(Label);
		Label l21 = default(Label);
		Label l22 = default(Label);
		if (flag3)
		{
			l20 = DefineLabel();
			l21 = DefineLabel();
			l22 = DefineLabel();
			Ldc(0);
			Stloc(rentedLocalBuilder6);
			BrFar(l20);
			MarkLabel(l22);
		}
		if (flag2)
		{
			j = 1;
			if (flag3)
			{
				Ldloca(rentedLocalBuilder5);
				Ldloc(rentedLocalBuilder6);
				Call(s_spanSliceIntMethod);
			}
			else
			{
				Ldloc(rentedLocalBuilder5);
			}
			switch (num7)
			{
			case 1:
				Ldc(chars[0]);
				Call(s_spanIndexOf);
				break;
			case 2:
				Ldc(chars[0]);
				Ldc(chars[1]);
				Call(s_spanIndexOfAnyCharChar);
				break;
			default:
				Ldc(chars[0]);
				Ldc(chars[1]);
				Ldc(chars[2]);
				Call(s_spanIndexOfAnyCharCharChar);
				break;
			}
			if (flag3)
			{
				using RentedLocalBuilder rentedLocalBuilder7 = RentInt32Local();
				Stloc(rentedLocalBuilder7);
				Ldloc(rentedLocalBuilder6);
				Ldloc(rentedLocalBuilder7);
				Add();
				Stloc(rentedLocalBuilder6);
				Ldloc(rentedLocalBuilder7);
				Ldc(0);
				BltFar(l);
			}
			else
			{
				Stloc(rentedLocalBuilder6);
				Ldloc(rentedLocalBuilder6);
				Ldc(0);
				BltFar(l);
			}
			if (_leadingCharClasses.Length > 1)
			{
				Ldloca(rentedLocalBuilder5);
				Call(s_spanGetLengthMethod);
				Ldc(_leadingCharClasses.Length - 1);
				Sub();
				Ldloc(rentedLocalBuilder6);
				BleFar(l);
			}
		}
		for (; j < _leadingCharClasses.Length; j++)
		{
			Ldloca(rentedLocalBuilder5);
			Ldloc(rentedLocalBuilder6);
			if (j > 0)
			{
				Ldc(j);
				Add();
			}
			Call(s_spanGetItemMethod);
			LdindU2();
			EmitMatchCharacterClass(_leadingCharClasses[j].CharClass, _leadingCharClasses[j].CaseInsensitive);
			BrfalseFar(l21);
		}
		Ldthis();
		Ldloc(_runtextposLocal);
		Ldloc(rentedLocalBuilder6);
		Add();
		Stfld(s_runtextposField);
		Ldc(1);
		Ret();
		if (flag3)
		{
			MarkLabel(l21);
			Ldloc(rentedLocalBuilder6);
			Ldc(1);
			Add();
			Stloc(rentedLocalBuilder6);
			MarkLabel(l20);
			Ldloc(rentedLocalBuilder6);
			Ldloca(rentedLocalBuilder5);
			Call(s_spanGetLengthMethod);
			if (_leadingCharClasses.Length > 1)
			{
				Ldc(_leadingCharClasses.Length - 1);
				Sub();
			}
			BltFar(l22);
			BrFar(l);
		}
	}

	private bool TryGenerateNonBacktrackingGo(RegexNode node)
	{
		if ((node.Options & RegexOptions.RightToLeft) != 0)
		{
			return false;
		}
		node = node.Child(0);
		if (!NodeSupportsNonBacktrackingImplementation(node, 20))
		{
			return false;
		}
		LocalBuilder runtextLocal = DeclareString();
		LocalBuilder lt = DeclareInt32();
		LocalBuilder runtextposLocal = DeclareInt32();
		LocalBuilder textSpanLocal = DeclareReadOnlySpanChar();
		LocalBuilder runtextendLocal = DeclareInt32();
		Label l = DefineLabel();
		Label doneLabel = DefineLabel();
		if (_hasTimeout)
		{
			_loopTimeoutCounterLocal = DeclareInt32();
		}
		InitializeCultureForGoIfNecessary();
		Mvfldloc(s_runtextField, runtextLocal);
		Mvfldloc(s_runtextendField, runtextendLocal);
		Ldthisfld(s_runtextposField);
		Stloc(runtextposLocal);
		Ldloc(runtextposLocal);
		Stloc(lt);
		int textSpanPos = 0;
		LoadTextSpanLocal();
		EmitNode(node);
		MarkLabel(l);
		Ldthis();
		Ldloc(runtextposLocal);
		if (textSpanPos > 0)
		{
			Ldc(textSpanPos);
			Add();
			Stloc(runtextposLocal);
			Ldloc(runtextposLocal);
		}
		Stfld(s_runtextposField);
		Ldthis();
		Ldc(0);
		Ldloc(lt);
		Ldloc(runtextposLocal);
		Call(s_captureMethod);
		if (((uint)node.Options & 0x80000000u) != 0)
		{
			Label l2 = DefineLabel();
			Br(l2);
			MarkLabel(doneLabel);
			Label l3 = DefineLabel();
			Label l4 = DefineLabel();
			Br(l3);
			MarkLabel(l4);
			Ldthis();
			Call(s_uncaptureMethod);
			MarkLabel(l3);
			Ldthis();
			Call(s_crawlposMethod);
			Brtrue(l4);
			MarkLabel(l2);
		}
		else
		{
			MarkLabel(doneLabel);
		}
		Ret();
		return true;
		void EmitAnchors(RegexNode node)
		{
			switch (node.Type)
			{
			default:
				return;
			case 18:
			case 19:
				if (textSpanPos > 0)
				{
					BrFar(doneLabel);
				}
				else
				{
					Ldloc(runtextposLocal);
					Ldthisfld((node.Type == 18) ? s_runtextbegField : s_runtextstartField);
					BneFar(doneLabel);
				}
				return;
			case 14:
				if (textSpanPos > 0)
				{
					Ldloca(textSpanLocal);
					Ldc(textSpanPos - 1);
					Call(s_spanGetItemMethod);
					LdindU2();
					Ldc(10);
					BneFar(doneLabel);
				}
				else
				{
					Label l5 = DefineLabel();
					Ldloc(runtextposLocal);
					Ldthisfld(s_runtextbegField);
					Ble(l5);
					Ldthisfld(s_runtextField);
					Ldloc(runtextposLocal);
					Ldc(1);
					Sub();
					Call(s_stringGetCharsMethod);
					Ldc(10);
					BneFar(doneLabel);
					MarkLabel(l5);
				}
				return;
			case 21:
				Ldc(textSpanPos);
				Ldloca(textSpanLocal);
				Call(s_spanGetLengthMethod);
				BltUnFar(doneLabel);
				return;
			case 20:
				Ldc(textSpanPos);
				Ldloca(textSpanLocal);
				Call(s_spanGetLengthMethod);
				Ldc(1);
				Sub();
				BltFar(doneLabel);
				break;
			case 15:
				break;
			case 16:
			case 17:
				return;
			}
			Label l6 = DefineLabel();
			Ldc(textSpanPos);
			Ldloca(textSpanLocal);
			Call(s_spanGetLengthMethod);
			BgeUn(l6);
			Ldloca(textSpanLocal);
			Ldc(textSpanPos);
			Call(s_spanGetItemMethod);
			LdindU2();
			Ldc(10);
			BneFar(doneLabel);
			MarkLabel(l6);
		}
		void EmitAtomicAlternate(RegexNode node)
		{
			using RentedLocalBuilder rentedLocalBuilder5 = RentInt32Local();
			Ldloc(runtextposLocal);
			Stloc(rentedLocalBuilder5);
			int num2 = textSpanPos;
			RentedLocalBuilder? rentedLocalBuilder6 = null;
			if (((uint)node.Options & 0x80000000u) != 0)
			{
				rentedLocalBuilder6 = RentInt32Local();
				Ldthis();
				Call(s_crawlposMethod);
				RentedLocalBuilder? rentedLocalBuilder7 = rentedLocalBuilder6;
				Stloc(rentedLocalBuilder7.HasValue ? ((LocalBuilder)rentedLocalBuilder7.GetValueOrDefault()) : null);
			}
			Label l17 = DefineLabel();
			Label label4 = doneLabel;
			int num3 = node.ChildCount();
			for (int j = 0; j < num3 - 1; j++)
			{
				Label l18 = (doneLabel = DefineLabel());
				EmitNode(node.Child(j));
				TransferTextSpanPosToRunTextPos();
				BrFar(l17);
				MarkLabel(l18);
				Ldloc(rentedLocalBuilder5);
				Stloc(runtextposLocal);
				LoadTextSpanLocal();
				textSpanPos = num2;
				if (rentedLocalBuilder6.HasValue)
				{
					RentedLocalBuilder? rentedLocalBuilder7 = rentedLocalBuilder6;
					EmitUncaptureUntil(rentedLocalBuilder7.HasValue ? ((LocalBuilder)rentedLocalBuilder7.GetValueOrDefault()) : null);
				}
			}
			if (rentedLocalBuilder6.HasValue)
			{
				Label l19 = (doneLabel = DefineLabel());
				EmitNode(node.Child(num3 - 1));
				doneLabel = label4;
				TransferTextSpanPosToRunTextPos();
				Br(l17);
				MarkLabel(l19);
				RentedLocalBuilder? rentedLocalBuilder7 = rentedLocalBuilder6;
				EmitUncaptureUntil(rentedLocalBuilder7.HasValue ? ((LocalBuilder)rentedLocalBuilder7.GetValueOrDefault()) : null);
				BrFar(doneLabel);
			}
			else
			{
				doneLabel = label4;
				EmitNode(node.Child(num3 - 1));
				TransferTextSpanPosToRunTextPos();
			}
			MarkLabel(l17);
			rentedLocalBuilder6?.Dispose();
		}
		void EmitAtomicNodeLoop(RegexNode node)
		{
			if (node.M == node.N)
			{
				EmitNodeRepeater(node);
				return;
			}
			using RentedLocalBuilder rentedLocalBuilder2 = RentInt32Local();
			using RentedLocalBuilder rentedLocalBuilder3 = RentInt32Local();
			Label label2 = doneLabel;
			doneLabel = DefineLabel();
			TransferTextSpanPosToRunTextPos();
			Label l10 = DefineLabel();
			Label l11 = DefineLabel();
			int m2 = node.M;
			int n2 = node.N;
			Ldc(0);
			Stloc(rentedLocalBuilder2);
			BrFar(l10);
			MarkLabel(l11);
			EmitTimeoutCheck();
			Label l12 = DefineLabel();
			Label label3 = doneLabel;
			doneLabel = DefineLabel();
			Ldloc(runtextposLocal);
			Stloc(rentedLocalBuilder3);
			EmitNode(node.Child(0));
			TransferTextSpanPosToRunTextPos();
			Br(l12);
			MarkLabel(doneLabel);
			doneLabel = label3;
			Ldloc(rentedLocalBuilder3);
			Stloc(runtextposLocal);
			BrFar(doneLabel);
			MarkLabel(l12);
			Ldloc(rentedLocalBuilder2);
			Ldc(1);
			Add();
			Stloc(rentedLocalBuilder2);
			MarkLabel(l10);
			if (n2 != int.MaxValue)
			{
				Ldloc(rentedLocalBuilder2);
				Ldc(n2);
				BltFar(l11);
			}
			else
			{
				BrFar(l11);
			}
			MarkLabel(doneLabel);
			doneLabel = label2;
			if (m2 > 0)
			{
				Ldloc(rentedLocalBuilder2);
				Ldc(m2);
				BltFar(doneLabel);
			}
		}
		void EmitAtomicSingleCharZeroOrOne(RegexNode node)
		{
			Label l7 = DefineLabel();
			Ldc(textSpanPos);
			Ldloca(textSpanLocal);
			Call(s_spanGetLengthMethod);
			BgeUnFar(l7);
			Ldloca(textSpanLocal);
			Ldc(textSpanPos);
			Call(s_spanGetItemMethod);
			LdindU2();
			switch (node.Type)
			{
			case 43:
				if (IsCaseInsensitive(node) && ParticipatesInCaseConversion(node.Ch))
				{
					CallToLower();
				}
				Ldc(node.Ch);
				BneFar(l7);
				break;
			case 44:
				if (IsCaseInsensitive(node) && ParticipatesInCaseConversion(node.Ch))
				{
					CallToLower();
				}
				Ldc(node.Ch);
				BeqFar(l7);
				break;
			case 45:
				EmitMatchCharacterClass(node.Str, IsCaseInsensitive(node));
				BrfalseFar(l7);
				break;
			}
			Ldloca(textSpanLocal);
			Ldc(1);
			Call(s_spanSliceIntMethod);
			Stloc(textSpanLocal);
			Ldloc(runtextposLocal);
			Ldc(1);
			Add();
			Stloc(runtextposLocal);
			MarkLabel(l7);
		}
		void EmitBoundary(RegexNode node)
		{
			Ldthis();
			Ldloc(runtextposLocal);
			if (textSpanPos > 0)
			{
				Ldc(textSpanPos);
				Add();
			}
			Ldthisfld(s_runtextbegField);
			Ldloc(runtextendLocal);
			switch (node.Type)
			{
			case 16:
				Call(s_isBoundaryMethod);
				BrfalseFar(doneLabel);
				break;
			case 17:
				Call(s_isBoundaryMethod);
				BrtrueFar(doneLabel);
				break;
			case 41:
				Call(s_isECMABoundaryMethod);
				BrfalseFar(doneLabel);
				break;
			default:
				Call(s_isECMABoundaryMethod);
				BrtrueFar(doneLabel);
				break;
			}
		}
		void EmitCapture(RegexNode node)
		{
			using RentedLocalBuilder rentedLocalBuilder10 = RentInt32Local();
			int num5 = node.M;
			if (num5 != -1 && _code.Caps != null)
			{
				num5 = (int)_code.Caps[num5];
			}
			TransferTextSpanPosToRunTextPos();
			Ldloc(runtextposLocal);
			Stloc(rentedLocalBuilder10);
			EmitNode(node.Child(0));
			TransferTextSpanPosToRunTextPos();
			Ldthis();
			Ldc(num5);
			Ldloc(rentedLocalBuilder10);
			Ldloc(runtextposLocal);
			Call(s_captureMethod);
		}
		void EmitMultiChar(RegexNode node)
		{
			bool flag = IsCaseInsensitive(node);
			if (!flag && node.Str.Length > 64)
			{
				Ldloca(textSpanLocal);
				Ldc(textSpanPos);
				Call(s_spanSliceIntMethod);
				Ldstr(node.Str);
				Call(s_stringAsSpanMethod);
				Call(s_spanStartsWith);
				BrfalseFar(doneLabel);
				textSpanPos += node.Str.Length;
			}
			else
			{
				ReadOnlySpan<char> span = node.Str;
				EmitSpanLengthCheck(span.Length);
				if (!flag && !_persistsAssembly)
				{
					if (IntPtr.Size == 8)
					{
						while (span.Length >= 4)
						{
							EmitTextSpanOffset();
							Unaligned(1);
							LdindI8();
							LdcI8(MemoryMarshal.Read<long>(MemoryMarshal.AsBytes(span)));
							BneFar(doneLabel);
							textSpanPos += 4;
							span = span.Slice(4);
						}
					}
					while (span.Length >= 2)
					{
						EmitTextSpanOffset();
						Unaligned(1);
						LdindI4();
						Ldc(MemoryMarshal.Read<int>(MemoryMarshal.AsBytes(span)));
						BneFar(doneLabel);
						textSpanPos += 2;
						span = span.Slice(2);
					}
				}
				for (int i = 0; i < span.Length; i++)
				{
					EmitTextSpanOffset();
					textSpanPos++;
					LdindU2();
					if (flag && ParticipatesInCaseConversion(span[i]))
					{
						CallToLower();
					}
					Ldc(span[i]);
					BneFar(doneLabel);
				}
			}
		}
		void EmitNegativeLookaheadAssertion(RegexNode node)
		{
			using RentedLocalBuilder rentedLocalBuilder12 = RentInt32Local();
			Ldloc(runtextposLocal);
			Stloc(rentedLocalBuilder12);
			int num7 = textSpanPos;
			Label label5 = doneLabel;
			doneLabel = DefineLabel();
			EmitNode(node.Child(0));
			BrFar(label5);
			MarkLabel(doneLabel);
			doneLabel = label5;
			Ldloc(rentedLocalBuilder12);
			Stloc(runtextposLocal);
			LoadTextSpanLocal();
			textSpanPos = num7;
		}
		void EmitNode(RegexNode node)
		{
			switch (node.Type)
			{
			case 9:
			case 10:
			case 11:
				EmitSingleChar(node);
				break;
			case 16:
			case 17:
			case 41:
			case 42:
				EmitBoundary(node);
				break;
			case 14:
			case 15:
			case 18:
			case 19:
			case 20:
			case 21:
				EmitAnchors(node);
				break;
			case 12:
				EmitMultiChar(node);
				break;
			case 43:
			case 44:
			case 45:
				EmitSingleCharAtomicLoop(node);
				break;
			case 26:
				EmitAtomicNodeLoop(node);
				break;
			case 27:
				if (node.M > 0)
				{
					EmitNodeRepeater(node);
				}
				break;
			case 32:
				EmitNode(node.Child(0));
				break;
			case 24:
				EmitAtomicAlternate(node);
				break;
			case 3:
			case 4:
			case 5:
			case 6:
			case 7:
			case 8:
				EmitSingleCharRepeater(node);
				break;
			case 25:
			{
				int num8 = node.ChildCount();
				for (int num9 = 0; num9 < num8; num9++)
				{
					EmitNode(node.Child(num9));
				}
				break;
			}
			case 28:
				EmitCapture(node);
				break;
			case 30:
				EmitPositiveLookaheadAssertion(node);
				break;
			case 31:
				EmitNegativeLookaheadAssertion(node);
				break;
			case 22:
				BrFar(doneLabel);
				break;
			case 46:
				EmitUpdateBumpalong();
				break;
			case 13:
			case 23:
			case 29:
			case 33:
			case 34:
			case 35:
			case 36:
			case 37:
			case 38:
			case 39:
			case 40:
				break;
			}
		}
		void EmitNodeRepeater(RegexNode node)
		{
			int m3 = node.M;
			if (m3 == 1)
			{
				EmitNode(node.Child(0));
				return;
			}
			TransferTextSpanPosToRunTextPos();
			Label l13 = DefineLabel();
			Label l14 = DefineLabel();
			using RentedLocalBuilder rentedLocalBuilder4 = RentInt32Local();
			Ldc(0);
			Stloc(rentedLocalBuilder4);
			BrFar(l13);
			MarkLabel(l14);
			EmitTimeoutCheck();
			EmitNode(node.Child(0));
			TransferTextSpanPosToRunTextPos();
			Ldloc(rentedLocalBuilder4);
			Ldc(1);
			Add();
			Stloc(rentedLocalBuilder4);
			MarkLabel(l13);
			Ldloc(rentedLocalBuilder4);
			Ldc(m3);
			BltFar(l14);
		}
		void EmitPositiveLookaheadAssertion(RegexNode node)
		{
			using RentedLocalBuilder rentedLocalBuilder11 = RentInt32Local();
			Ldloc(runtextposLocal);
			Stloc(rentedLocalBuilder11);
			int num6 = textSpanPos;
			EmitNode(node.Child(0));
			Ldloc(rentedLocalBuilder11);
			Stloc(runtextposLocal);
			LoadTextSpanLocal();
			textSpanPos = num6;
		}
		void EmitSingleChar(RegexNode node, bool emitLengthCheck = true, LocalBuilder offset = null)
		{
			if (emitLengthCheck)
			{
				EmitSpanLengthCheck(1, offset);
			}
			Ldloca(textSpanLocal);
			EmitSum(textSpanPos, offset);
			Call(s_spanGetItemMethod);
			LdindU2();
			switch (node.Type)
			{
			case 5:
			case 8:
			case 11:
			case 45:
				EmitMatchCharacterClass(node.Str, IsCaseInsensitive(node));
				BrfalseFar(doneLabel);
				break;
			case 3:
			case 6:
			case 9:
			case 43:
				if (IsCaseInsensitive(node) && ParticipatesInCaseConversion(node.Ch))
				{
					CallToLower();
				}
				Ldc(node.Ch);
				BneFar(doneLabel);
				break;
			default:
				if (IsCaseInsensitive(node) && ParticipatesInCaseConversion(node.Ch))
				{
					CallToLower();
				}
				Ldc(node.Ch);
				BeqFar(doneLabel);
				break;
			}
			textSpanPos++;
		}
		void EmitSingleCharAtomicLoop(RegexNode node)
		{
			if (node.M == node.N)
			{
				EmitSingleCharRepeater(node);
			}
			else
			{
				if (node.M != 0 || node.N != 1)
				{
					int m = node.M;
					int n = node.N;
					using RentedLocalBuilder rentedLocalBuilder = RentInt32Local();
					Label label = doneLabel;
					doneLabel = DefineLabel();
					Span<char> chars = stackalloc char[3];
					int num = 0;
					if (node.Type == 44 && n == int.MaxValue && (!IsCaseInsensitive(node) || !ParticipatesInCaseConversion(node.Ch)))
					{
						if (textSpanPos > 0)
						{
							Ldloca(textSpanLocal);
							Ldc(textSpanPos);
							Call(s_spanSliceIntMethod);
						}
						else
						{
							Ldloc(textSpanLocal);
						}
						Ldc(node.Ch);
						Call(s_spanIndexOf);
						Stloc(rentedLocalBuilder);
						Ldloc(rentedLocalBuilder);
						Ldc(-1);
						BneFar(doneLabel);
						Ldloca(textSpanLocal);
						Call(s_spanGetLengthMethod);
						if (textSpanPos > 0)
						{
							Ldc(textSpanPos);
							Sub();
						}
						Stloc(rentedLocalBuilder);
					}
					else if (node.Type == 45 && n == int.MaxValue && !IsCaseInsensitive(node) && (num = RegexCharClass.GetSetChars(node.Str, chars)) > 1 && RegexCharClass.IsNegated(node.Str))
					{
						if (textSpanPos > 0)
						{
							Ldloca(textSpanLocal);
							Ldc(textSpanPos);
							Call(s_spanSliceIntMethod);
						}
						else
						{
							Ldloc(textSpanLocal);
						}
						Ldc(chars[0]);
						Ldc(chars[1]);
						if (num == 2)
						{
							Call(s_spanIndexOfAnyCharChar);
						}
						else
						{
							Ldc(chars[2]);
							Call(s_spanIndexOfAnyCharCharChar);
						}
						Stloc(rentedLocalBuilder);
						Ldloc(rentedLocalBuilder);
						Ldc(-1);
						BneFar(doneLabel);
						Ldloca(textSpanLocal);
						Call(s_spanGetLengthMethod);
						if (textSpanPos > 0)
						{
							Ldc(textSpanPos);
							Sub();
						}
						Stloc(rentedLocalBuilder);
					}
					else if (node.Type == 45 && n == int.MaxValue && node.Str == "\0\u0001\0\0")
					{
						TransferTextSpanPosToRunTextPos();
						Ldloc(runtextendLocal);
						Ldloc(runtextposLocal);
						Sub();
						Stloc(rentedLocalBuilder);
					}
					else
					{
						TransferTextSpanPosToRunTextPos();
						Label l8 = DefineLabel();
						Label l9 = DefineLabel();
						Ldc(0);
						Stloc(rentedLocalBuilder);
						BrFar(l8);
						MarkLabel(l9);
						EmitTimeoutCheck();
						Ldloc(rentedLocalBuilder);
						Ldloca(textSpanLocal);
						Call(s_spanGetLengthMethod);
						BgeUnFar(doneLabel);
						Ldloca(textSpanLocal);
						Ldloc(rentedLocalBuilder);
						Call(s_spanGetItemMethod);
						LdindU2();
						switch (node.Type)
						{
						case 43:
							if (IsCaseInsensitive(node) && ParticipatesInCaseConversion(node.Ch))
							{
								CallToLower();
							}
							Ldc(node.Ch);
							BneFar(doneLabel);
							break;
						case 44:
							if (IsCaseInsensitive(node) && ParticipatesInCaseConversion(node.Ch))
							{
								CallToLower();
							}
							Ldc(node.Ch);
							BeqFar(doneLabel);
							break;
						case 45:
							EmitMatchCharacterClass(node.Str, IsCaseInsensitive(node));
							BrfalseFar(doneLabel);
							break;
						}
						Ldloc(rentedLocalBuilder);
						Ldc(1);
						Add();
						Stloc(rentedLocalBuilder);
						MarkLabel(l8);
						if (n != int.MaxValue)
						{
							Ldloc(rentedLocalBuilder);
							Ldc(n);
							BltFar(l9);
						}
						else
						{
							BrFar(l9);
						}
					}
					MarkLabel(doneLabel);
					doneLabel = label;
					if (m > 0)
					{
						Ldloc(rentedLocalBuilder);
						Ldc(m);
						BltFar(doneLabel);
					}
					Ldloca(textSpanLocal);
					Ldloc(rentedLocalBuilder);
					Call(s_spanSliceIntMethod);
					Stloc(textSpanLocal);
					Ldloc(runtextposLocal);
					Ldloc(rentedLocalBuilder);
					Add();
					Stloc(runtextposLocal);
					return;
				}
				EmitAtomicSingleCharZeroOrOne(node);
			}
		}
		void EmitSingleCharRepeater(RegexNode node)
		{
			int m4 = node.M;
			if (m4 != 0)
			{
				EmitSpanLengthCheck(m4);
				if (m4 > 16)
				{
					Label l20 = DefineLabel();
					Label l21 = DefineLabel();
					using RentedLocalBuilder rentedLocalBuilder8 = RentReadOnlySpanCharLocal();
					Ldloca(textSpanLocal);
					Ldc(textSpanPos);
					Ldc(m4);
					Call(s_spanSliceIntIntMethod);
					Stloc(rentedLocalBuilder8);
					using RentedLocalBuilder rentedLocalBuilder9 = RentInt32Local();
					Ldc(0);
					Stloc(rentedLocalBuilder9);
					BrFar(l20);
					MarkLabel(l21);
					EmitTimeoutCheck();
					LocalBuilder localBuilder = textSpanLocal;
					int num4 = textSpanPos;
					textSpanLocal = rentedLocalBuilder8;
					textSpanPos = 0;
					EmitSingleChar(node, emitLengthCheck: false, rentedLocalBuilder9);
					textSpanLocal = localBuilder;
					textSpanPos = num4;
					Ldloc(rentedLocalBuilder9);
					Ldc(1);
					Add();
					Stloc(rentedLocalBuilder9);
					MarkLabel(l20);
					Ldloc(rentedLocalBuilder9);
					Ldloca(rentedLocalBuilder8);
					Call(s_spanGetLengthMethod);
					BltFar(l21);
					textSpanPos += m4;
					return;
				}
				for (int k = 0; k < m4; k++)
				{
					EmitSingleChar(node, emitLengthCheck: false);
				}
			}
		}
		void EmitSpanLengthCheck(int requiredLength, LocalBuilder dynamicRequiredLength = null)
		{
			EmitSum(textSpanPos + requiredLength - 1, dynamicRequiredLength);
			Ldloca(textSpanLocal);
			Call(s_spanGetLengthMethod);
			BgeUnFar(doneLabel);
		}
		void EmitSum(int constant, LocalBuilder local)
		{
			if (local == null)
			{
				Ldc(constant);
			}
			else if (constant == 0)
			{
				Ldloc(local);
			}
			else
			{
				Ldloc(local);
				Ldc(constant);
				Add();
			}
		}
		void EmitTextSpanOffset()
		{
			Ldloc(textSpanLocal);
			Call(s_memoryMarshalGetReference);
			if (textSpanPos > 0)
			{
				Ldc(textSpanPos * 2);
				Add();
			}
		}
		void EmitUncaptureUntil(LocalBuilder startingCrawlpos)
		{
			Label l15 = DefineLabel();
			Label l16 = DefineLabel();
			Br(l15);
			MarkLabel(l16);
			Ldthis();
			Call(s_uncaptureMethod);
			MarkLabel(l15);
			Ldthis();
			Call(s_crawlposMethod);
			Ldloc(startingCrawlpos);
			Bne(l16);
		}
		void EmitUpdateBumpalong()
		{
			TransferTextSpanPosToRunTextPos();
			Ldthis();
			Ldloc(runtextposLocal);
			Stfld(s_runtextposField);
		}
		static bool IsCaseInsensitive(RegexNode node)
		{
			return (node.Options & RegexOptions.IgnoreCase) != 0;
		}
		void LoadTextSpanLocal()
		{
			Ldloc(runtextLocal);
			Ldloc(runtextposLocal);
			Ldloc(runtextendLocal);
			Ldloc(runtextposLocal);
			Sub();
			Call(s_stringAsSpanIntIntMethod);
			Stloc(textSpanLocal);
		}
		static bool NodeSupportsNonBacktrackingImplementation(RegexNode node, int maxDepth)
		{
			bool flag2 = false;
			if ((node.Options & RegexOptions.RightToLeft) == 0 && maxDepth > 0)
			{
				int num10 = node.ChildCount();
				switch (node.Type)
				{
				case 9:
				case 10:
				case 11:
				case 12:
				case 14:
				case 15:
				case 16:
				case 17:
				case 18:
				case 19:
				case 20:
				case 21:
				case 22:
				case 23:
				case 41:
				case 42:
				case 43:
				case 44:
				case 45:
				case 46:
					flag2 = true;
					break;
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
					flag2 = node.M == node.N || (node.Next != null && node.Next.Type == 32);
					break;
				case 26:
				case 27:
					flag2 = (node.M == node.N || (node.Next != null && node.Next.Type == 32)) && NodeSupportsNonBacktrackingImplementation(node.Child(0), maxDepth - 1);
					break;
				case 30:
				case 31:
				case 32:
					flag2 = NodeSupportsNonBacktrackingImplementation(node.Child(0), maxDepth - 1);
					break;
				case 24:
					if (node.Next == null || (!node.IsAtomicByParent() && (node.Next.Type != 28 || node.Next.Next != null)))
					{
						break;
					}
					goto case 25;
				case 25:
				{
					flag2 = true;
					for (int num11 = 0; num11 < num10; num11++)
					{
						if (flag2 && !NodeSupportsNonBacktrackingImplementation(node.Child(num11), maxDepth - 1))
						{
							flag2 = false;
							break;
						}
					}
					break;
				}
				case 28:
					flag2 = node.N == -1;
					if (flag2)
					{
						RegexNode regexNode = node.Next;
						while (regexNode != null)
						{
							switch (regexNode.Type)
							{
							case 24:
							case 25:
							case 28:
							case 30:
							case 32:
								regexNode = regexNode.Next;
								break;
							default:
								regexNode = null;
								flag2 = false;
								break;
							}
						}
						if (flag2)
						{
							flag2 = NodeSupportsNonBacktrackingImplementation(node.Child(0), maxDepth - 1);
							if (flag2)
							{
								regexNode = node;
								while (regexNode != null && (regexNode.Options & (RegexOptions)(-2147483648)) == 0)
								{
									regexNode.Options |= (RegexOptions)(-2147483648);
									regexNode = regexNode.Next;
								}
							}
						}
					}
					break;
				}
			}
			return flag2;
		}
		void TransferTextSpanPosToRunTextPos()
		{
			if (textSpanPos > 0)
			{
				Ldloc(runtextposLocal);
				Ldc(textSpanPos);
				Add();
				Stloc(runtextposLocal);
				Ldloca(textSpanLocal);
				Ldc(textSpanPos);
				Call(s_spanSliceIntMethod);
				Stloc(textSpanLocal);
				textSpanPos = 0;
			}
		}
	}

	protected void GenerateGo()
	{
		_int32LocalsPool?.Clear();
		_readOnlySpanCharLocalsPool?.Clear();
		if (!TryGenerateNonBacktrackingGo(_code.Tree.Root))
		{
			_runtextposLocal = DeclareInt32();
			_runtextLocal = DeclareString();
			_runtrackposLocal = DeclareInt32();
			_runtrackLocal = DeclareInt32Array();
			_runstackposLocal = DeclareInt32();
			_runstackLocal = DeclareInt32Array();
			if (_hasTimeout)
			{
				_loopTimeoutCounterLocal = DeclareInt32();
			}
			_runtextbegLocal = DeclareInt32();
			_runtextendLocal = DeclareInt32();
			InitializeCultureForGoIfNecessary();
			_labels = null;
			_notes = null;
			_notecount = 0;
			_backtrack = DefineLabel();
			GenerateForwardSection();
			GenerateMiddleSection();
			GenerateBacktrackSection();
		}
	}

	private void InitializeCultureForGoIfNecessary()
	{
		_textInfoLocal = null;
		if ((_options & RegexOptions.CultureInvariant) != 0)
		{
			return;
		}
		bool flag = (_options & RegexOptions.IgnoreCase) != 0;
		if (!flag)
		{
			for (int i = 0; i < _codes.Length; i += RegexCode.OpcodeSize(_codes[i]))
			{
				if ((_codes[i] & 0x200) == 512)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			_textInfoLocal = DeclareTextInfo();
			InitLocalCultureInfo();
		}
	}

	private void GenerateOneCode()
	{
		if (_hasTimeout)
		{
			Ldthis();
			Call(s_checkTimeoutMethod);
		}
		switch (_regexopcode)
		{
		case 40:
			Mvlocfld(_runtextposLocal, s_runtextposField);
			Ret();
			break;
		case 22:
			Back();
			break;
		case 46:
			Ldloc(_runtrackLocal);
			Dup();
			Ldlen();
			Ldc(1);
			Sub();
			Ldloc(_runtextposLocal);
			StelemI4();
			break;
		case 38:
			Goto(Operand(0));
			break;
		case 37:
			Ldthis();
			Ldc(Operand(0));
			Call(s_isMatchedMethod);
			BrfalseFar(_backtrack);
			break;
		case 23:
			PushTrack(_runtextposLocal);
			Track();
			break;
		case 151:
			PopTrack();
			Stloc(_runtextposLocal);
			Goto(Operand(0));
			break;
		case 30:
			ReadyPushStack();
			Ldc(-1);
			DoPush();
			TrackUnique(0);
			break;
		case 31:
			PushStack(_runtextposLocal);
			TrackUnique(0);
			break;
		case 158:
		case 159:
			PopDiscardStack();
			Back();
			break;
		case 33:
			ReadyPushTrack();
			PopStack();
			Stloc(_runtextposLocal);
			Ldloc(_runtextposLocal);
			DoPush();
			Track();
			break;
		case 161:
			ReadyPushStack();
			PopTrack();
			DoPush();
			Back();
			break;
		case 32:
		{
			if (Operand(1) != -1)
			{
				Ldthis();
				Ldc(Operand(1));
				Call(s_isMatchedMethod);
				BrfalseFar(_backtrack);
			}
			using (RentedLocalBuilder rentedLocalBuilder20 = RentInt32Local())
			{
				PopStack();
				Stloc(rentedLocalBuilder20);
				if (Operand(1) != -1)
				{
					Ldthis();
					Ldc(Operand(0));
					Ldc(Operand(1));
					Ldloc(rentedLocalBuilder20);
					Ldloc(_runtextposLocal);
					Call(s_transferCaptureMethod);
				}
				else
				{
					Ldthis();
					Ldc(Operand(0));
					Ldloc(rentedLocalBuilder20);
					Ldloc(_runtextposLocal);
					Call(s_captureMethod);
				}
				PushTrack(rentedLocalBuilder20);
			}
			TrackUnique((Operand(0) != -1 && Operand(1) != -1) ? 4 : 3);
			break;
		}
		case 160:
			ReadyPushStack();
			PopTrack();
			DoPush();
			Ldthis();
			Call(s_uncaptureMethod);
			if (Operand(0) != -1 && Operand(1) != -1)
			{
				Ldthis();
				Call(s_uncaptureMethod);
			}
			Back();
			break;
		case 24:
		{
			Label l19 = DefineLabel();
			PopStack();
			using (RentedLocalBuilder rentedLocalBuilder18 = RentInt32Local())
			{
				Stloc(rentedLocalBuilder18);
				PushTrack(rentedLocalBuilder18);
				Ldloc(rentedLocalBuilder18);
			}
			Ldloc(_runtextposLocal);
			Beq(l19);
			PushTrack(_runtextposLocal);
			PushStack(_runtextposLocal);
			Track();
			Goto(Operand(0));
			MarkLabel(l19);
			TrackUnique2(5);
			break;
		}
		case 152:
			PopTrack();
			Stloc(_runtextposLocal);
			PopStack();
			Pop();
			TrackUnique2(5);
			Advance();
			break;
		case 280:
			ReadyPushStack();
			PopTrack();
			DoPush();
			Back();
			break;
		case 25:
		{
			using (RentedLocalBuilder rentedLocalBuilder17 = RentInt32Local())
			{
				PopStack();
				Stloc(rentedLocalBuilder17);
				Label l16 = DefineLabel();
				Label l17 = DefineLabel();
				Ldloc(rentedLocalBuilder17);
				Ldc(-1);
				Beq(l16);
				PushTrack(rentedLocalBuilder17);
				Br(l17);
				MarkLabel(l16);
				PushTrack(_runtextposLocal);
				MarkLabel(l17);
				Label l18 = DefineLabel();
				Ldloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder17);
				Beq(l18);
				PushTrack(_runtextposLocal);
				Track();
				Br(AdvanceLabel());
				MarkLabel(l18);
				ReadyPushStack();
				Ldloc(rentedLocalBuilder17);
			}
			DoPush();
			TrackUnique2(6);
			break;
		}
		case 153:
			PopTrack();
			Stloc(_runtextposLocal);
			PushStack(_runtextposLocal);
			TrackUnique2(6);
			Goto(Operand(0));
			break;
		case 281:
			ReadyReplaceStack(0);
			PopTrack();
			DoReplace();
			Back();
			break;
		case 26:
			ReadyPushStack();
			Ldc(-1);
			DoPush();
			ReadyPushStack();
			Ldc(Operand(0));
			DoPush();
			TrackUnique(1);
			break;
		case 27:
			PushStack(_runtextposLocal);
			ReadyPushStack();
			Ldc(Operand(0));
			DoPush();
			TrackUnique(1);
			break;
		case 154:
		case 155:
			PopDiscardStack(2);
			Back();
			break;
		case 28:
		{
			using (RentedLocalBuilder rentedLocalBuilder15 = RentInt32Local())
			{
				PopStack();
				Stloc(rentedLocalBuilder15);
				PopStack();
				using (RentedLocalBuilder rentedLocalBuilder16 = RentInt32Local())
				{
					Stloc(rentedLocalBuilder16);
					PushTrack(rentedLocalBuilder16);
					Ldloc(rentedLocalBuilder16);
				}
				Label l14 = DefineLabel();
				Label l15 = DefineLabel();
				Ldloc(_runtextposLocal);
				Bne(l14);
				Ldloc(rentedLocalBuilder15);
				Ldc(0);
				Bge(l15);
				MarkLabel(l14);
				Ldloc(rentedLocalBuilder15);
				Ldc(Operand(1));
				Bge(l15);
				PushStack(_runtextposLocal);
				ReadyPushStack();
				Ldloc(rentedLocalBuilder15);
				Ldc(1);
				Add();
				DoPush();
				Track();
				Goto(Operand(0));
				MarkLabel(l15);
				PushTrack(rentedLocalBuilder15);
			}
			TrackUnique2(7);
			break;
		}
		case 156:
		{
			using (RentedLocalBuilder rentedLocalBuilder11 = RentInt32Local())
			{
				Label l12 = DefineLabel();
				PopStack();
				Ldc(1);
				Sub();
				Stloc(rentedLocalBuilder11);
				Ldloc(rentedLocalBuilder11);
				Ldc(0);
				Blt(l12);
				PopStack();
				Stloc(_runtextposLocal);
				PushTrack(rentedLocalBuilder11);
				TrackUnique2(7);
				Advance();
				MarkLabel(l12);
				ReadyReplaceStack(0);
				PopTrack();
				DoReplace();
				PushStack(rentedLocalBuilder11);
			}
			Back();
			break;
		}
		case 284:
		{
			PopTrack();
			using (RentedLocalBuilder rentedLocalBuilder12 = RentInt32Local())
			{
				Stloc(rentedLocalBuilder12);
				ReadyPushStack();
				PopTrack();
				DoPush();
				PushStack(rentedLocalBuilder12);
			}
			Back();
			break;
		}
		case 29:
		{
			PopStack();
			using (RentedLocalBuilder rentedLocalBuilder8 = RentInt32Local())
			{
				Stloc(rentedLocalBuilder8);
				PopStack();
				using (RentedLocalBuilder rentedLocalBuilder9 = RentInt32Local())
				{
					Stloc(rentedLocalBuilder9);
					Label l10 = DefineLabel();
					Ldloc(rentedLocalBuilder8);
					Ldc(0);
					Bge(l10);
					PushTrack(rentedLocalBuilder9);
					PushStack(_runtextposLocal);
					ReadyPushStack();
					Ldloc(rentedLocalBuilder8);
					Ldc(1);
					Add();
					DoPush();
					TrackUnique2(8);
					Goto(Operand(0));
					MarkLabel(l10);
					PushTrack(rentedLocalBuilder9);
				}
				PushTrack(rentedLocalBuilder8);
			}
			PushTrack(_runtextposLocal);
			Track();
			break;
		}
		case 157:
		{
			using (RentedLocalBuilder rentedLocalBuilder4 = RentInt32Local())
			{
				Label l4 = DefineLabel();
				PopTrack();
				Stloc(_runtextposLocal);
				PopTrack();
				Stloc(rentedLocalBuilder4);
				Ldloc(rentedLocalBuilder4);
				Ldc(Operand(1));
				Bge(l4);
				Ldloc(_runtextposLocal);
				TopTrack();
				Beq(l4);
				PushStack(_runtextposLocal);
				ReadyPushStack();
				Ldloc(rentedLocalBuilder4);
				Ldc(1);
				Add();
				DoPush();
				TrackUnique2(8);
				Goto(Operand(0));
				MarkLabel(l4);
				ReadyPushStack();
				PopTrack();
				DoPush();
				PushStack(rentedLocalBuilder4);
			}
			Back();
			break;
		}
		case 285:
			ReadyReplaceStack(1);
			PopTrack();
			DoReplace();
			ReadyReplaceStack(0);
			TopStack();
			Ldc(1);
			Sub();
			DoReplace();
			Back();
			break;
		case 34:
			ReadyPushStack();
			Ldthisfld(s_runtrackField);
			Ldlen();
			Ldloc(_runtrackposLocal);
			Sub();
			DoPush();
			ReadyPushStack();
			Ldthis();
			Call(s_crawlposMethod);
			DoPush();
			TrackUnique(1);
			break;
		case 162:
			PopDiscardStack(2);
			Back();
			break;
		case 35:
		{
			Label l = DefineLabel();
			Label l2 = DefineLabel();
			using (RentedLocalBuilder rentedLocalBuilder2 = RentInt32Local())
			{
				PopStack();
				Stloc(rentedLocalBuilder2);
				Ldthisfld(s_runtrackField);
				Ldlen();
				PopStack();
				Sub();
				Stloc(_runtrackposLocal);
				MarkLabel(l);
				Ldthis();
				Call(s_crawlposMethod);
				Ldloc(rentedLocalBuilder2);
				Beq(l2);
				Ldthis();
				Call(s_uncaptureMethod);
				Br(l);
			}
			MarkLabel(l2);
			Back();
			break;
		}
		case 36:
		{
			PopStack();
			using (RentedLocalBuilder rentedLocalBuilder21 = RentInt32Local())
			{
				Stloc(rentedLocalBuilder21);
				Ldthisfld(s_runtrackField);
				Ldlen();
				PopStack();
				Sub();
				Stloc(_runtrackposLocal);
				PushTrack(rentedLocalBuilder21);
			}
			TrackUnique(9);
			break;
		}
		case 164:
		{
			Label l22 = DefineLabel();
			Label l23 = DefineLabel();
			using (RentedLocalBuilder rentedLocalBuilder19 = RentInt32Local())
			{
				PopTrack();
				Stloc(rentedLocalBuilder19);
				MarkLabel(l22);
				Ldthis();
				Call(s_crawlposMethod);
				Ldloc(rentedLocalBuilder19);
				Beq(l23);
				Ldthis();
				Call(s_uncaptureMethod);
				Br(l22);
			}
			MarkLabel(l23);
			Back();
			break;
		}
		case 14:
		{
			Label l21 = _labels[NextCodepos()];
			Ldloc(_runtextposLocal);
			Ldloc(_runtextbegLocal);
			Ble(l21);
			Leftchar();
			Ldc(10);
			BneFar(_backtrack);
			break;
		}
		case 15:
		{
			Label l20 = _labels[NextCodepos()];
			Ldloc(_runtextposLocal);
			Ldloc(_runtextendLocal);
			Bge(l20);
			Rightchar();
			Ldc(10);
			BneFar(_backtrack);
			break;
		}
		case 16:
		case 17:
			Ldthis();
			Ldloc(_runtextposLocal);
			Ldloc(_runtextbegLocal);
			Ldloc(_runtextendLocal);
			Call(s_isBoundaryMethod);
			if (Code() == 16)
			{
				BrfalseFar(_backtrack);
			}
			else
			{
				BrtrueFar(_backtrack);
			}
			break;
		case 41:
		case 42:
			Ldthis();
			Ldloc(_runtextposLocal);
			Ldloc(_runtextbegLocal);
			Ldloc(_runtextendLocal);
			Call(s_isECMABoundaryMethod);
			if (Code() == 41)
			{
				BrfalseFar(_backtrack);
			}
			else
			{
				BrtrueFar(_backtrack);
			}
			break;
		case 18:
			Ldloc(_runtextposLocal);
			Ldloc(_runtextbegLocal);
			BgtFar(_backtrack);
			break;
		case 19:
			Ldloc(_runtextposLocal);
			Ldthisfld(s_runtextstartField);
			BneFar(_backtrack);
			break;
		case 20:
			Ldloc(_runtextposLocal);
			Ldloc(_runtextendLocal);
			Ldc(1);
			Sub();
			BltFar(_backtrack);
			Ldloc(_runtextposLocal);
			Ldloc(_runtextendLocal);
			Bge(_labels[NextCodepos()]);
			Rightchar();
			Ldc(10);
			BneFar(_backtrack);
			break;
		case 21:
			Ldloc(_runtextposLocal);
			Ldloc(_runtextendLocal);
			BltFar(_backtrack);
			break;
		case 9:
		case 10:
		case 11:
		case 73:
		case 74:
		case 75:
		case 521:
		case 522:
		case 523:
		case 585:
		case 586:
		case 587:
			Ldloc(_runtextposLocal);
			if (!IsRightToLeft())
			{
				Ldloc(_runtextendLocal);
				BgeFar(_backtrack);
				Rightcharnext();
			}
			else
			{
				Ldloc(_runtextbegLocal);
				BleFar(_backtrack);
				Leftcharnext();
			}
			if (Code() == 11)
			{
				EmitMatchCharacterClass(_strings[Operand(0)], IsCaseInsensitive());
				BrfalseFar(_backtrack);
				break;
			}
			if (IsCaseInsensitive() && ParticipatesInCaseConversion(Operand(0)))
			{
				CallToLower();
			}
			Ldc(Operand(0));
			if (Code() == 9)
			{
				BneFar(_backtrack);
			}
			else
			{
				BeqFar(_backtrack);
			}
			break;
		case 12:
		case 524:
		{
			string text3 = _strings[Operand(0)];
			Ldc(text3.Length);
			Ldloc(_runtextendLocal);
			Ldloc(_runtextposLocal);
			Sub();
			BgtFar(_backtrack);
			for (int i = 0; i < text3.Length; i++)
			{
				Ldloc(_runtextLocal);
				Ldloc(_runtextposLocal);
				if (i != 0)
				{
					Ldc(i);
					Add();
				}
				Call(s_stringGetCharsMethod);
				if (IsCaseInsensitive() && ParticipatesInCaseConversion(text3[i]))
				{
					CallToLower();
				}
				Ldc(text3[i]);
				BneFar(_backtrack);
			}
			Ldloc(_runtextposLocal);
			Ldc(text3.Length);
			Add();
			Stloc(_runtextposLocal);
			break;
		}
		case 76:
		case 588:
		{
			string text2 = _strings[Operand(0)];
			Ldc(text2.Length);
			Ldloc(_runtextposLocal);
			Ldloc(_runtextbegLocal);
			Sub();
			BgtFar(_backtrack);
			int num4 = text2.Length;
			while (num4 > 0)
			{
				num4--;
				Ldloc(_runtextLocal);
				Ldloc(_runtextposLocal);
				Ldc(text2.Length - num4);
				Sub();
				Call(s_stringGetCharsMethod);
				if (IsCaseInsensitive() && ParticipatesInCaseConversion(text2[num4]))
				{
					CallToLower();
				}
				Ldc(text2[num4]);
				BneFar(_backtrack);
			}
			Ldloc(_runtextposLocal);
			Ldc(text2.Length);
			Sub();
			Stloc(_runtextposLocal);
			break;
		}
		case 13:
		case 77:
		case 525:
		case 589:
		{
			using RentedLocalBuilder rentedLocalBuilder13 = RentInt32Local();
			using RentedLocalBuilder rentedLocalBuilder14 = RentInt32Local();
			Label l13 = DefineLabel();
			Ldthis();
			Ldc(Operand(0));
			Call(s_isMatchedMethod);
			if ((_options & RegexOptions.ECMAScript) != 0)
			{
				Brfalse(AdvanceLabel());
			}
			else
			{
				BrfalseFar(_backtrack);
			}
			Ldthis();
			Ldc(Operand(0));
			Call(s_matchLengthMethod);
			Stloc(rentedLocalBuilder13);
			Ldloc(rentedLocalBuilder13);
			if (!IsRightToLeft())
			{
				Ldloc(_runtextendLocal);
				Ldloc(_runtextposLocal);
			}
			else
			{
				Ldloc(_runtextposLocal);
				Ldloc(_runtextbegLocal);
			}
			Sub();
			BgtFar(_backtrack);
			Ldthis();
			Ldc(Operand(0));
			Call(s_matchIndexMethod);
			if (!IsRightToLeft())
			{
				Ldloc(rentedLocalBuilder13);
				Add(IsRightToLeft());
			}
			Stloc(rentedLocalBuilder14);
			Ldloc(_runtextposLocal);
			Ldloc(rentedLocalBuilder13);
			Add(IsRightToLeft());
			Stloc(_runtextposLocal);
			MarkLabel(l13);
			Ldloc(rentedLocalBuilder13);
			Ldc(0);
			Ble(AdvanceLabel());
			Ldloc(_runtextLocal);
			Ldloc(rentedLocalBuilder14);
			Ldloc(rentedLocalBuilder13);
			if (IsRightToLeft())
			{
				Ldc(1);
				Sub();
				Stloc(rentedLocalBuilder13);
				Ldloc(rentedLocalBuilder13);
			}
			Sub(IsRightToLeft());
			Call(s_stringGetCharsMethod);
			if (IsCaseInsensitive())
			{
				CallToLower();
			}
			Ldloc(_runtextLocal);
			Ldloc(_runtextposLocal);
			Ldloc(rentedLocalBuilder13);
			if (!IsRightToLeft())
			{
				Ldloc(rentedLocalBuilder13);
				Ldc(1);
				Sub();
				Stloc(rentedLocalBuilder13);
			}
			Sub(IsRightToLeft());
			Call(s_stringGetCharsMethod);
			if (IsCaseInsensitive())
			{
				CallToLower();
			}
			Beq(l13);
			Back();
			break;
		}
		case 0:
		case 1:
		case 2:
		case 64:
		case 65:
		case 66:
		case 512:
		case 513:
		case 514:
		case 576:
		case 577:
		case 578:
		{
			int num3 = Operand(1);
			if (num3 == 0)
			{
				break;
			}
			Ldc(num3);
			if (!IsRightToLeft())
			{
				Ldloc(_runtextendLocal);
				Ldloc(_runtextposLocal);
			}
			else
			{
				Ldloc(_runtextposLocal);
				Ldloc(_runtextbegLocal);
			}
			Sub();
			BgtFar(_backtrack);
			Ldloc(_runtextposLocal);
			Ldc(num3);
			Add(IsRightToLeft());
			Stloc(_runtextposLocal);
			using RentedLocalBuilder rentedLocalBuilder10 = RentInt32Local();
			Label l11 = DefineLabel();
			Ldc(num3);
			Stloc(rentedLocalBuilder10);
			MarkLabel(l11);
			Ldloc(_runtextLocal);
			Ldloc(_runtextposLocal);
			Ldloc(rentedLocalBuilder10);
			if (IsRightToLeft())
			{
				Ldc(1);
				Sub();
				Stloc(rentedLocalBuilder10);
				Ldloc(rentedLocalBuilder10);
				Add();
			}
			else
			{
				Ldloc(rentedLocalBuilder10);
				Ldc(1);
				Sub();
				Stloc(rentedLocalBuilder10);
				Sub();
			}
			Call(s_stringGetCharsMethod);
			if (Code() == 2)
			{
				EmitTimeoutCheck();
				EmitMatchCharacterClass(_strings[Operand(0)], IsCaseInsensitive());
				BrfalseFar(_backtrack);
			}
			else
			{
				if (IsCaseInsensitive() && ParticipatesInCaseConversion(Operand(0)))
				{
					CallToLower();
				}
				Ldc(Operand(0));
				if (Code() == 0)
				{
					BneFar(_backtrack);
				}
				else
				{
					BeqFar(_backtrack);
				}
			}
			Ldloc(rentedLocalBuilder10);
			Ldc(0);
			if (Code() == 2)
			{
				BgtFar(l11);
			}
			else
			{
				Bgt(l11);
			}
			break;
		}
		case 3:
		case 4:
		case 5:
		case 43:
		case 44:
		case 45:
		case 67:
		case 68:
		case 69:
		case 107:
		case 108:
		case 109:
		case 515:
		case 516:
		case 517:
		case 555:
		case 556:
		case 557:
		case 579:
		case 580:
		case 581:
		case 619:
		case 620:
		case 621:
		{
			int num2 = Operand(1);
			if (num2 == 0)
			{
				break;
			}
			using RentedLocalBuilder rentedLocalBuilder6 = RentInt32Local();
			using RentedLocalBuilder rentedLocalBuilder7 = RentInt32Local();
			if (!IsRightToLeft())
			{
				Ldloc(_runtextendLocal);
				Ldloc(_runtextposLocal);
			}
			else
			{
				Ldloc(_runtextposLocal);
				Ldloc(_runtextbegLocal);
			}
			Sub();
			Stloc(rentedLocalBuilder6);
			if (num2 != int.MaxValue)
			{
				Label l5 = DefineLabel();
				Ldloc(rentedLocalBuilder6);
				Ldc(num2);
				Blt(l5);
				Ldc(num2);
				Stloc(rentedLocalBuilder6);
				MarkLabel(l5);
			}
			Label l6 = DefineLabel();
			string text = ((Code() == 5 || Code() == 45) ? _strings[Operand(0)] : null);
			Span<char> chars = stackalloc char[3];
			int setChars;
			if ((Code() == 4 || Code() == 44) && !IsRightToLeft() && (!IsCaseInsensitive() || !ParticipatesInCaseConversion(Operand(0))))
			{
				Ldloc(_runtextLocal);
				Ldloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder6);
				Call(s_stringAsSpanIntIntMethod);
				Ldc(Operand(0));
				Call(s_spanIndexOf);
				Stloc(rentedLocalBuilder7);
				Label l7 = DefineLabel();
				Ldloc(rentedLocalBuilder7);
				Ldc(-1);
				Bne(l7);
				Ldloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder6);
				Add();
				Stloc(_runtextposLocal);
				Ldc(0);
				Stloc(rentedLocalBuilder7);
				BrFar(l6);
				MarkLabel(l7);
				Ldloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder7);
				Add();
				Stloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder6);
				Ldloc(rentedLocalBuilder7);
				Sub();
				Stloc(rentedLocalBuilder7);
				BrFar(l6);
			}
			else if ((Code() == 5 || Code() == 45) && !IsRightToLeft() && !IsCaseInsensitive() && (setChars = RegexCharClass.GetSetChars(text, chars)) > 1 && RegexCharClass.IsNegated(text))
			{
				Ldloc(_runtextLocal);
				Ldloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder6);
				Call(s_stringAsSpanIntIntMethod);
				Ldc(chars[0]);
				Ldc(chars[1]);
				if (setChars == 2)
				{
					Call(s_spanIndexOfAnyCharChar);
				}
				else
				{
					Ldc(chars[2]);
					Call(s_spanIndexOfAnyCharCharChar);
				}
				Stloc(rentedLocalBuilder7);
				Label l8 = DefineLabel();
				Ldloc(rentedLocalBuilder7);
				Ldc(-1);
				Bne(l8);
				Ldloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder6);
				Add();
				Stloc(_runtextposLocal);
				Ldc(0);
				Stloc(rentedLocalBuilder7);
				BrFar(l6);
				MarkLabel(l8);
				Ldloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder7);
				Add();
				Stloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder6);
				Ldloc(rentedLocalBuilder7);
				Sub();
				Stloc(rentedLocalBuilder7);
				BrFar(l6);
			}
			else if ((Code() == 5 || Code() == 45) && !IsRightToLeft() && text == "\0\u0001\0\0")
			{
				Ldloc(_runtextposLocal);
				Ldloc(rentedLocalBuilder6);
				Add();
				Stloc(_runtextposLocal);
				Ldc(0);
				Stloc(rentedLocalBuilder7);
				BrFar(l6);
			}
			else
			{
				Ldloc(rentedLocalBuilder6);
				Ldc(1);
				Add();
				Stloc(rentedLocalBuilder7);
				Label l9 = DefineLabel();
				MarkLabel(l9);
				Ldloc(rentedLocalBuilder7);
				Ldc(1);
				Sub();
				Stloc(rentedLocalBuilder7);
				Ldloc(rentedLocalBuilder7);
				Ldc(0);
				if (Code() == 5 || Code() == 45)
				{
					BleFar(l6);
				}
				else
				{
					Ble(l6);
				}
				if (IsRightToLeft())
				{
					Leftcharnext();
				}
				else
				{
					Rightcharnext();
				}
				if (Code() == 5 || Code() == 45)
				{
					EmitTimeoutCheck();
					EmitMatchCharacterClass(_strings[Operand(0)], IsCaseInsensitive());
					BrtrueFar(l9);
				}
				else
				{
					if (IsCaseInsensitive() && ParticipatesInCaseConversion(Operand(0)))
					{
						CallToLower();
					}
					Ldc(Operand(0));
					if (Code() == 3 || Code() == 43)
					{
						Beq(l9);
					}
					else
					{
						Bne(l9);
					}
				}
				Ldloc(_runtextposLocal);
				Ldc(1);
				Sub(IsRightToLeft());
				Stloc(_runtextposLocal);
			}
			MarkLabel(l6);
			if (Code() != 43 && Code() != 44 && Code() != 45)
			{
				Ldloc(rentedLocalBuilder6);
				Ldloc(rentedLocalBuilder7);
				Ble(AdvanceLabel());
				ReadyPushTrack();
				Ldloc(rentedLocalBuilder6);
				Ldloc(rentedLocalBuilder7);
				Sub();
				Ldc(1);
				Sub();
				DoPush();
				ReadyPushTrack();
				Ldloc(_runtextposLocal);
				Ldc(1);
				Sub(IsRightToLeft());
				DoPush();
				Track();
			}
			break;
		}
		case 131:
		case 132:
		case 133:
		case 195:
		case 196:
		case 197:
		case 643:
		case 644:
		case 645:
		case 707:
		case 708:
		case 709:
		{
			PopTrack();
			Stloc(_runtextposLocal);
			PopTrack();
			using (RentedLocalBuilder rentedLocalBuilder5 = RentInt32Local())
			{
				Stloc(rentedLocalBuilder5);
				Ldloc(rentedLocalBuilder5);
				Ldc(0);
				BleFar(AdvanceLabel());
				ReadyPushTrack();
				Ldloc(rentedLocalBuilder5);
			}
			Ldc(1);
			Sub();
			DoPush();
			ReadyPushTrack();
			Ldloc(_runtextposLocal);
			Ldc(1);
			Sub(IsRightToLeft());
			DoPush();
			Trackagain();
			Advance();
			break;
		}
		case 6:
		case 7:
		case 8:
		case 70:
		case 71:
		case 72:
		case 518:
		case 519:
		case 520:
		case 582:
		case 583:
		case 584:
		{
			int num = Operand(1);
			if (num == 0)
			{
				break;
			}
			if (!IsRightToLeft())
			{
				Ldloc(_runtextendLocal);
				Ldloc(_runtextposLocal);
			}
			else
			{
				Ldloc(_runtextposLocal);
				Ldloc(_runtextbegLocal);
			}
			Sub();
			using (RentedLocalBuilder rentedLocalBuilder3 = RentInt32Local())
			{
				Stloc(rentedLocalBuilder3);
				if (num != int.MaxValue)
				{
					Label l3 = DefineLabel();
					Ldloc(rentedLocalBuilder3);
					Ldc(num);
					Blt(l3);
					Ldc(num);
					Stloc(rentedLocalBuilder3);
					MarkLabel(l3);
				}
				Ldloc(rentedLocalBuilder3);
				Ldc(0);
				Ble(AdvanceLabel());
				ReadyPushTrack();
				Ldloc(rentedLocalBuilder3);
			}
			Ldc(1);
			Sub();
			DoPush();
			PushTrack(_runtextposLocal);
			Track();
			break;
		}
		case 134:
		case 135:
		case 136:
		case 198:
		case 199:
		case 200:
		case 646:
		case 647:
		case 648:
		case 710:
		case 711:
		case 712:
		{
			PopTrack();
			Stloc(_runtextposLocal);
			PopTrack();
			using (RentedLocalBuilder rentedLocalBuilder = RentInt32Local())
			{
				Stloc(rentedLocalBuilder);
				if (!IsRightToLeft())
				{
					Rightcharnext();
				}
				else
				{
					Leftcharnext();
				}
				if (Code() == 8)
				{
					EmitMatchCharacterClass(_strings[Operand(0)], IsCaseInsensitive());
					BrfalseFar(_backtrack);
				}
				else
				{
					if (IsCaseInsensitive() && ParticipatesInCaseConversion(Operand(0)))
					{
						CallToLower();
					}
					Ldc(Operand(0));
					if (Code() == 6)
					{
						BneFar(_backtrack);
					}
					else
					{
						BeqFar(_backtrack);
					}
				}
				Ldloc(rentedLocalBuilder);
				Ldc(0);
				BleFar(AdvanceLabel());
				ReadyPushTrack();
				Ldloc(rentedLocalBuilder);
			}
			Ldc(1);
			Sub();
			DoPush();
			PushTrack(_runtextposLocal);
			Trackagain();
			Advance();
			break;
		}
		}
	}

	private void EmitMatchCharacterClass(string charClass, bool caseInsensitive)
	{
		switch (charClass)
		{
		case "\0\u0001\0\0":
			Pop();
			Ldc(1);
			return;
		case "\0\0\u0001\t":
			Call(s_charIsDigitMethod);
			return;
		case "\0\0\u0001\ufff7":
			Call(s_charIsDigitMethod);
			Ldc(0);
			Ceq();
			return;
		case "\0\0\u0001d":
			Call(s_charIsWhiteSpaceMethod);
			return;
		case "\u0001\0\u0001d":
			Call(s_charIsWhiteSpaceMethod);
			Ldc(0);
			Ceq();
			return;
		}
		bool invariant = false;
		if (caseInsensitive)
		{
			invariant = UseToLowerInvariant;
			if (!invariant)
			{
				CallToLower();
			}
		}
		if (!invariant && RegexCharClass.TryGetSingleRange(charClass, out var lowInclusive, out var highInclusive))
		{
			if (lowInclusive == highInclusive)
			{
				Ldc(lowInclusive);
				Ceq();
			}
			else
			{
				Ldc(lowInclusive);
				Sub();
				Ldc(highInclusive - lowInclusive + 1);
				CltUn();
			}
			if (RegexCharClass.IsNegated(charClass))
			{
				Ldc(0);
				Ceq();
			}
			return;
		}
		if (!invariant && RegexCharClass.TryGetSingleUnicodeCategory(charClass, out var category, out var negated))
		{
			Call(s_charGetUnicodeInfo);
			Ldc((int)category);
			Ceq();
			if (negated)
			{
				Ldc(0);
				Ceq();
			}
			return;
		}
		RentedLocalBuilder tempLocal = RentInt32Local();
		RentedLocalBuilder resultLocal;
		try
		{
			Stloc(tempLocal);
			if (!invariant)
			{
				Span<char> chars = stackalloc char[3];
				int setChars = RegexCharClass.GetSetChars(charClass, chars);
				if (setChars > 0 && !RegexCharClass.IsNegated(charClass))
				{
					Ldloc(tempLocal);
					Ldc(chars[0]);
					Ceq();
					Ldloc(tempLocal);
					Ldc(chars[1]);
					Ceq();
					Or();
					if (setChars == 3)
					{
						Ldloc(tempLocal);
						Ldc(chars[2]);
						Ceq();
						Or();
					}
					return;
				}
			}
			resultLocal = RentInt32Local();
			try
			{
				RegexCharClass.CharClassAnalysisResults charClassAnalysisResults = RegexCharClass.Analyze(charClass);
				Label l = DefineLabel();
				Label l2 = DefineLabel();
				if (!invariant)
				{
					if (charClassAnalysisResults.ContainsNoAscii)
					{
						Ldloc(tempLocal);
						Ldc(128);
						Blt(l2);
						EmitCharInClass();
						Br(l);
						MarkLabel(l2);
						Ldc(0);
						Stloc(resultLocal);
						MarkLabel(l);
						Ldloc(resultLocal);
						return;
					}
					if (charClassAnalysisResults.AllAsciiContained)
					{
						Ldloc(tempLocal);
						Ldc(128);
						Blt(l2);
						EmitCharInClass();
						Br(l);
						MarkLabel(l2);
						Ldc(1);
						Stloc(resultLocal);
						MarkLabel(l);
						Ldloc(resultLocal);
						return;
					}
				}
				string str = string.Create(8, (charClass, invariant), delegate(Span<char> dest, (string charClass, bool invariant) state)
				{
					for (int i = 0; i < 128; i++)
					{
						char c = (char)i;
						if (state.invariant ? RegexCharClass.CharInClass(char.ToLowerInvariant(c), state.charClass) : RegexCharClass.CharInClass(c, state.charClass))
						{
							dest[i >> 4] |= (char)(ushort)(1 << (i & 0xF));
						}
					}
				});
				Ldloc(tempLocal);
				Ldc(128);
				Bge(l2);
				Ldstr(str);
				Ldloc(tempLocal);
				Ldc(4);
				Shr();
				Call(s_stringGetCharsMethod);
				Ldc(1);
				Ldloc(tempLocal);
				Ldc(15);
				And();
				Ldc(31);
				And();
				Shl();
				And();
				Ldc(0);
				CgtUn();
				Stloc(resultLocal);
				Br(l);
				MarkLabel(l2);
				if (charClassAnalysisResults.ContainsOnlyAscii)
				{
					Ldc(0);
					Stloc(resultLocal);
				}
				else if (charClassAnalysisResults.AllNonAsciiContained)
				{
					Ldc(1);
					Stloc(resultLocal);
				}
				else
				{
					EmitCharInClass();
				}
				MarkLabel(l);
				Ldloc(resultLocal);
			}
			finally
			{
				((IDisposable)resultLocal).Dispose();
			}
		}
		finally
		{
			((IDisposable)tempLocal).Dispose();
		}
		void EmitCharInClass()
		{
			Ldloc(tempLocal);
			if (invariant)
			{
				CallToLower();
			}
			Ldstr(charClass);
			Call(s_charInClassMethod);
			Stloc(resultLocal);
		}
	}

	private void EmitTimeoutCheck()
	{
		if (_hasTimeout)
		{
			Ldloc(_loopTimeoutCounterLocal);
			Ldc(1);
			Add();
			Stloc(_loopTimeoutCounterLocal);
			Label l = DefineLabel();
			Ldloc(_loopTimeoutCounterLocal);
			Ldc(2048);
			RemUn();
			Brtrue(l);
			Ldthis();
			Call(s_checkTimeoutMethod);
			MarkLabel(l);
		}
	}
}
