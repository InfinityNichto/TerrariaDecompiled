using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using Internal.Win32;

namespace System;

[Serializable]
[TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class TimeZoneInfo : IEquatable<TimeZoneInfo?>, ISerializable, IDeserializationCallback
{
	[Serializable]
	public sealed class AdjustmentRule : IEquatable<AdjustmentRule?>, ISerializable, IDeserializationCallback
	{
		private static readonly TimeSpan DaylightDeltaAdjustment = TimeSpan.FromHours(24.0);

		private static readonly TimeSpan MaxDaylightDelta = TimeSpan.FromHours(12.0);

		private readonly DateTime _dateStart;

		private readonly DateTime _dateEnd;

		private readonly TimeSpan _daylightDelta;

		private readonly TransitionTime _daylightTransitionStart;

		private readonly TransitionTime _daylightTransitionEnd;

		private readonly TimeSpan _baseUtcOffsetDelta;

		private readonly bool _noDaylightTransitions;

		public DateTime DateStart => _dateStart;

		public DateTime DateEnd => _dateEnd;

		public TimeSpan DaylightDelta => _daylightDelta;

		public TransitionTime DaylightTransitionStart => _daylightTransitionStart;

		public TransitionTime DaylightTransitionEnd => _daylightTransitionEnd;

		public TimeSpan BaseUtcOffsetDelta => _baseUtcOffsetDelta;

		internal bool NoDaylightTransitions => _noDaylightTransitions;

		internal bool HasDaylightSaving
		{
			get
			{
				if (!(DaylightDelta != TimeSpan.Zero) && (!(DaylightTransitionStart != default(TransitionTime)) || !(DaylightTransitionStart.TimeOfDay != DateTime.MinValue)))
				{
					if (DaylightTransitionEnd != default(TransitionTime))
					{
						return DaylightTransitionEnd.TimeOfDay != DateTime.MinValue.AddMilliseconds(1.0);
					}
					return false;
				}
				return true;
			}
		}

		public bool Equals([NotNullWhen(true)] AdjustmentRule? other)
		{
			if (other != null && _dateStart == other._dateStart && _dateEnd == other._dateEnd && _daylightDelta == other._daylightDelta && _baseUtcOffsetDelta == other._baseUtcOffsetDelta && _daylightTransitionEnd.Equals(other._daylightTransitionEnd))
			{
				return _daylightTransitionStart.Equals(other._daylightTransitionStart);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return _dateStart.GetHashCode();
		}

		private AdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd, TimeSpan baseUtcOffsetDelta, bool noDaylightTransitions)
		{
			ValidateAdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd, noDaylightTransitions);
			_dateStart = dateStart;
			_dateEnd = dateEnd;
			_daylightDelta = daylightDelta;
			_daylightTransitionStart = daylightTransitionStart;
			_daylightTransitionEnd = daylightTransitionEnd;
			_baseUtcOffsetDelta = baseUtcOffsetDelta;
			_noDaylightTransitions = noDaylightTransitions;
		}

		public static AdjustmentRule CreateAdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd, TimeSpan baseUtcOffsetDelta)
		{
			return new AdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd, baseUtcOffsetDelta, noDaylightTransitions: false);
		}

		public static AdjustmentRule CreateAdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd)
		{
			return new AdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd, TimeSpan.Zero, noDaylightTransitions: false);
		}

		internal static AdjustmentRule CreateAdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd, TimeSpan baseUtcOffsetDelta, bool noDaylightTransitions)
		{
			AdjustDaylightDeltaToExpectedRange(ref daylightDelta, ref baseUtcOffsetDelta);
			return new AdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd, baseUtcOffsetDelta, noDaylightTransitions);
		}

		internal bool IsStartDateMarkerForBeginningOfYear()
		{
			if (!NoDaylightTransitions && DaylightTransitionStart.Month == 1 && DaylightTransitionStart.Day == 1)
			{
				return DaylightTransitionStart.TimeOfDay.TimeOfDay.Ticks < 10000000;
			}
			return false;
		}

		internal bool IsEndDateMarkerForEndOfYear()
		{
			if (!NoDaylightTransitions && DaylightTransitionEnd.Month == 1 && DaylightTransitionEnd.Day == 1)
			{
				return DaylightTransitionEnd.TimeOfDay.TimeOfDay.Ticks < 10000000;
			}
			return false;
		}

		private static void ValidateAdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd, bool noDaylightTransitions)
		{
			if (dateStart.Kind != 0 && dateStart.Kind != DateTimeKind.Utc)
			{
				throw new ArgumentException(SR.Argument_DateTimeKindMustBeUnspecifiedOrUtc, "dateStart");
			}
			if (dateEnd.Kind != 0 && dateEnd.Kind != DateTimeKind.Utc)
			{
				throw new ArgumentException(SR.Argument_DateTimeKindMustBeUnspecifiedOrUtc, "dateEnd");
			}
			if (daylightTransitionStart.Equals(daylightTransitionEnd) && !noDaylightTransitions)
			{
				throw new ArgumentException(SR.Argument_TransitionTimesAreIdentical, "daylightTransitionEnd");
			}
			if (dateStart > dateEnd)
			{
				throw new ArgumentException(SR.Argument_OutOfOrderDateTimes, "dateStart");
			}
			if (daylightDelta.TotalHours < -23.0 || daylightDelta.TotalHours > 14.0)
			{
				throw new ArgumentOutOfRangeException("daylightDelta", daylightDelta, SR.ArgumentOutOfRange_UtcOffset);
			}
			if (daylightDelta.Ticks % 600000000 != 0L)
			{
				throw new ArgumentException(SR.Argument_TimeSpanHasSeconds, "daylightDelta");
			}
			if (dateStart != DateTime.MinValue && dateStart.Kind == DateTimeKind.Unspecified && dateStart.TimeOfDay != TimeSpan.Zero)
			{
				throw new ArgumentException(SR.Argument_DateTimeHasTimeOfDay, "dateStart");
			}
			if (dateEnd != DateTime.MaxValue && dateEnd.Kind == DateTimeKind.Unspecified && dateEnd.TimeOfDay != TimeSpan.Zero)
			{
				throw new ArgumentException(SR.Argument_DateTimeHasTimeOfDay, "dateEnd");
			}
		}

		private static void AdjustDaylightDeltaToExpectedRange(ref TimeSpan daylightDelta, ref TimeSpan baseUtcOffsetDelta)
		{
			if (daylightDelta > MaxDaylightDelta)
			{
				daylightDelta -= DaylightDeltaAdjustment;
				baseUtcOffsetDelta += DaylightDeltaAdjustment;
			}
			else if (daylightDelta < -MaxDaylightDelta)
			{
				daylightDelta += DaylightDeltaAdjustment;
				baseUtcOffsetDelta -= DaylightDeltaAdjustment;
			}
		}

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			try
			{
				ValidateAdjustmentRule(_dateStart, _dateEnd, _daylightDelta, _daylightTransitionStart, _daylightTransitionEnd, _noDaylightTransitions);
			}
			catch (ArgumentException innerException)
			{
				throw new SerializationException(SR.Serialization_InvalidData, innerException);
			}
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.AddValue("DateStart", _dateStart);
			info.AddValue("DateEnd", _dateEnd);
			info.AddValue("DaylightDelta", _daylightDelta);
			info.AddValue("DaylightTransitionStart", _daylightTransitionStart);
			info.AddValue("DaylightTransitionEnd", _daylightTransitionEnd);
			info.AddValue("BaseUtcOffsetDelta", _baseUtcOffsetDelta);
			info.AddValue("NoDaylightTransitions", _noDaylightTransitions);
		}

		private AdjustmentRule(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			_dateStart = (DateTime)info.GetValue("DateStart", typeof(DateTime));
			_dateEnd = (DateTime)info.GetValue("DateEnd", typeof(DateTime));
			_daylightDelta = (TimeSpan)info.GetValue("DaylightDelta", typeof(TimeSpan));
			_daylightTransitionStart = (TransitionTime)info.GetValue("DaylightTransitionStart", typeof(TransitionTime));
			_daylightTransitionEnd = (TransitionTime)info.GetValue("DaylightTransitionEnd", typeof(TransitionTime));
			object valueNoThrow = info.GetValueNoThrow("BaseUtcOffsetDelta", typeof(TimeSpan));
			if (valueNoThrow != null)
			{
				_baseUtcOffsetDelta = (TimeSpan)valueNoThrow;
			}
			valueNoThrow = info.GetValueNoThrow("NoDaylightTransitions", typeof(bool));
			if (valueNoThrow != null)
			{
				_noDaylightTransitions = (bool)valueNoThrow;
			}
		}
	}

	private enum TimeZoneInfoResult
	{
		Success,
		TimeZoneNotFoundException,
		InvalidTimeZoneException,
		SecurityException
	}

	private sealed class CachedData
	{
		private volatile TimeZoneInfo _localTimeZone;

		public Dictionary<string, TimeZoneInfo> _systemTimeZones;

		public ReadOnlyCollection<TimeZoneInfo> _readOnlySystemTimeZones;

		public bool _allSystemTimeZonesRead;

		private volatile OffsetAndRule _oneYearLocalFromUtc;

		public TimeZoneInfo Local => _localTimeZone ?? CreateLocal();

		private TimeZoneInfo CreateLocal()
		{
			lock (this)
			{
				TimeZoneInfo timeZoneInfo = _localTimeZone;
				if (timeZoneInfo == null)
				{
					timeZoneInfo = GetLocalTimeZone(this);
					timeZoneInfo = (_localTimeZone = new TimeZoneInfo(timeZoneInfo._id, timeZoneInfo._baseUtcOffset, timeZoneInfo._displayName, timeZoneInfo._standardDisplayName, timeZoneInfo._daylightDisplayName, timeZoneInfo._adjustmentRules, disableDaylightSavingTime: false, timeZoneInfo.HasIanaId));
				}
				return timeZoneInfo;
			}
		}

		public DateTimeKind GetCorrespondingKind(TimeZoneInfo timeZone)
		{
			if (timeZone != s_utcTimeZone)
			{
				if (timeZone != _localTimeZone)
				{
					return DateTimeKind.Unspecified;
				}
				return DateTimeKind.Local;
			}
			return DateTimeKind.Utc;
		}

		private static TimeZoneInfo GetCurrentOneYearLocal()
		{
			Interop.Kernel32.TIME_ZONE_INFORMATION lpTimeZoneInformation;
			uint timeZoneInformation = Interop.Kernel32.GetTimeZoneInformation(out lpTimeZoneInformation);
			if (timeZoneInformation != uint.MaxValue)
			{
				return GetLocalTimeZoneFromWin32Data(in lpTimeZoneInformation, dstDisabled: false);
			}
			return CreateCustomTimeZone("Local", TimeSpan.Zero, "Local", "Local");
		}

		public OffsetAndRule GetOneYearLocalFromUtc(int year)
		{
			OffsetAndRule offsetAndRule = _oneYearLocalFromUtc;
			if (offsetAndRule == null || offsetAndRule.Year != year)
			{
				TimeZoneInfo currentOneYearLocal = GetCurrentOneYearLocal();
				AdjustmentRule[] adjustmentRules = currentOneYearLocal._adjustmentRules;
				AdjustmentRule rule = ((adjustmentRules != null) ? adjustmentRules[0] : null);
				offsetAndRule = (_oneYearLocalFromUtc = new OffsetAndRule(year, currentOneYearLocal.BaseUtcOffset, rule));
			}
			return offsetAndRule;
		}
	}

	private struct StringSerializer
	{
		private enum State
		{
			Escaped,
			NotEscaped,
			StartOfToken,
			EndOfLine
		}

		private readonly string _serializedText;

		private int _currentTokenStartIndex;

		private State _state;

		public static string GetSerializedString(TimeZoneInfo zone)
		{
			Span<char> initialBuffer = stackalloc char[64];
			ValueStringBuilder serializedText = new ValueStringBuilder(initialBuffer);
			SerializeSubstitute(zone.Id, ref serializedText);
			serializedText.Append(';');
			serializedText.AppendSpanFormattable(zone.BaseUtcOffset.TotalMinutes, null, CultureInfo.InvariantCulture);
			serializedText.Append(';');
			SerializeSubstitute(zone.DisplayName, ref serializedText);
			serializedText.Append(';');
			SerializeSubstitute(zone.StandardName, ref serializedText);
			serializedText.Append(';');
			SerializeSubstitute(zone.DaylightName, ref serializedText);
			serializedText.Append(';');
			AdjustmentRule[] adjustmentRules = zone.GetAdjustmentRules();
			AdjustmentRule[] array = adjustmentRules;
			foreach (AdjustmentRule adjustmentRule in array)
			{
				serializedText.Append('[');
				serializedText.AppendSpanFormattable(adjustmentRule.DateStart, "MM:dd:yyyy", DateTimeFormatInfo.InvariantInfo);
				serializedText.Append(';');
				serializedText.AppendSpanFormattable(adjustmentRule.DateEnd, "MM:dd:yyyy", DateTimeFormatInfo.InvariantInfo);
				serializedText.Append(';');
				serializedText.AppendSpanFormattable(adjustmentRule.DaylightDelta.TotalMinutes, null, CultureInfo.InvariantCulture);
				serializedText.Append(';');
				SerializeTransitionTime(adjustmentRule.DaylightTransitionStart, ref serializedText);
				serializedText.Append(';');
				SerializeTransitionTime(adjustmentRule.DaylightTransitionEnd, ref serializedText);
				serializedText.Append(';');
				if (adjustmentRule.BaseUtcOffsetDelta != TimeSpan.Zero)
				{
					serializedText.AppendSpanFormattable(adjustmentRule.BaseUtcOffsetDelta.TotalMinutes, null, CultureInfo.InvariantCulture);
					serializedText.Append(';');
				}
				if (adjustmentRule.NoDaylightTransitions)
				{
					serializedText.Append('1');
					serializedText.Append(';');
				}
				serializedText.Append(']');
			}
			serializedText.Append(';');
			return serializedText.ToString();
		}

		public static TimeZoneInfo GetDeserializedTimeZoneInfo(string source)
		{
			StringSerializer stringSerializer = new StringSerializer(source);
			string nextStringValue = stringSerializer.GetNextStringValue();
			TimeSpan nextTimeSpanValue = stringSerializer.GetNextTimeSpanValue();
			string nextStringValue2 = stringSerializer.GetNextStringValue();
			string nextStringValue3 = stringSerializer.GetNextStringValue();
			string nextStringValue4 = stringSerializer.GetNextStringValue();
			AdjustmentRule[] nextAdjustmentRuleArrayValue = stringSerializer.GetNextAdjustmentRuleArrayValue();
			try
			{
				return new TimeZoneInfo(nextStringValue, nextTimeSpanValue, nextStringValue2, nextStringValue3, nextStringValue4, nextAdjustmentRuleArrayValue, disableDaylightSavingTime: false);
			}
			catch (ArgumentException innerException)
			{
				throw new SerializationException(SR.Serialization_InvalidData, innerException);
			}
			catch (InvalidTimeZoneException innerException2)
			{
				throw new SerializationException(SR.Serialization_InvalidData, innerException2);
			}
		}

		private StringSerializer(string str)
		{
			_serializedText = str;
			_currentTokenStartIndex = 0;
			_state = State.StartOfToken;
		}

		private static void SerializeSubstitute(string text, ref ValueStringBuilder serializedText)
		{
			foreach (char c in text)
			{
				if (c == '\\' || c == '[' || c == ']' || c == ';')
				{
					serializedText.Append('\\');
				}
				serializedText.Append(c);
			}
		}

		private static void SerializeTransitionTime(TransitionTime time, ref ValueStringBuilder serializedText)
		{
			serializedText.Append('[');
			serializedText.Append(time.IsFixedDateRule ? '1' : '0');
			serializedText.Append(';');
			serializedText.AppendSpanFormattable(time.TimeOfDay, "HH:mm:ss.FFF", DateTimeFormatInfo.InvariantInfo);
			serializedText.Append(';');
			serializedText.AppendSpanFormattable(time.Month, null, CultureInfo.InvariantCulture);
			serializedText.Append(';');
			if (time.IsFixedDateRule)
			{
				serializedText.AppendSpanFormattable(time.Day, null, CultureInfo.InvariantCulture);
				serializedText.Append(';');
			}
			else
			{
				serializedText.AppendSpanFormattable(time.Week, null, CultureInfo.InvariantCulture);
				serializedText.Append(';');
				serializedText.AppendSpanFormattable((int)time.DayOfWeek, null, CultureInfo.InvariantCulture);
				serializedText.Append(';');
			}
			serializedText.Append(']');
		}

		private static void VerifyIsEscapableCharacter(char c)
		{
			if (c != '\\' && c != ';' && c != '[' && c != ']')
			{
				throw new SerializationException(SR.Format(SR.Serialization_InvalidEscapeSequence, c));
			}
		}

		private void SkipVersionNextDataFields(int depth)
		{
			if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			State state = State.NotEscaped;
			for (int i = _currentTokenStartIndex; i < _serializedText.Length; i++)
			{
				switch (state)
				{
				case State.Escaped:
					VerifyIsEscapableCharacter(_serializedText[i]);
					state = State.NotEscaped;
					break;
				case State.NotEscaped:
					switch (_serializedText[i])
					{
					case '\\':
						state = State.Escaped;
						break;
					case '[':
						depth++;
						break;
					case ']':
						depth--;
						if (depth == 0)
						{
							_currentTokenStartIndex = i + 1;
							if (_currentTokenStartIndex >= _serializedText.Length)
							{
								_state = State.EndOfLine;
							}
							else
							{
								_state = State.StartOfToken;
							}
							return;
						}
						break;
					case '\0':
						throw new SerializationException(SR.Serialization_InvalidData);
					}
					break;
				}
			}
			throw new SerializationException(SR.Serialization_InvalidData);
		}

		private string GetNextStringValue()
		{
			if (_state == State.EndOfLine)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			State state = State.NotEscaped;
			Span<char> initialBuffer = stackalloc char[64];
			ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
			for (int i = _currentTokenStartIndex; i < _serializedText.Length; i++)
			{
				switch (state)
				{
				case State.Escaped:
					VerifyIsEscapableCharacter(_serializedText[i]);
					valueStringBuilder.Append(_serializedText[i]);
					state = State.NotEscaped;
					break;
				case State.NotEscaped:
					switch (_serializedText[i])
					{
					case '\\':
						state = State.Escaped;
						break;
					case '[':
						throw new SerializationException(SR.Serialization_InvalidData);
					case ']':
						throw new SerializationException(SR.Serialization_InvalidData);
					case ';':
						_currentTokenStartIndex = i + 1;
						if (_currentTokenStartIndex >= _serializedText.Length)
						{
							_state = State.EndOfLine;
						}
						else
						{
							_state = State.StartOfToken;
						}
						return valueStringBuilder.ToString();
					case '\0':
						throw new SerializationException(SR.Serialization_InvalidData);
					default:
						valueStringBuilder.Append(_serializedText[i]);
						break;
					}
					break;
				}
			}
			if (state == State.Escaped)
			{
				throw new SerializationException(SR.Format(SR.Serialization_InvalidEscapeSequence, string.Empty));
			}
			throw new SerializationException(SR.Serialization_InvalidData);
		}

		private DateTime GetNextDateTimeValue(string format)
		{
			string nextStringValue = GetNextStringValue();
			if (!DateTime.TryParseExact(nextStringValue, format, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out var result))
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			return result;
		}

		private TimeSpan GetNextTimeSpanValue()
		{
			int nextInt32Value = GetNextInt32Value();
			try
			{
				return new TimeSpan(0, nextInt32Value, 0);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				throw new SerializationException(SR.Serialization_InvalidData, innerException);
			}
		}

		private int GetNextInt32Value()
		{
			string nextStringValue = GetNextStringValue();
			if (!int.TryParse(nextStringValue, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			return result;
		}

		private AdjustmentRule[] GetNextAdjustmentRuleArrayValue()
		{
			List<AdjustmentRule> list = new List<AdjustmentRule>(1);
			int num = 0;
			for (AdjustmentRule nextAdjustmentRuleValue = GetNextAdjustmentRuleValue(); nextAdjustmentRuleValue != null; nextAdjustmentRuleValue = GetNextAdjustmentRuleValue())
			{
				list.Add(nextAdjustmentRuleValue);
				num++;
			}
			if (_state == State.EndOfLine)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (num == 0)
			{
				return null;
			}
			return list.ToArray();
		}

		private AdjustmentRule GetNextAdjustmentRuleValue()
		{
			if (_state == State.EndOfLine)
			{
				return null;
			}
			if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (_serializedText[_currentTokenStartIndex] == ';')
			{
				return null;
			}
			if (_serializedText[_currentTokenStartIndex] != '[')
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			_currentTokenStartIndex++;
			DateTime nextDateTimeValue = GetNextDateTimeValue("MM:dd:yyyy");
			DateTime nextDateTimeValue2 = GetNextDateTimeValue("MM:dd:yyyy");
			TimeSpan nextTimeSpanValue = GetNextTimeSpanValue();
			TransitionTime nextTransitionTimeValue = GetNextTransitionTimeValue();
			TransitionTime nextTransitionTimeValue2 = GetNextTransitionTimeValue();
			TimeSpan baseUtcOffsetDelta = TimeSpan.Zero;
			int num = 0;
			if (_state == State.EndOfLine || _currentTokenStartIndex >= _serializedText.Length)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if ((_serializedText[_currentTokenStartIndex] >= '0' && _serializedText[_currentTokenStartIndex] <= '9') || _serializedText[_currentTokenStartIndex] == '-' || _serializedText[_currentTokenStartIndex] == '+')
			{
				baseUtcOffsetDelta = GetNextTimeSpanValue();
			}
			if (_serializedText[_currentTokenStartIndex] >= '0' && _serializedText[_currentTokenStartIndex] <= '1')
			{
				num = GetNextInt32Value();
			}
			if (_state == State.EndOfLine || _currentTokenStartIndex >= _serializedText.Length)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (_serializedText[_currentTokenStartIndex] != ']')
			{
				SkipVersionNextDataFields(1);
			}
			else
			{
				_currentTokenStartIndex++;
			}
			AdjustmentRule result;
			try
			{
				result = AdjustmentRule.CreateAdjustmentRule(nextDateTimeValue, nextDateTimeValue2, nextTimeSpanValue, nextTransitionTimeValue, nextTransitionTimeValue2, baseUtcOffsetDelta, num > 0);
			}
			catch (ArgumentException innerException)
			{
				throw new SerializationException(SR.Serialization_InvalidData, innerException);
			}
			if (_currentTokenStartIndex >= _serializedText.Length)
			{
				_state = State.EndOfLine;
			}
			else
			{
				_state = State.StartOfToken;
			}
			return result;
		}

		private TransitionTime GetNextTransitionTimeValue()
		{
			if (_state == State.EndOfLine || (_currentTokenStartIndex < _serializedText.Length && _serializedText[_currentTokenStartIndex] == ']'))
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (_currentTokenStartIndex < 0 || _currentTokenStartIndex >= _serializedText.Length)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (_serializedText[_currentTokenStartIndex] != '[')
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			_currentTokenStartIndex++;
			int nextInt32Value = GetNextInt32Value();
			if (nextInt32Value != 0 && nextInt32Value != 1)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			DateTime nextDateTimeValue = GetNextDateTimeValue("HH:mm:ss.FFF");
			nextDateTimeValue = new DateTime(1, 1, 1, nextDateTimeValue.Hour, nextDateTimeValue.Minute, nextDateTimeValue.Second, nextDateTimeValue.Millisecond);
			int nextInt32Value2 = GetNextInt32Value();
			TransitionTime result;
			if (nextInt32Value == 1)
			{
				int nextInt32Value3 = GetNextInt32Value();
				try
				{
					result = TransitionTime.CreateFixedDateRule(nextDateTimeValue, nextInt32Value2, nextInt32Value3);
				}
				catch (ArgumentException innerException)
				{
					throw new SerializationException(SR.Serialization_InvalidData, innerException);
				}
			}
			else
			{
				int nextInt32Value4 = GetNextInt32Value();
				int nextInt32Value5 = GetNextInt32Value();
				try
				{
					result = TransitionTime.CreateFloatingDateRule(nextDateTimeValue, nextInt32Value2, nextInt32Value4, (DayOfWeek)nextInt32Value5);
				}
				catch (ArgumentException innerException2)
				{
					throw new SerializationException(SR.Serialization_InvalidData, innerException2);
				}
			}
			if (_state == State.EndOfLine || _currentTokenStartIndex >= _serializedText.Length)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (_serializedText[_currentTokenStartIndex] != ']')
			{
				SkipVersionNextDataFields(1);
			}
			else
			{
				_currentTokenStartIndex++;
			}
			bool flag = false;
			if (_currentTokenStartIndex < _serializedText.Length && _serializedText[_currentTokenStartIndex] == ';')
			{
				_currentTokenStartIndex++;
				flag = true;
			}
			if (!flag)
			{
				throw new SerializationException(SR.Serialization_InvalidData);
			}
			if (_currentTokenStartIndex >= _serializedText.Length)
			{
				_state = State.EndOfLine;
			}
			else
			{
				_state = State.StartOfToken;
			}
			return result;
		}
	}

	[Serializable]
	public readonly struct TransitionTime : IEquatable<TransitionTime>, ISerializable, IDeserializationCallback
	{
		private readonly DateTime _timeOfDay;

		private readonly byte _month;

		private readonly byte _week;

		private readonly byte _day;

		private readonly DayOfWeek _dayOfWeek;

		private readonly bool _isFixedDateRule;

		public DateTime TimeOfDay => _timeOfDay;

		public int Month => _month;

		public int Week => _week;

		public int Day => _day;

		public DayOfWeek DayOfWeek => _dayOfWeek;

		public bool IsFixedDateRule => _isFixedDateRule;

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			if (obj is TransitionTime)
			{
				return Equals((TransitionTime)obj);
			}
			return false;
		}

		public static bool operator ==(TransitionTime t1, TransitionTime t2)
		{
			return t1.Equals(t2);
		}

		public static bool operator !=(TransitionTime t1, TransitionTime t2)
		{
			return !t1.Equals(t2);
		}

		public bool Equals(TransitionTime other)
		{
			if (_isFixedDateRule == other._isFixedDateRule && _timeOfDay == other._timeOfDay && _month == other._month)
			{
				if (!other._isFixedDateRule)
				{
					if (_week == other._week)
					{
						return _dayOfWeek == other._dayOfWeek;
					}
					return false;
				}
				return _day == other._day;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return _month ^ (_week << 8);
		}

		private TransitionTime(DateTime timeOfDay, int month, int week, int day, DayOfWeek dayOfWeek, bool isFixedDateRule)
		{
			ValidateTransitionTime(timeOfDay, month, week, day, dayOfWeek);
			_timeOfDay = timeOfDay;
			_month = (byte)month;
			_week = (byte)week;
			_day = (byte)day;
			_dayOfWeek = dayOfWeek;
			_isFixedDateRule = isFixedDateRule;
		}

		public static TransitionTime CreateFixedDateRule(DateTime timeOfDay, int month, int day)
		{
			return new TransitionTime(timeOfDay, month, 1, day, DayOfWeek.Sunday, isFixedDateRule: true);
		}

		public static TransitionTime CreateFloatingDateRule(DateTime timeOfDay, int month, int week, DayOfWeek dayOfWeek)
		{
			return new TransitionTime(timeOfDay, month, week, 1, dayOfWeek, isFixedDateRule: false);
		}

		private static void ValidateTransitionTime(DateTime timeOfDay, int month, int week, int day, DayOfWeek dayOfWeek)
		{
			if (timeOfDay.Kind != 0)
			{
				throw new ArgumentException(SR.Argument_DateTimeKindMustBeUnspecified, "timeOfDay");
			}
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", SR.ArgumentOutOfRange_MonthParam);
			}
			if (day < 1 || day > 31)
			{
				throw new ArgumentOutOfRangeException("day", SR.ArgumentOutOfRange_DayParam);
			}
			if (week < 1 || week > 5)
			{
				throw new ArgumentOutOfRangeException("week", SR.ArgumentOutOfRange_Week);
			}
			if (dayOfWeek < DayOfWeek.Sunday || dayOfWeek > DayOfWeek.Saturday)
			{
				throw new ArgumentOutOfRangeException("dayOfWeek", SR.ArgumentOutOfRange_DayOfWeek);
			}
			timeOfDay.GetDate(out var year, out var month2, out var day2);
			if (year != 1 || month2 != 1 || day2 != 1 || timeOfDay.Ticks % 10000 != 0L)
			{
				throw new ArgumentException(SR.Argument_DateTimeHasTicks, "timeOfDay");
			}
		}

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			if (this != default(TransitionTime))
			{
				try
				{
					ValidateTransitionTime(_timeOfDay, _month, _week, _day, _dayOfWeek);
				}
				catch (ArgumentException innerException)
				{
					throw new SerializationException(SR.Serialization_InvalidData, innerException);
				}
			}
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.AddValue("TimeOfDay", _timeOfDay);
			info.AddValue("Month", _month);
			info.AddValue("Week", _week);
			info.AddValue("Day", _day);
			info.AddValue("DayOfWeek", _dayOfWeek);
			info.AddValue("IsFixedDateRule", _isFixedDateRule);
		}

		private TransitionTime(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			_timeOfDay = (DateTime)info.GetValue("TimeOfDay", typeof(DateTime));
			_month = (byte)info.GetValue("Month", typeof(byte));
			_week = (byte)info.GetValue("Week", typeof(byte));
			_day = (byte)info.GetValue("Day", typeof(byte));
			_dayOfWeek = (DayOfWeek)info.GetValue("DayOfWeek", typeof(DayOfWeek));
			_isFixedDateRule = (bool)info.GetValue("IsFixedDateRule", typeof(bool));
		}
	}

	private sealed class OffsetAndRule
	{
		public readonly int Year;

		public readonly TimeSpan Offset;

		public readonly AdjustmentRule Rule;

		public OffsetAndRule(int year, TimeSpan offset, AdjustmentRule rule)
		{
			Year = year;
			Offset = offset;
			Rule = rule;
		}
	}

	private readonly string _id;

	private readonly string _displayName;

	private readonly string _standardDisplayName;

	private readonly string _daylightDisplayName;

	private readonly TimeSpan _baseUtcOffset;

	private readonly bool _supportsDaylightSavingTime;

	private readonly AdjustmentRule[] _adjustmentRules;

	private List<TimeZoneInfo> _equivalentZones;

	private static readonly TimeZoneInfo s_utcTimeZone = CreateUtcTimeZone();

	private static CachedData s_cachedData = new CachedData();

	private static readonly DateTime s_maxDateOnly = new DateTime(9999, 12, 31);

	private static readonly DateTime s_minDateOnly = new DateTime(1, 1, 2);

	private static readonly TimeSpan MaxOffset = TimeSpan.FromHours(14.0);

	private static readonly TimeSpan MinOffset = -MaxOffset;

	public string Id => _id;

	public bool HasIanaId { get; }

	public string DisplayName => _displayName ?? string.Empty;

	public string StandardName => _standardDisplayName ?? string.Empty;

	public string DaylightName => _daylightDisplayName ?? string.Empty;

	public TimeSpan BaseUtcOffset => _baseUtcOffset;

	public bool SupportsDaylightSavingTime => _supportsDaylightSavingTime;

	public static TimeZoneInfo Local => s_cachedData.Local;

	public static TimeZoneInfo Utc => s_utcTimeZone;

	public TimeSpan[] GetAmbiguousTimeOffsets(DateTimeOffset dateTimeOffset)
	{
		if (!SupportsDaylightSavingTime)
		{
			throw new ArgumentException(SR.Argument_DateTimeOffsetIsNotAmbiguous, "dateTimeOffset");
		}
		DateTime dateTime = ConvertTime(dateTimeOffset, this).DateTime;
		bool flag = false;
		int? ruleIndex;
		AdjustmentRule adjustmentRuleForAmbiguousOffsets = GetAdjustmentRuleForAmbiguousOffsets(dateTime, out ruleIndex);
		if (adjustmentRuleForAmbiguousOffsets != null && adjustmentRuleForAmbiguousOffsets.HasDaylightSaving)
		{
			DaylightTimeStruct daylightTime = GetDaylightTime(dateTime.Year, adjustmentRuleForAmbiguousOffsets, ruleIndex);
			flag = GetIsAmbiguousTime(dateTime, adjustmentRuleForAmbiguousOffsets, daylightTime);
		}
		if (!flag)
		{
			throw new ArgumentException(SR.Argument_DateTimeOffsetIsNotAmbiguous, "dateTimeOffset");
		}
		TimeSpan[] array = new TimeSpan[2];
		TimeSpan timeSpan = _baseUtcOffset + adjustmentRuleForAmbiguousOffsets.BaseUtcOffsetDelta;
		if (adjustmentRuleForAmbiguousOffsets.DaylightDelta > TimeSpan.Zero)
		{
			array[0] = timeSpan;
			array[1] = timeSpan + adjustmentRuleForAmbiguousOffsets.DaylightDelta;
		}
		else
		{
			array[0] = timeSpan + adjustmentRuleForAmbiguousOffsets.DaylightDelta;
			array[1] = timeSpan;
		}
		return array;
	}

	public TimeSpan[] GetAmbiguousTimeOffsets(DateTime dateTime)
	{
		if (!SupportsDaylightSavingTime)
		{
			throw new ArgumentException(SR.Argument_DateTimeIsNotAmbiguous, "dateTime");
		}
		DateTime dateTime2;
		if (dateTime.Kind == DateTimeKind.Local)
		{
			CachedData cachedData = s_cachedData;
			dateTime2 = ConvertTime(dateTime, cachedData.Local, this, TimeZoneInfoOptions.None, cachedData);
		}
		else if (dateTime.Kind == DateTimeKind.Utc)
		{
			CachedData cachedData2 = s_cachedData;
			dateTime2 = ConvertTime(dateTime, s_utcTimeZone, this, TimeZoneInfoOptions.None, cachedData2);
		}
		else
		{
			dateTime2 = dateTime;
		}
		bool flag = false;
		int? ruleIndex;
		AdjustmentRule adjustmentRuleForAmbiguousOffsets = GetAdjustmentRuleForAmbiguousOffsets(dateTime2, out ruleIndex);
		if (adjustmentRuleForAmbiguousOffsets != null && adjustmentRuleForAmbiguousOffsets.HasDaylightSaving)
		{
			DaylightTimeStruct daylightTime = GetDaylightTime(dateTime2.Year, adjustmentRuleForAmbiguousOffsets, ruleIndex);
			flag = GetIsAmbiguousTime(dateTime2, adjustmentRuleForAmbiguousOffsets, daylightTime);
		}
		if (!flag)
		{
			throw new ArgumentException(SR.Argument_DateTimeIsNotAmbiguous, "dateTime");
		}
		TimeSpan[] array = new TimeSpan[2];
		TimeSpan timeSpan = _baseUtcOffset + adjustmentRuleForAmbiguousOffsets.BaseUtcOffsetDelta;
		if (adjustmentRuleForAmbiguousOffsets.DaylightDelta > TimeSpan.Zero)
		{
			array[0] = timeSpan;
			array[1] = timeSpan + adjustmentRuleForAmbiguousOffsets.DaylightDelta;
		}
		else
		{
			array[0] = timeSpan + adjustmentRuleForAmbiguousOffsets.DaylightDelta;
			array[1] = timeSpan;
		}
		return array;
	}

	private AdjustmentRule GetAdjustmentRuleForAmbiguousOffsets(DateTime adjustedTime, out int? ruleIndex)
	{
		AdjustmentRule adjustmentRuleForTime = GetAdjustmentRuleForTime(adjustedTime, out ruleIndex);
		if (adjustmentRuleForTime != null && adjustmentRuleForTime.NoDaylightTransitions && !adjustmentRuleForTime.HasDaylightSaving)
		{
			return GetPreviousAdjustmentRule(adjustmentRuleForTime, ruleIndex);
		}
		return adjustmentRuleForTime;
	}

	private AdjustmentRule GetPreviousAdjustmentRule(AdjustmentRule rule, int? ruleIndex)
	{
		if (ruleIndex.HasValue && 0 < ruleIndex.GetValueOrDefault() && ruleIndex.GetValueOrDefault() < _adjustmentRules.Length)
		{
			return _adjustmentRules[ruleIndex.GetValueOrDefault() - 1];
		}
		AdjustmentRule result = rule;
		for (int i = 1; i < _adjustmentRules.Length; i++)
		{
			if (rule == _adjustmentRules[i])
			{
				result = _adjustmentRules[i - 1];
				break;
			}
		}
		return result;
	}

	public TimeSpan GetUtcOffset(DateTimeOffset dateTimeOffset)
	{
		return GetUtcOffsetFromUtc(dateTimeOffset.UtcDateTime, this);
	}

	public TimeSpan GetUtcOffset(DateTime dateTime)
	{
		return GetUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime, s_cachedData);
	}

	internal static TimeSpan GetLocalUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags)
	{
		CachedData cachedData = s_cachedData;
		return cachedData.Local.GetUtcOffset(dateTime, flags, cachedData);
	}

	internal TimeSpan GetUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags)
	{
		return GetUtcOffset(dateTime, flags, s_cachedData);
	}

	private TimeSpan GetUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags, CachedData cachedData)
	{
		if (dateTime.Kind == DateTimeKind.Local)
		{
			if (cachedData.GetCorrespondingKind(this) != DateTimeKind.Local)
			{
				DateTime time = ConvertTime(dateTime, cachedData.Local, s_utcTimeZone, flags);
				return GetUtcOffsetFromUtc(time, this);
			}
		}
		else if (dateTime.Kind == DateTimeKind.Utc)
		{
			if (cachedData.GetCorrespondingKind(this) == DateTimeKind.Utc)
			{
				return _baseUtcOffset;
			}
			return GetUtcOffsetFromUtc(dateTime, this);
		}
		return GetUtcOffset(dateTime, this);
	}

	public bool IsAmbiguousTime(DateTimeOffset dateTimeOffset)
	{
		if (!_supportsDaylightSavingTime)
		{
			return false;
		}
		return IsAmbiguousTime(ConvertTime(dateTimeOffset, this).DateTime);
	}

	public bool IsAmbiguousTime(DateTime dateTime)
	{
		return IsAmbiguousTime(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
	}

	internal bool IsAmbiguousTime(DateTime dateTime, TimeZoneInfoOptions flags)
	{
		if (!_supportsDaylightSavingTime)
		{
			return false;
		}
		CachedData cachedData = s_cachedData;
		DateTime dateTime2 = ((dateTime.Kind == DateTimeKind.Local) ? ConvertTime(dateTime, cachedData.Local, this, flags, cachedData) : ((dateTime.Kind == DateTimeKind.Utc) ? ConvertTime(dateTime, s_utcTimeZone, this, flags, cachedData) : dateTime));
		int? ruleIndex;
		AdjustmentRule adjustmentRuleForTime = GetAdjustmentRuleForTime(dateTime2, out ruleIndex);
		if (adjustmentRuleForTime != null && adjustmentRuleForTime.HasDaylightSaving)
		{
			DaylightTimeStruct daylightTime = GetDaylightTime(dateTime2.Year, adjustmentRuleForTime, ruleIndex);
			return GetIsAmbiguousTime(dateTime2, adjustmentRuleForTime, daylightTime);
		}
		return false;
	}

	public bool IsDaylightSavingTime(DateTimeOffset dateTimeOffset)
	{
		GetUtcOffsetFromUtc(dateTimeOffset.UtcDateTime, this, out var isDaylightSavings);
		return isDaylightSavings;
	}

	public bool IsDaylightSavingTime(DateTime dateTime)
	{
		return IsDaylightSavingTime(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime, s_cachedData);
	}

	internal bool IsDaylightSavingTime(DateTime dateTime, TimeZoneInfoOptions flags)
	{
		return IsDaylightSavingTime(dateTime, flags, s_cachedData);
	}

	private bool IsDaylightSavingTime(DateTime dateTime, TimeZoneInfoOptions flags, CachedData cachedData)
	{
		if (!_supportsDaylightSavingTime || _adjustmentRules == null)
		{
			return false;
		}
		DateTime dateTime2;
		if (dateTime.Kind == DateTimeKind.Local)
		{
			dateTime2 = ConvertTime(dateTime, cachedData.Local, this, flags, cachedData);
		}
		else
		{
			if (dateTime.Kind == DateTimeKind.Utc)
			{
				if (cachedData.GetCorrespondingKind(this) == DateTimeKind.Utc)
				{
					return false;
				}
				GetUtcOffsetFromUtc(dateTime, this, out var isDaylightSavings);
				return isDaylightSavings;
			}
			dateTime2 = dateTime;
		}
		int? ruleIndex;
		AdjustmentRule adjustmentRuleForTime = GetAdjustmentRuleForTime(dateTime2, out ruleIndex);
		if (adjustmentRuleForTime != null && adjustmentRuleForTime.HasDaylightSaving)
		{
			DaylightTimeStruct daylightTime = GetDaylightTime(dateTime2.Year, adjustmentRuleForTime, ruleIndex);
			return GetIsDaylightSavings(dateTime2, adjustmentRuleForTime, daylightTime);
		}
		return false;
	}

	public bool IsInvalidTime(DateTime dateTime)
	{
		bool result = false;
		if (dateTime.Kind == DateTimeKind.Unspecified || (dateTime.Kind == DateTimeKind.Local && s_cachedData.GetCorrespondingKind(this) == DateTimeKind.Local))
		{
			int? ruleIndex;
			AdjustmentRule adjustmentRuleForTime = GetAdjustmentRuleForTime(dateTime, out ruleIndex);
			if (adjustmentRuleForTime != null && adjustmentRuleForTime.HasDaylightSaving)
			{
				DaylightTimeStruct daylightTime = GetDaylightTime(dateTime.Year, adjustmentRuleForTime, ruleIndex);
				result = GetIsInvalidTime(dateTime, adjustmentRuleForTime, daylightTime);
			}
			else
			{
				result = false;
			}
		}
		return result;
	}

	public static void ClearCachedData()
	{
		s_cachedData = new CachedData();
	}

	public static DateTimeOffset ConvertTimeBySystemTimeZoneId(DateTimeOffset dateTimeOffset, string destinationTimeZoneId)
	{
		return ConvertTime(dateTimeOffset, FindSystemTimeZoneById(destinationTimeZoneId));
	}

	public static DateTime ConvertTimeBySystemTimeZoneId(DateTime dateTime, string destinationTimeZoneId)
	{
		return ConvertTime(dateTime, FindSystemTimeZoneById(destinationTimeZoneId));
	}

	public static DateTime ConvertTimeBySystemTimeZoneId(DateTime dateTime, string sourceTimeZoneId, string destinationTimeZoneId)
	{
		if (dateTime.Kind == DateTimeKind.Local && string.Equals(sourceTimeZoneId, Local.Id, StringComparison.OrdinalIgnoreCase))
		{
			CachedData cachedData = s_cachedData;
			return ConvertTime(dateTime, cachedData.Local, FindSystemTimeZoneById(destinationTimeZoneId), TimeZoneInfoOptions.None, cachedData);
		}
		if (dateTime.Kind == DateTimeKind.Utc && string.Equals(sourceTimeZoneId, Utc.Id, StringComparison.OrdinalIgnoreCase))
		{
			return ConvertTime(dateTime, s_utcTimeZone, FindSystemTimeZoneById(destinationTimeZoneId), TimeZoneInfoOptions.None, s_cachedData);
		}
		return ConvertTime(dateTime, FindSystemTimeZoneById(sourceTimeZoneId), FindSystemTimeZoneById(destinationTimeZoneId));
	}

	public static DateTimeOffset ConvertTime(DateTimeOffset dateTimeOffset, TimeZoneInfo destinationTimeZone)
	{
		if (destinationTimeZone == null)
		{
			throw new ArgumentNullException("destinationTimeZone");
		}
		DateTime utcDateTime = dateTimeOffset.UtcDateTime;
		TimeSpan utcOffsetFromUtc = GetUtcOffsetFromUtc(utcDateTime, destinationTimeZone);
		long num = utcDateTime.Ticks + utcOffsetFromUtc.Ticks;
		if (num <= DateTimeOffset.MaxValue.Ticks)
		{
			if (num >= DateTimeOffset.MinValue.Ticks)
			{
				return new DateTimeOffset(num, utcOffsetFromUtc);
			}
			return DateTimeOffset.MinValue;
		}
		return DateTimeOffset.MaxValue;
	}

	public static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo destinationTimeZone)
	{
		if (destinationTimeZone == null)
		{
			throw new ArgumentNullException("destinationTimeZone");
		}
		if (dateTime.Ticks == 0L)
		{
			ClearCachedData();
		}
		CachedData cachedData = s_cachedData;
		TimeZoneInfo sourceTimeZone = ((dateTime.Kind == DateTimeKind.Utc) ? s_utcTimeZone : cachedData.Local);
		return ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
	}

	public static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
	{
		return ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, TimeZoneInfoOptions.None, s_cachedData);
	}

	internal static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags)
	{
		return ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, flags, s_cachedData);
	}

	private static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags, CachedData cachedData)
	{
		if (sourceTimeZone == null)
		{
			throw new ArgumentNullException("sourceTimeZone");
		}
		if (destinationTimeZone == null)
		{
			throw new ArgumentNullException("destinationTimeZone");
		}
		DateTimeKind correspondingKind = cachedData.GetCorrespondingKind(sourceTimeZone);
		if ((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0 && dateTime.Kind != 0 && dateTime.Kind != correspondingKind)
		{
			throw new ArgumentException(SR.Argument_ConvertMismatch, "sourceTimeZone");
		}
		int? ruleIndex;
		AdjustmentRule adjustmentRuleForTime = sourceTimeZone.GetAdjustmentRuleForTime(dateTime, out ruleIndex);
		TimeSpan baseUtcOffset = sourceTimeZone.BaseUtcOffset;
		if (adjustmentRuleForTime != null)
		{
			baseUtcOffset += adjustmentRuleForTime.BaseUtcOffsetDelta;
			if (adjustmentRuleForTime.HasDaylightSaving)
			{
				bool flag = false;
				DaylightTimeStruct daylightTime = sourceTimeZone.GetDaylightTime(dateTime.Year, adjustmentRuleForTime, ruleIndex);
				if ((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0 && GetIsInvalidTime(dateTime, adjustmentRuleForTime, daylightTime))
				{
					throw new ArgumentException(SR.Argument_DateTimeIsInvalid, "dateTime");
				}
				flag = GetIsDaylightSavings(dateTime, adjustmentRuleForTime, daylightTime);
				baseUtcOffset += (flag ? adjustmentRuleForTime.DaylightDelta : TimeSpan.Zero);
			}
		}
		DateTimeKind correspondingKind2 = cachedData.GetCorrespondingKind(destinationTimeZone);
		if (dateTime.Kind != 0 && correspondingKind != 0 && correspondingKind == correspondingKind2)
		{
			return dateTime;
		}
		long ticks = dateTime.Ticks - baseUtcOffset.Ticks;
		bool isAmbiguousLocalDst;
		DateTime dateTime2 = ConvertUtcToTimeZone(ticks, destinationTimeZone, out isAmbiguousLocalDst);
		if (correspondingKind2 == DateTimeKind.Local)
		{
			return new DateTime(dateTime2.Ticks, DateTimeKind.Local, isAmbiguousLocalDst);
		}
		return new DateTime(dateTime2.Ticks, correspondingKind2);
	}

	public static DateTime ConvertTimeFromUtc(DateTime dateTime, TimeZoneInfo destinationTimeZone)
	{
		return ConvertTime(dateTime, s_utcTimeZone, destinationTimeZone, TimeZoneInfoOptions.None, s_cachedData);
	}

	public static DateTime ConvertTimeToUtc(DateTime dateTime)
	{
		if (dateTime.Kind == DateTimeKind.Utc)
		{
			return dateTime;
		}
		CachedData cachedData = s_cachedData;
		return ConvertTime(dateTime, cachedData.Local, s_utcTimeZone, TimeZoneInfoOptions.None, cachedData);
	}

	internal static DateTime ConvertTimeToUtc(DateTime dateTime, TimeZoneInfoOptions flags)
	{
		if (dateTime.Kind == DateTimeKind.Utc)
		{
			return dateTime;
		}
		CachedData cachedData = s_cachedData;
		return ConvertTime(dateTime, cachedData.Local, s_utcTimeZone, flags, cachedData);
	}

	public static DateTime ConvertTimeToUtc(DateTime dateTime, TimeZoneInfo sourceTimeZone)
	{
		return ConvertTime(dateTime, sourceTimeZone, s_utcTimeZone, TimeZoneInfoOptions.None, s_cachedData);
	}

	public bool Equals([NotNullWhen(true)] TimeZoneInfo? other)
	{
		if (other != null && string.Equals(_id, other._id, StringComparison.OrdinalIgnoreCase))
		{
			return HasSameRules(other);
		}
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return Equals(obj as TimeZoneInfo);
	}

	public static TimeZoneInfo FromSerializedString(string source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (source.Length == 0)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidSerializedString, source), "source");
		}
		return StringSerializer.GetDeserializedTimeZoneInfo(source);
	}

	public override int GetHashCode()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(_id);
	}

	public static ReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones()
	{
		CachedData cachedData = s_cachedData;
		lock (cachedData)
		{
			if (cachedData._readOnlySystemTimeZones == null)
			{
				PopulateAllSystemTimeZones(cachedData);
				cachedData._allSystemTimeZonesRead = true;
				List<TimeZoneInfo> list = ((cachedData._systemTimeZones == null) ? new List<TimeZoneInfo>() : new List<TimeZoneInfo>(cachedData._systemTimeZones.Values));
				list.Sort(delegate(TimeZoneInfo x, TimeZoneInfo y)
				{
					int num = x.BaseUtcOffset.CompareTo(y.BaseUtcOffset);
					return (num != 0) ? num : string.CompareOrdinal(x.DisplayName, y.DisplayName);
				});
				cachedData._readOnlySystemTimeZones = new ReadOnlyCollection<TimeZoneInfo>(list);
			}
		}
		return cachedData._readOnlySystemTimeZones;
	}

	public bool HasSameRules(TimeZoneInfo other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		if (_baseUtcOffset != other._baseUtcOffset || _supportsDaylightSavingTime != other._supportsDaylightSavingTime)
		{
			return false;
		}
		AdjustmentRule[] adjustmentRules = _adjustmentRules;
		AdjustmentRule[] adjustmentRules2 = other._adjustmentRules;
		if (adjustmentRules == null || adjustmentRules2 == null)
		{
			return adjustmentRules == adjustmentRules2;
		}
		return adjustmentRules.AsSpan().SequenceEqual(adjustmentRules2);
	}

	public string ToSerializedString()
	{
		return StringSerializer.GetSerializedString(this);
	}

	public override string ToString()
	{
		return DisplayName;
	}

	private TimeZoneInfo(string id, TimeSpan baseUtcOffset, string displayName, string standardDisplayName, string daylightDisplayName, AdjustmentRule[] adjustmentRules, bool disableDaylightSavingTime, bool hasIanaId = false)
	{
		ValidateTimeZoneInfo(id, baseUtcOffset, adjustmentRules, out var adjustmentRulesSupportDst);
		_id = id;
		_baseUtcOffset = baseUtcOffset;
		_displayName = displayName;
		_standardDisplayName = standardDisplayName;
		_daylightDisplayName = (disableDaylightSavingTime ? null : daylightDisplayName);
		_supportsDaylightSavingTime = adjustmentRulesSupportDst && !disableDaylightSavingTime;
		_adjustmentRules = adjustmentRules;
		HasIanaId = _id.Equals("UTC", StringComparison.OrdinalIgnoreCase) || hasIanaId;
	}

	public static TimeZoneInfo CreateCustomTimeZone(string id, TimeSpan baseUtcOffset, string? displayName, string? standardDisplayName)
	{
		string windowsId;
		bool hasIanaId = TryConvertIanaIdToWindowsId(id, allocate: false, out windowsId);
		return new TimeZoneInfo(id, baseUtcOffset, displayName, standardDisplayName, standardDisplayName, null, disableDaylightSavingTime: false, hasIanaId);
	}

	public static TimeZoneInfo CreateCustomTimeZone(string id, TimeSpan baseUtcOffset, string? displayName, string? standardDisplayName, string? daylightDisplayName, AdjustmentRule[]? adjustmentRules)
	{
		return CreateCustomTimeZone(id, baseUtcOffset, displayName, standardDisplayName, daylightDisplayName, adjustmentRules, disableDaylightSavingTime: false);
	}

	public static TimeZoneInfo CreateCustomTimeZone(string id, TimeSpan baseUtcOffset, string? displayName, string? standardDisplayName, string? daylightDisplayName, AdjustmentRule[]? adjustmentRules, bool disableDaylightSavingTime)
	{
		if (!disableDaylightSavingTime && adjustmentRules != null && adjustmentRules.Length != 0)
		{
			adjustmentRules = (AdjustmentRule[])adjustmentRules.Clone();
		}
		string windowsId;
		bool hasIanaId = TryConvertIanaIdToWindowsId(id, allocate: false, out windowsId);
		return new TimeZoneInfo(id, baseUtcOffset, displayName, standardDisplayName, daylightDisplayName, adjustmentRules, disableDaylightSavingTime, hasIanaId);
	}

	public static bool TryConvertIanaIdToWindowsId(string ianaId, [NotNullWhen(true)] out string? windowsId)
	{
		return TryConvertIanaIdToWindowsId(ianaId, allocate: true, out windowsId);
	}

	public static bool TryConvertWindowsIdToIanaId(string windowsId, [NotNullWhen(true)] out string? ianaId)
	{
		return TryConvertWindowsIdToIanaId(windowsId, null, allocate: true, out ianaId);
	}

	public static bool TryConvertWindowsIdToIanaId(string windowsId, string? region, [NotNullWhen(true)] out string? ianaId)
	{
		return TryConvertWindowsIdToIanaId(windowsId, region, allocate: true, out ianaId);
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		try
		{
			ValidateTimeZoneInfo(_id, _baseUtcOffset, _adjustmentRules, out var adjustmentRulesSupportDst);
			if (adjustmentRulesSupportDst != _supportsDaylightSavingTime)
			{
				throw new SerializationException(SR.Format(SR.Serialization_CorruptField, "SupportsDaylightSavingTime"));
			}
		}
		catch (ArgumentException innerException)
		{
			throw new SerializationException(SR.Serialization_InvalidData, innerException);
		}
		catch (InvalidTimeZoneException innerException2)
		{
			throw new SerializationException(SR.Serialization_InvalidData, innerException2);
		}
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("Id", _id);
		info.AddValue("DisplayName", _displayName);
		info.AddValue("StandardName", _standardDisplayName);
		info.AddValue("DaylightName", _daylightDisplayName);
		info.AddValue("BaseUtcOffset", _baseUtcOffset);
		info.AddValue("AdjustmentRules", _adjustmentRules);
		info.AddValue("SupportsDaylightSavingTime", _supportsDaylightSavingTime);
	}

	private TimeZoneInfo(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		_id = (string)info.GetValue("Id", typeof(string));
		_displayName = (string)info.GetValue("DisplayName", typeof(string));
		_standardDisplayName = (string)info.GetValue("StandardName", typeof(string));
		_daylightDisplayName = (string)info.GetValue("DaylightName", typeof(string));
		_baseUtcOffset = (TimeSpan)info.GetValue("BaseUtcOffset", typeof(TimeSpan));
		_adjustmentRules = (AdjustmentRule[])info.GetValue("AdjustmentRules", typeof(AdjustmentRule[]));
		_supportsDaylightSavingTime = (bool)info.GetValue("SupportsDaylightSavingTime", typeof(bool));
	}

	private AdjustmentRule GetAdjustmentRuleForTime(DateTime dateTime, out int? ruleIndex)
	{
		return GetAdjustmentRuleForTime(dateTime, dateTimeisUtc: false, out ruleIndex);
	}

	private AdjustmentRule GetAdjustmentRuleForTime(DateTime dateTime, bool dateTimeisUtc, out int? ruleIndex)
	{
		if (_adjustmentRules == null || _adjustmentRules.Length == 0)
		{
			ruleIndex = null;
			return null;
		}
		DateTime dateOnly = (dateTimeisUtc ? (dateTime + BaseUtcOffset).Date : dateTime.Date);
		int num = 0;
		int num2 = _adjustmentRules.Length - 1;
		while (num <= num2)
		{
			int num3 = num + (num2 - num >> 1);
			AdjustmentRule adjustmentRule = _adjustmentRules[num3];
			AdjustmentRule previousRule = ((num3 > 0) ? _adjustmentRules[num3 - 1] : adjustmentRule);
			int num4 = CompareAdjustmentRuleToDateTime(adjustmentRule, previousRule, dateTime, dateOnly, dateTimeisUtc);
			if (num4 == 0)
			{
				ruleIndex = num3;
				return adjustmentRule;
			}
			if (num4 < 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		ruleIndex = null;
		return null;
	}

	private int CompareAdjustmentRuleToDateTime(AdjustmentRule rule, AdjustmentRule previousRule, DateTime dateTime, DateTime dateOnly, bool dateTimeisUtc)
	{
		bool flag;
		if (rule.DateStart.Kind == DateTimeKind.Utc)
		{
			DateTime dateTime2 = (dateTimeisUtc ? dateTime : ConvertToUtc(dateTime, previousRule.DaylightDelta, previousRule.BaseUtcOffsetDelta));
			flag = dateTime2 >= rule.DateStart;
		}
		else
		{
			flag = dateOnly >= rule.DateStart;
		}
		if (!flag)
		{
			return 1;
		}
		bool flag2;
		if (rule.DateEnd.Kind == DateTimeKind.Utc)
		{
			DateTime dateTime3 = (dateTimeisUtc ? dateTime : ConvertToUtc(dateTime, rule.DaylightDelta, rule.BaseUtcOffsetDelta));
			flag2 = dateTime3 <= rule.DateEnd;
		}
		else
		{
			flag2 = dateOnly <= rule.DateEnd;
		}
		if (!flag2)
		{
			return -1;
		}
		return 0;
	}

	private DateTime ConvertToUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta)
	{
		return ConvertToFromUtc(dateTime, daylightDelta, baseUtcOffsetDelta, convertToUtc: true);
	}

	private DateTime ConvertFromUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta)
	{
		return ConvertToFromUtc(dateTime, daylightDelta, baseUtcOffsetDelta, convertToUtc: false);
	}

	private DateTime ConvertToFromUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta, bool convertToUtc)
	{
		TimeSpan timeSpan = BaseUtcOffset + daylightDelta + baseUtcOffsetDelta;
		if (convertToUtc)
		{
			timeSpan = timeSpan.Negate();
		}
		long num = dateTime.Ticks + timeSpan.Ticks;
		if (num <= DateTime.MaxValue.Ticks)
		{
			if (num >= DateTime.MinValue.Ticks)
			{
				return new DateTime(num);
			}
			return DateTime.MinValue;
		}
		return DateTime.MaxValue;
	}

	private static DateTime ConvertUtcToTimeZone(long ticks, TimeZoneInfo destinationTimeZone, out bool isAmbiguousLocalDst)
	{
		DateTime time = ((ticks > DateTime.MaxValue.Ticks) ? DateTime.MaxValue : ((ticks < DateTime.MinValue.Ticks) ? DateTime.MinValue : new DateTime(ticks)));
		ticks += GetUtcOffsetFromUtc(time, destinationTimeZone, out isAmbiguousLocalDst).Ticks;
		if (ticks <= DateTime.MaxValue.Ticks)
		{
			if (ticks >= DateTime.MinValue.Ticks)
			{
				return new DateTime(ticks);
			}
			return DateTime.MinValue;
		}
		return DateTime.MaxValue;
	}

	private DaylightTimeStruct GetDaylightTime(int year, AdjustmentRule rule, int? ruleIndex)
	{
		TimeSpan daylightDelta = rule.DaylightDelta;
		DateTime start;
		DateTime end;
		if (rule.NoDaylightTransitions)
		{
			AdjustmentRule previousAdjustmentRule = GetPreviousAdjustmentRule(rule, ruleIndex);
			start = ConvertFromUtc(rule.DateStart, previousAdjustmentRule.DaylightDelta, previousAdjustmentRule.BaseUtcOffsetDelta);
			end = ConvertFromUtc(rule.DateEnd, rule.DaylightDelta, rule.BaseUtcOffsetDelta);
		}
		else
		{
			start = TransitionTimeToDateTime(year, rule.DaylightTransitionStart);
			end = TransitionTimeToDateTime(year, rule.DaylightTransitionEnd);
		}
		return new DaylightTimeStruct(start, end, daylightDelta);
	}

	private static bool GetIsDaylightSavings(DateTime time, AdjustmentRule rule, DaylightTimeStruct daylightTime)
	{
		if (rule == null)
		{
			return false;
		}
		DateTime startTime;
		DateTime endTime;
		if (time.Kind == DateTimeKind.Local)
		{
			startTime = (rule.IsStartDateMarkerForBeginningOfYear() ? new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) : (daylightTime.Start + daylightTime.Delta));
			endTime = (rule.IsEndDateMarkerForEndOfYear() ? new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1L) : daylightTime.End);
		}
		else
		{
			bool flag = rule.DaylightDelta > TimeSpan.Zero;
			startTime = (rule.IsStartDateMarkerForBeginningOfYear() ? new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) : (daylightTime.Start + (flag ? rule.DaylightDelta : TimeSpan.Zero)));
			endTime = (rule.IsEndDateMarkerForEndOfYear() ? new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1L) : (daylightTime.End + (flag ? (-rule.DaylightDelta) : TimeSpan.Zero)));
		}
		bool flag2 = CheckIsDst(startTime, time, endTime, ignoreYearAdjustment: false, rule);
		if (flag2 && time.Kind == DateTimeKind.Local && GetIsAmbiguousTime(time, rule, daylightTime))
		{
			flag2 = time.IsAmbiguousDaylightSavingTime();
		}
		return flag2;
	}

	private TimeSpan GetDaylightSavingsStartOffsetFromUtc(TimeSpan baseUtcOffset, AdjustmentRule rule, int? ruleIndex)
	{
		if (rule.NoDaylightTransitions)
		{
			AdjustmentRule previousAdjustmentRule = GetPreviousAdjustmentRule(rule, ruleIndex);
			return baseUtcOffset + previousAdjustmentRule.BaseUtcOffsetDelta + previousAdjustmentRule.DaylightDelta;
		}
		return baseUtcOffset + rule.BaseUtcOffsetDelta;
	}

	private static TimeSpan GetDaylightSavingsEndOffsetFromUtc(TimeSpan baseUtcOffset, AdjustmentRule rule)
	{
		return baseUtcOffset + rule.BaseUtcOffsetDelta + rule.DaylightDelta;
	}

	private static bool GetIsDaylightSavingsFromUtc(DateTime time, int year, TimeSpan utc, AdjustmentRule rule, int? ruleIndex, out bool isAmbiguousLocalDst, TimeZoneInfo zone)
	{
		isAmbiguousLocalDst = false;
		if (rule == null)
		{
			return false;
		}
		DaylightTimeStruct daylightTime = zone.GetDaylightTime(year, rule, ruleIndex);
		bool ignoreYearAdjustment = false;
		TimeSpan daylightSavingsStartOffsetFromUtc = zone.GetDaylightSavingsStartOffsetFromUtc(utc, rule, ruleIndex);
		DateTime dateTime;
		if (rule.IsStartDateMarkerForBeginningOfYear() && daylightTime.Start.Year > DateTime.MinValue.Year)
		{
			int? ruleIndex2;
			AdjustmentRule adjustmentRuleForTime = zone.GetAdjustmentRuleForTime(new DateTime(daylightTime.Start.Year - 1, 12, 31), out ruleIndex2);
			if (adjustmentRuleForTime != null && adjustmentRuleForTime.IsEndDateMarkerForEndOfYear())
			{
				dateTime = zone.GetDaylightTime(daylightTime.Start.Year - 1, adjustmentRuleForTime, ruleIndex2).Start - utc - adjustmentRuleForTime.BaseUtcOffsetDelta;
				ignoreYearAdjustment = true;
			}
			else
			{
				dateTime = new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) - daylightSavingsStartOffsetFromUtc;
			}
		}
		else
		{
			dateTime = daylightTime.Start - daylightSavingsStartOffsetFromUtc;
		}
		TimeSpan daylightSavingsEndOffsetFromUtc = GetDaylightSavingsEndOffsetFromUtc(utc, rule);
		DateTime dateTime2;
		if (rule.IsEndDateMarkerForEndOfYear() && daylightTime.End.Year < DateTime.MaxValue.Year)
		{
			int? ruleIndex3;
			AdjustmentRule adjustmentRuleForTime2 = zone.GetAdjustmentRuleForTime(new DateTime(daylightTime.End.Year + 1, 1, 1), out ruleIndex3);
			if (adjustmentRuleForTime2 != null && adjustmentRuleForTime2.IsStartDateMarkerForBeginningOfYear())
			{
				dateTime2 = ((!adjustmentRuleForTime2.IsEndDateMarkerForEndOfYear()) ? (zone.GetDaylightTime(daylightTime.End.Year + 1, adjustmentRuleForTime2, ruleIndex3).End - utc - adjustmentRuleForTime2.BaseUtcOffsetDelta - adjustmentRuleForTime2.DaylightDelta) : (new DateTime(daylightTime.End.Year + 1, 12, 31) - utc - adjustmentRuleForTime2.BaseUtcOffsetDelta - adjustmentRuleForTime2.DaylightDelta));
				ignoreYearAdjustment = true;
			}
			else
			{
				dateTime2 = new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1L) - daylightSavingsEndOffsetFromUtc;
			}
		}
		else
		{
			dateTime2 = daylightTime.End - daylightSavingsEndOffsetFromUtc;
		}
		DateTime dateTime3;
		DateTime dateTime4;
		if (daylightTime.Delta.Ticks > 0)
		{
			dateTime3 = dateTime2 - daylightTime.Delta;
			dateTime4 = dateTime2;
		}
		else
		{
			dateTime3 = dateTime;
			dateTime4 = dateTime - daylightTime.Delta;
		}
		bool flag = CheckIsDst(dateTime, time, dateTime2, ignoreYearAdjustment, rule);
		if (flag)
		{
			isAmbiguousLocalDst = time >= dateTime3 && time < dateTime4;
			if (!isAmbiguousLocalDst && dateTime3.Year != dateTime4.Year)
			{
				try
				{
					DateTime dateTime5 = dateTime3.AddYears(1);
					DateTime dateTime6 = dateTime4.AddYears(1);
					isAmbiguousLocalDst = time >= dateTime5 && time < dateTime6;
				}
				catch (ArgumentOutOfRangeException)
				{
				}
				if (!isAmbiguousLocalDst)
				{
					try
					{
						DateTime dateTime5 = dateTime3.AddYears(-1);
						DateTime dateTime6 = dateTime4.AddYears(-1);
						isAmbiguousLocalDst = time >= dateTime5 && time < dateTime6;
					}
					catch (ArgumentOutOfRangeException)
					{
					}
				}
			}
		}
		return flag;
	}

	private static bool CheckIsDst(DateTime startTime, DateTime time, DateTime endTime, bool ignoreYearAdjustment, AdjustmentRule rule)
	{
		if (!ignoreYearAdjustment && !rule.NoDaylightTransitions)
		{
			int year = startTime.Year;
			int year2 = endTime.Year;
			if (year != year2)
			{
				endTime = endTime.AddYears(year - year2);
			}
			int year3 = time.Year;
			if (year != year3)
			{
				time = time.AddYears(year - year3);
			}
		}
		if (startTime > endTime)
		{
			if (!(time < endTime))
			{
				return time >= startTime;
			}
			return true;
		}
		if (rule.NoDaylightTransitions)
		{
			if (time >= startTime)
			{
				return time <= endTime;
			}
			return false;
		}
		if (time >= startTime)
		{
			return time < endTime;
		}
		return false;
	}

	private static bool GetIsAmbiguousTime(DateTime time, AdjustmentRule rule, DaylightTimeStruct daylightTime)
	{
		bool result = false;
		if (rule == null || rule.DaylightDelta == TimeSpan.Zero)
		{
			return result;
		}
		DateTime dateTime;
		DateTime dateTime2;
		if (rule.DaylightDelta > TimeSpan.Zero)
		{
			if (rule.IsEndDateMarkerForEndOfYear())
			{
				return false;
			}
			dateTime = daylightTime.End;
			dateTime2 = daylightTime.End - rule.DaylightDelta;
		}
		else
		{
			if (rule.IsStartDateMarkerForBeginningOfYear())
			{
				return false;
			}
			dateTime = daylightTime.Start;
			dateTime2 = daylightTime.Start + rule.DaylightDelta;
		}
		result = time >= dateTime2 && time < dateTime;
		if (!result && dateTime.Year != dateTime2.Year)
		{
			try
			{
				DateTime dateTime3 = dateTime.AddYears(1);
				DateTime dateTime4 = dateTime2.AddYears(1);
				result = time >= dateTime4 && time < dateTime3;
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			if (!result)
			{
				try
				{
					DateTime dateTime3 = dateTime.AddYears(-1);
					DateTime dateTime4 = dateTime2.AddYears(-1);
					result = time >= dateTime4 && time < dateTime3;
				}
				catch (ArgumentOutOfRangeException)
				{
				}
			}
		}
		return result;
	}

	private static bool GetIsInvalidTime(DateTime time, AdjustmentRule rule, DaylightTimeStruct daylightTime)
	{
		bool result = false;
		if (rule == null || rule.DaylightDelta == TimeSpan.Zero)
		{
			return result;
		}
		DateTime dateTime;
		DateTime dateTime2;
		if (rule.DaylightDelta < TimeSpan.Zero)
		{
			if (rule.IsEndDateMarkerForEndOfYear())
			{
				return false;
			}
			dateTime = daylightTime.End;
			dateTime2 = daylightTime.End - rule.DaylightDelta;
		}
		else
		{
			if (rule.IsStartDateMarkerForBeginningOfYear())
			{
				return false;
			}
			dateTime = daylightTime.Start;
			dateTime2 = daylightTime.Start + rule.DaylightDelta;
		}
		result = time >= dateTime && time < dateTime2;
		if (!result && dateTime.Year != dateTime2.Year)
		{
			try
			{
				DateTime dateTime3 = dateTime.AddYears(1);
				DateTime dateTime4 = dateTime2.AddYears(1);
				result = time >= dateTime3 && time < dateTime4;
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			if (!result)
			{
				try
				{
					DateTime dateTime3 = dateTime.AddYears(-1);
					DateTime dateTime4 = dateTime2.AddYears(-1);
					result = time >= dateTime3 && time < dateTime4;
				}
				catch (ArgumentOutOfRangeException)
				{
				}
			}
		}
		return result;
	}

	private static TimeSpan GetUtcOffset(DateTime time, TimeZoneInfo zone)
	{
		TimeSpan baseUtcOffset = zone.BaseUtcOffset;
		int? ruleIndex;
		AdjustmentRule adjustmentRuleForTime = zone.GetAdjustmentRuleForTime(time, out ruleIndex);
		if (adjustmentRuleForTime != null)
		{
			baseUtcOffset += adjustmentRuleForTime.BaseUtcOffsetDelta;
			if (adjustmentRuleForTime.HasDaylightSaving)
			{
				DaylightTimeStruct daylightTime = zone.GetDaylightTime(time.Year, adjustmentRuleForTime, ruleIndex);
				bool isDaylightSavings = GetIsDaylightSavings(time, adjustmentRuleForTime, daylightTime);
				baseUtcOffset += (isDaylightSavings ? adjustmentRuleForTime.DaylightDelta : TimeSpan.Zero);
			}
		}
		return baseUtcOffset;
	}

	private static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone)
	{
		bool isDaylightSavings;
		return GetUtcOffsetFromUtc(time, zone, out isDaylightSavings);
	}

	private static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone, out bool isDaylightSavings)
	{
		bool isAmbiguousLocalDst;
		return GetUtcOffsetFromUtc(time, zone, out isDaylightSavings, out isAmbiguousLocalDst);
	}

	internal static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone, out bool isDaylightSavings, out bool isAmbiguousLocalDst)
	{
		isDaylightSavings = false;
		isAmbiguousLocalDst = false;
		TimeSpan baseUtcOffset = zone.BaseUtcOffset;
		AdjustmentRule adjustmentRuleForTime;
		int? ruleIndex;
		int year;
		if (time > s_maxDateOnly)
		{
			adjustmentRuleForTime = zone.GetAdjustmentRuleForTime(DateTime.MaxValue, out ruleIndex);
			year = 9999;
		}
		else if (time < s_minDateOnly)
		{
			adjustmentRuleForTime = zone.GetAdjustmentRuleForTime(DateTime.MinValue, out ruleIndex);
			year = 1;
		}
		else
		{
			adjustmentRuleForTime = zone.GetAdjustmentRuleForTime(time, dateTimeisUtc: true, out ruleIndex);
			year = (time + baseUtcOffset).Year;
		}
		if (adjustmentRuleForTime != null)
		{
			baseUtcOffset += adjustmentRuleForTime.BaseUtcOffsetDelta;
			if (adjustmentRuleForTime.HasDaylightSaving)
			{
				isDaylightSavings = GetIsDaylightSavingsFromUtc(time, year, zone._baseUtcOffset, adjustmentRuleForTime, ruleIndex, out isAmbiguousLocalDst, zone);
				baseUtcOffset += (isDaylightSavings ? adjustmentRuleForTime.DaylightDelta : TimeSpan.Zero);
			}
		}
		return baseUtcOffset;
	}

	internal static DateTime TransitionTimeToDateTime(int year, TransitionTime transitionTime)
	{
		TimeSpan timeOfDay = transitionTime.TimeOfDay.TimeOfDay;
		DateTime result;
		if (transitionTime.IsFixedDateRule)
		{
			int num = transitionTime.Day;
			if (num > 28)
			{
				int num2 = DateTime.DaysInMonth(year, transitionTime.Month);
				if (num > num2)
				{
					num = num2;
				}
			}
			result = new DateTime(year, transitionTime.Month, num) + timeOfDay;
		}
		else if (transitionTime.Week <= 4)
		{
			result = new DateTime(year, transitionTime.Month, 1) + timeOfDay;
			int dayOfWeek = (int)result.DayOfWeek;
			int num3 = (int)(transitionTime.DayOfWeek - dayOfWeek);
			if (num3 < 0)
			{
				num3 += 7;
			}
			num3 += 7 * (transitionTime.Week - 1);
			if (num3 > 0)
			{
				result = result.AddDays(num3);
			}
		}
		else
		{
			int day = DateTime.DaysInMonth(year, transitionTime.Month);
			result = new DateTime(year, transitionTime.Month, day) + timeOfDay;
			int dayOfWeek2 = (int)result.DayOfWeek;
			int num4 = (int)(dayOfWeek2 - transitionTime.DayOfWeek);
			if (num4 < 0)
			{
				num4 += 7;
			}
			if (num4 > 0)
			{
				result = result.AddDays(-num4);
			}
		}
		return result;
	}

	private static TimeZoneInfoResult TryGetTimeZone(string id, bool dstDisabled, out TimeZoneInfo value, out Exception e, CachedData cachedData, bool alwaysFallbackToLocalMachine = false)
	{
		TimeZoneInfoResult timeZoneInfoResult = TryGetTimeZoneUsingId(id, dstDisabled, out value, out e, cachedData, alwaysFallbackToLocalMachine);
		if (timeZoneInfoResult != 0)
		{
			bool idIsIana;
			string alternativeId = GetAlternativeId(id, out idIsIana);
			if (alternativeId != null)
			{
				timeZoneInfoResult = TryGetTimeZoneUsingId(alternativeId, dstDisabled, out value, out e, cachedData, alwaysFallbackToLocalMachine);
				if (timeZoneInfoResult == TimeZoneInfoResult.Success)
				{
					TimeZoneInfo timeZoneInfo = null;
					if (value._equivalentZones == null)
					{
						timeZoneInfo = new TimeZoneInfo(id, value._baseUtcOffset, value._displayName, value._standardDisplayName, value._daylightDisplayName, value._adjustmentRules, dstDisabled && value._supportsDaylightSavingTime, idIsIana);
						value._equivalentZones = new List<TimeZoneInfo>();
						lock (value._equivalentZones)
						{
							value._equivalentZones.Add(timeZoneInfo);
						}
					}
					else
					{
						foreach (TimeZoneInfo equivalentZone in value._equivalentZones)
						{
							if (equivalentZone.Id == id)
							{
								timeZoneInfo = equivalentZone;
								break;
							}
						}
						if (timeZoneInfo == null)
						{
							timeZoneInfo = new TimeZoneInfo(id, value._baseUtcOffset, value._displayName, value._standardDisplayName, value._daylightDisplayName, value._adjustmentRules, dstDisabled && value._supportsDaylightSavingTime, idIsIana);
							lock (value._equivalentZones)
							{
								value._equivalentZones.Add(timeZoneInfo);
							}
						}
					}
					value = timeZoneInfo;
				}
			}
		}
		return timeZoneInfoResult;
	}

	private static TimeZoneInfoResult TryGetTimeZoneUsingId(string id, bool dstDisabled, out TimeZoneInfo value, out Exception e, CachedData cachedData, bool alwaysFallbackToLocalMachine)
	{
		TimeZoneInfoResult result = TimeZoneInfoResult.Success;
		e = null;
		if (cachedData._systemTimeZones != null && cachedData._systemTimeZones.TryGetValue(id, out var value2))
		{
			if (dstDisabled && value2._supportsDaylightSavingTime)
			{
				value = CreateCustomTimeZone(value2._id, value2._baseUtcOffset, value2._displayName, value2._standardDisplayName);
			}
			else
			{
				value = new TimeZoneInfo(value2._id, value2._baseUtcOffset, value2._displayName, value2._standardDisplayName, value2._daylightDisplayName, value2._adjustmentRules, disableDaylightSavingTime: false, value2.HasIanaId);
			}
			return result;
		}
		if (!cachedData._allSystemTimeZonesRead || alwaysFallbackToLocalMachine)
		{
			result = TryGetTimeZoneFromLocalMachine(id, dstDisabled, out value, out e, cachedData);
		}
		else
		{
			result = TimeZoneInfoResult.TimeZoneNotFoundException;
			value = null;
		}
		return result;
	}

	private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachine(string id, bool dstDisabled, out TimeZoneInfo value, out Exception e, CachedData cachedData)
	{
		TimeZoneInfo value2;
		TimeZoneInfoResult timeZoneInfoResult = TryGetTimeZoneFromLocalMachine(id, out value2, out e);
		if (timeZoneInfoResult == TimeZoneInfoResult.Success)
		{
			if (cachedData._systemTimeZones == null)
			{
				cachedData._systemTimeZones = new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase) { { "UTC", s_utcTimeZone } };
			}
			if (!id.Equals("UTC", StringComparison.OrdinalIgnoreCase))
			{
				cachedData._systemTimeZones.Add(id, value2);
			}
			if (dstDisabled && value2._supportsDaylightSavingTime)
			{
				value = CreateCustomTimeZone(value2._id, value2._baseUtcOffset, value2._displayName, value2._standardDisplayName);
			}
			else
			{
				value = new TimeZoneInfo(value2._id, value2._baseUtcOffset, value2._displayName, value2._standardDisplayName, value2._daylightDisplayName, value2._adjustmentRules, disableDaylightSavingTime: false, value2.HasIanaId);
			}
		}
		else
		{
			value = null;
		}
		return timeZoneInfoResult;
	}

	private static void ValidateTimeZoneInfo(string id, TimeSpan baseUtcOffset, AdjustmentRule[] adjustmentRules, out bool adjustmentRulesSupportDst)
	{
		if (id == null)
		{
			throw new ArgumentNullException("id");
		}
		if (id.Length == 0)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidId, id), "id");
		}
		if (UtcOffsetOutOfRange(baseUtcOffset))
		{
			throw new ArgumentOutOfRangeException("baseUtcOffset", SR.ArgumentOutOfRange_UtcOffset);
		}
		if (baseUtcOffset.Ticks % 600000000 != 0L)
		{
			throw new ArgumentException(SR.Argument_TimeSpanHasSeconds, "baseUtcOffset");
		}
		adjustmentRulesSupportDst = false;
		if (adjustmentRules == null || adjustmentRules.Length == 0)
		{
			return;
		}
		adjustmentRulesSupportDst = true;
		AdjustmentRule adjustmentRule = null;
		AdjustmentRule adjustmentRule2 = null;
		for (int i = 0; i < adjustmentRules.Length; i++)
		{
			adjustmentRule = adjustmentRule2;
			adjustmentRule2 = adjustmentRules[i];
			if (adjustmentRule2 == null)
			{
				throw new InvalidTimeZoneException(SR.Argument_AdjustmentRulesNoNulls);
			}
			if (!IsValidAdjustmentRuleOffset(baseUtcOffset, adjustmentRule2))
			{
				throw new InvalidTimeZoneException(SR.ArgumentOutOfRange_UtcOffsetAndDaylightDelta);
			}
			if (adjustmentRule != null && adjustmentRule2.DateStart <= adjustmentRule.DateEnd)
			{
				throw new InvalidTimeZoneException(SR.Argument_AdjustmentRulesOutOfOrder);
			}
		}
	}

	internal static bool UtcOffsetOutOfRange(TimeSpan offset)
	{
		if (!(offset < MinOffset))
		{
			return offset > MaxOffset;
		}
		return true;
	}

	private static TimeSpan GetUtcOffset(TimeSpan baseUtcOffset, AdjustmentRule adjustmentRule)
	{
		return baseUtcOffset + adjustmentRule.BaseUtcOffsetDelta + (adjustmentRule.HasDaylightSaving ? adjustmentRule.DaylightDelta : TimeSpan.Zero);
	}

	private static bool IsValidAdjustmentRuleOffset(TimeSpan baseUtcOffset, AdjustmentRule adjustmentRule)
	{
		TimeSpan utcOffset = GetUtcOffset(baseUtcOffset, adjustmentRule);
		return !UtcOffsetOutOfRange(utcOffset);
	}

	private static TimeZoneInfo CreateUtcTimeZone()
	{
		string utcStandardDisplayName = GetUtcStandardDisplayName();
		string utcFullDisplayName = GetUtcFullDisplayName("UTC", utcStandardDisplayName);
		return CreateCustomTimeZone("UTC", TimeSpan.Zero, utcFullDisplayName, utcStandardDisplayName);
	}

	private unsafe static bool TryConvertIanaIdToWindowsId(string ianaId, bool allocate, out string windowsId)
	{
		if (GlobalizationMode.Invariant || GlobalizationMode.UseNls || ianaId == null)
		{
			windowsId = null;
			return false;
		}
		foreach (char c in ianaId)
		{
			if (c == '\\' || c == '\n' || c == '\r')
			{
				windowsId = null;
				return false;
			}
		}
		char* ptr = stackalloc char[100];
		int num = Interop.Globalization.IanaIdToWindowsId(ianaId, ptr, 100);
		if (num > 0)
		{
			windowsId = (allocate ? new string(ptr, 0, num) : null);
			return true;
		}
		windowsId = null;
		return false;
	}

	private unsafe static bool TryConvertWindowsIdToIanaId(string windowsId, string region, bool allocate, out string ianaId)
	{
		if (GlobalizationMode.Invariant || GlobalizationMode.UseNls || windowsId == null)
		{
			ianaId = null;
			return false;
		}
		if (windowsId.Equals("utc", StringComparison.OrdinalIgnoreCase))
		{
			ianaId = "Etc/UTC";
			return true;
		}
		foreach (char c in windowsId)
		{
			if (c == '\\' || c == '\n' || c == '\r')
			{
				ianaId = null;
				return false;
			}
		}
		IntPtr region2 = IntPtr.Zero;
		if (region != null && region.Length < 11)
		{
			byte* ptr = stackalloc byte[(int)(uint)(region.Length + 1)];
			int j;
			for (j = 0; j < region.Length && region[j] <= '\u007f'; j++)
			{
				ptr[j] = (((uint)(region[j] - 97) <= 25u) ? ((byte)(region[j] - 97 + 65)) : ((byte)region[j]));
			}
			if (j >= region.Length)
			{
				ptr[region.Length] = 0;
				region2 = new IntPtr(ptr);
			}
		}
		char* ptr2 = stackalloc char[100];
		int num = Interop.Globalization.WindowsIdToIanaId(windowsId, region2, ptr2, 100);
		if (num > 0)
		{
			ianaId = (allocate ? new string(ptr2, 0, num) : null);
			return true;
		}
		ianaId = null;
		return false;
	}

	public AdjustmentRule[] GetAdjustmentRules()
	{
		if (_adjustmentRules == null)
		{
			return Array.Empty<AdjustmentRule>();
		}
		return (AdjustmentRule[])_adjustmentRules.Clone();
	}

	private static void PopulateAllSystemTimeZones(CachedData cachedData)
	{
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones", writable: false);
		if (registryKey != null)
		{
			string[] subKeyNames = registryKey.GetSubKeyNames();
			foreach (string id in subKeyNames)
			{
				TryGetTimeZone(id, dstDisabled: false, out var _, out var _, cachedData);
			}
		}
	}

	private static string GetAlternativeId(string id, out bool idIsIana)
	{
		idIsIana = true;
		if (!TryConvertIanaIdToWindowsId(id, out string windowsId))
		{
			return null;
		}
		return windowsId;
	}

	private TimeZoneInfo(in Interop.Kernel32.TIME_ZONE_INFORMATION zone, bool dstDisabled)
	{
		string standardName = zone.GetStandardName();
		if (standardName.Length == 0)
		{
			_id = "Local";
		}
		else
		{
			_id = standardName;
		}
		_baseUtcOffset = new TimeSpan(0, -zone.Bias, 0);
		if (!dstDisabled)
		{
			Interop.Kernel32.REG_TZI_FORMAT timeZoneInformation = new Interop.Kernel32.REG_TZI_FORMAT(in zone);
			AdjustmentRule adjustmentRule = CreateAdjustmentRuleFromTimeZoneInformation(in timeZoneInformation, DateTime.MinValue.Date, DateTime.MaxValue.Date, zone.Bias);
			if (adjustmentRule != null)
			{
				_adjustmentRules = new AdjustmentRule[1] { adjustmentRule };
			}
		}
		ValidateTimeZoneInfo(_id, _baseUtcOffset, _adjustmentRules, out _supportsDaylightSavingTime);
		_displayName = standardName;
		_standardDisplayName = standardName;
		_daylightDisplayName = zone.GetDaylightName();
	}

	private static bool CheckDaylightSavingTimeNotSupported(in Interop.Kernel32.TIME_ZONE_INFORMATION timeZone)
	{
		return timeZone.DaylightDate.Equals(in timeZone.StandardDate);
	}

	private static AdjustmentRule CreateAdjustmentRuleFromTimeZoneInformation(in Interop.Kernel32.REG_TZI_FORMAT timeZoneInformation, DateTime startDate, DateTime endDate, int defaultBaseUtcOffset)
	{
		if (timeZoneInformation.StandardDate.Month == 0)
		{
			if (timeZoneInformation.Bias == defaultBaseUtcOffset)
			{
				return null;
			}
			return AdjustmentRule.CreateAdjustmentRule(startDate, endDate, TimeSpan.Zero, TransitionTime.CreateFixedDateRule(DateTime.MinValue, 1, 1), TransitionTime.CreateFixedDateRule(DateTime.MinValue.AddMilliseconds(1.0), 1, 1), new TimeSpan(0, defaultBaseUtcOffset - timeZoneInformation.Bias, 0), noDaylightTransitions: false);
		}
		if (!TransitionTimeFromTimeZoneInformation(in timeZoneInformation, out var transitionTime, readStartDate: true))
		{
			return null;
		}
		if (!TransitionTimeFromTimeZoneInformation(in timeZoneInformation, out var transitionTime2, readStartDate: false))
		{
			return null;
		}
		if (transitionTime.Equals(transitionTime2))
		{
			return null;
		}
		return AdjustmentRule.CreateAdjustmentRule(startDate, endDate, new TimeSpan(0, -timeZoneInformation.DaylightBias, 0), transitionTime, transitionTime2, new TimeSpan(0, defaultBaseUtcOffset - timeZoneInformation.Bias, 0), noDaylightTransitions: false);
	}

	private static string FindIdFromTimeZoneInformation(in Interop.Kernel32.TIME_ZONE_INFORMATION timeZone, out bool dstDisabled)
	{
		dstDisabled = false;
		using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones", writable: false))
		{
			if (registryKey == null)
			{
				return null;
			}
			string[] subKeyNames = registryKey.GetSubKeyNames();
			foreach (string text in subKeyNames)
			{
				if (TryCompareTimeZoneInformationToRegistry(in timeZone, text, out dstDisabled))
				{
					return text;
				}
			}
		}
		return null;
	}

	private static TimeZoneInfo GetLocalTimeZone(CachedData cachedData)
	{
		Interop.Kernel32.TIME_DYNAMIC_ZONE_INFORMATION pTimeZoneInformation;
		uint dynamicTimeZoneInformation = Interop.Kernel32.GetDynamicTimeZoneInformation(out pTimeZoneInformation);
		if (dynamicTimeZoneInformation == uint.MaxValue)
		{
			return CreateCustomTimeZone("Local", TimeSpan.Zero, "Local", "Local");
		}
		string timeZoneKeyName = pTimeZoneInformation.GetTimeZoneKeyName();
		if (timeZoneKeyName.Length != 0 && TryGetTimeZone(timeZoneKeyName, pTimeZoneInformation.DynamicDaylightTimeDisabled != 0, out var value, out var e, cachedData) == TimeZoneInfoResult.Success)
		{
			return value;
		}
		Interop.Kernel32.TIME_ZONE_INFORMATION timeZone = new Interop.Kernel32.TIME_ZONE_INFORMATION(in pTimeZoneInformation);
		bool dstDisabled;
		string text = FindIdFromTimeZoneInformation(in timeZone, out dstDisabled);
		if (text != null && TryGetTimeZone(text, dstDisabled, out var value2, out e, cachedData) == TimeZoneInfoResult.Success)
		{
			return value2;
		}
		return GetLocalTimeZoneFromWin32Data(in timeZone, dstDisabled);
	}

	private static TimeZoneInfo GetLocalTimeZoneFromWin32Data(in Interop.Kernel32.TIME_ZONE_INFORMATION timeZoneInformation, bool dstDisabled)
	{
		try
		{
			return new TimeZoneInfo(in timeZoneInformation, dstDisabled);
		}
		catch (ArgumentException)
		{
		}
		catch (InvalidTimeZoneException)
		{
		}
		if (!dstDisabled)
		{
			try
			{
				return new TimeZoneInfo(in timeZoneInformation, dstDisabled: true);
			}
			catch (ArgumentException)
			{
			}
			catch (InvalidTimeZoneException)
			{
			}
		}
		return CreateCustomTimeZone("Local", TimeSpan.Zero, "Local", "Local");
	}

	public static TimeZoneInfo FindSystemTimeZoneById(string id)
	{
		if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
		{
			return Utc;
		}
		if (id == null)
		{
			throw new ArgumentNullException("id");
		}
		if (id.Length == 0 || id.Length > 255 || id.Contains('\0'))
		{
			throw new TimeZoneNotFoundException(SR.Format(SR.TimeZoneNotFound_MissingData, id));
		}
		CachedData cachedData = s_cachedData;
		TimeZoneInfoResult timeZoneInfoResult;
		TimeZoneInfo value;
		Exception e;
		lock (cachedData)
		{
			timeZoneInfoResult = TryGetTimeZone(id, dstDisabled: false, out value, out e, cachedData);
		}
		return timeZoneInfoResult switch
		{
			TimeZoneInfoResult.Success => value, 
			TimeZoneInfoResult.InvalidTimeZoneException => throw new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidRegistryData, id), e), 
			TimeZoneInfoResult.SecurityException => throw new SecurityException(SR.Format(SR.Security_CannotReadRegistryData, id), e), 
			_ => throw new TimeZoneNotFoundException(SR.Format(SR.TimeZoneNotFound_MissingData, id), e), 
		};
	}

	internal static TimeSpan GetDateTimeNowUtcOffsetFromUtc(DateTime time, out bool isAmbiguousLocalDst)
	{
		isAmbiguousLocalDst = false;
		int year = time.Year;
		OffsetAndRule oneYearLocalFromUtc = s_cachedData.GetOneYearLocalFromUtc(year);
		TimeSpan offset = oneYearLocalFromUtc.Offset;
		if (oneYearLocalFromUtc.Rule != null)
		{
			offset += oneYearLocalFromUtc.Rule.BaseUtcOffsetDelta;
			if (oneYearLocalFromUtc.Rule.HasDaylightSaving)
			{
				bool isDaylightSavingsFromUtc = GetIsDaylightSavingsFromUtc(time, year, oneYearLocalFromUtc.Offset, oneYearLocalFromUtc.Rule, null, out isAmbiguousLocalDst, Local);
				offset += (isDaylightSavingsFromUtc ? oneYearLocalFromUtc.Rule.DaylightDelta : TimeSpan.Zero);
			}
		}
		return offset;
	}

	private static bool TransitionTimeFromTimeZoneInformation(in Interop.Kernel32.REG_TZI_FORMAT timeZoneInformation, out TransitionTime transitionTime, bool readStartDate)
	{
		if (timeZoneInformation.StandardDate.Month == 0)
		{
			transitionTime = default(TransitionTime);
			return false;
		}
		if (readStartDate)
		{
			if (timeZoneInformation.DaylightDate.Year == 0)
			{
				transitionTime = TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, timeZoneInformation.DaylightDate.Hour, timeZoneInformation.DaylightDate.Minute, timeZoneInformation.DaylightDate.Second, timeZoneInformation.DaylightDate.Milliseconds), timeZoneInformation.DaylightDate.Month, timeZoneInformation.DaylightDate.Day, (DayOfWeek)timeZoneInformation.DaylightDate.DayOfWeek);
			}
			else
			{
				transitionTime = TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, timeZoneInformation.DaylightDate.Hour, timeZoneInformation.DaylightDate.Minute, timeZoneInformation.DaylightDate.Second, timeZoneInformation.DaylightDate.Milliseconds), timeZoneInformation.DaylightDate.Month, timeZoneInformation.DaylightDate.Day);
			}
		}
		else if (timeZoneInformation.StandardDate.Year == 0)
		{
			transitionTime = TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, timeZoneInformation.StandardDate.Hour, timeZoneInformation.StandardDate.Minute, timeZoneInformation.StandardDate.Second, timeZoneInformation.StandardDate.Milliseconds), timeZoneInformation.StandardDate.Month, timeZoneInformation.StandardDate.Day, (DayOfWeek)timeZoneInformation.StandardDate.DayOfWeek);
		}
		else
		{
			transitionTime = TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, timeZoneInformation.StandardDate.Hour, timeZoneInformation.StandardDate.Minute, timeZoneInformation.StandardDate.Second, timeZoneInformation.StandardDate.Milliseconds), timeZoneInformation.StandardDate.Month, timeZoneInformation.StandardDate.Day);
		}
		return true;
	}

	private static bool TryCreateAdjustmentRules(string id, in Interop.Kernel32.REG_TZI_FORMAT defaultTimeZoneInformation, out AdjustmentRule[] rules, out Exception e, int defaultBaseUtcOffset)
	{
		rules = null;
		e = null;
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones\\" + id + "\\Dynamic DST", writable: false);
			if (registryKey == null)
			{
				AdjustmentRule adjustmentRule = CreateAdjustmentRuleFromTimeZoneInformation(in defaultTimeZoneInformation, DateTime.MinValue.Date, DateTime.MaxValue.Date, defaultBaseUtcOffset);
				if (adjustmentRule != null)
				{
					rules = new AdjustmentRule[1] { adjustmentRule };
				}
				return true;
			}
			int num = (int)registryKey.GetValue("FirstEntry", -1);
			int num2 = (int)registryKey.GetValue("LastEntry", -1);
			if (num == -1 || num2 == -1 || num > num2)
			{
				return false;
			}
			if (!TryGetTimeZoneEntryFromRegistry(registryKey, num.ToString(CultureInfo.InvariantCulture), out var dtzi))
			{
				return false;
			}
			if (num == num2)
			{
				AdjustmentRule adjustmentRule2 = CreateAdjustmentRuleFromTimeZoneInformation(in dtzi, DateTime.MinValue.Date, DateTime.MaxValue.Date, defaultBaseUtcOffset);
				if (adjustmentRule2 != null)
				{
					rules = new AdjustmentRule[1] { adjustmentRule2 };
				}
				return true;
			}
			List<AdjustmentRule> list = new List<AdjustmentRule>(1);
			AdjustmentRule adjustmentRule3 = CreateAdjustmentRuleFromTimeZoneInformation(in dtzi, DateTime.MinValue.Date, new DateTime(num, 12, 31), defaultBaseUtcOffset);
			if (adjustmentRule3 != null)
			{
				list.Add(adjustmentRule3);
			}
			for (int i = num + 1; i < num2; i++)
			{
				if (!TryGetTimeZoneEntryFromRegistry(registryKey, i.ToString(CultureInfo.InvariantCulture), out dtzi))
				{
					return false;
				}
				AdjustmentRule adjustmentRule4 = CreateAdjustmentRuleFromTimeZoneInformation(in dtzi, new DateTime(i, 1, 1), new DateTime(i, 12, 31), defaultBaseUtcOffset);
				if (adjustmentRule4 != null)
				{
					list.Add(adjustmentRule4);
				}
			}
			if (!TryGetTimeZoneEntryFromRegistry(registryKey, num2.ToString(CultureInfo.InvariantCulture), out dtzi))
			{
				return false;
			}
			AdjustmentRule adjustmentRule5 = CreateAdjustmentRuleFromTimeZoneInformation(in dtzi, new DateTime(num2, 1, 1), DateTime.MaxValue.Date, defaultBaseUtcOffset);
			if (adjustmentRule5 != null)
			{
				list.Add(adjustmentRule5);
			}
			if (list.Count != 0)
			{
				rules = list.ToArray();
			}
		}
		catch (InvalidCastException ex)
		{
			e = ex;
			return false;
		}
		catch (ArgumentOutOfRangeException ex2)
		{
			e = ex2;
			return false;
		}
		catch (ArgumentException ex3)
		{
			e = ex3;
			return false;
		}
		return true;
	}

	private unsafe static bool TryGetTimeZoneEntryFromRegistry(RegistryKey key, string name, out Interop.Kernel32.REG_TZI_FORMAT dtzi)
	{
		if (!(key.GetValue(name, null) is byte[] array) || array.Length != sizeof(Interop.Kernel32.REG_TZI_FORMAT))
		{
			dtzi = default(Interop.Kernel32.REG_TZI_FORMAT);
			return false;
		}
		fixed (byte* ptr = &array[0])
		{
			dtzi = *(Interop.Kernel32.REG_TZI_FORMAT*)ptr;
		}
		return true;
	}

	private static bool TryCompareStandardDate(in Interop.Kernel32.TIME_ZONE_INFORMATION timeZone, in Interop.Kernel32.REG_TZI_FORMAT registryTimeZoneInfo)
	{
		if (timeZone.Bias == registryTimeZoneInfo.Bias && timeZone.StandardBias == registryTimeZoneInfo.StandardBias)
		{
			return timeZone.StandardDate.Equals(in registryTimeZoneInfo.StandardDate);
		}
		return false;
	}

	private static bool TryCompareTimeZoneInformationToRegistry(in Interop.Kernel32.TIME_ZONE_INFORMATION timeZone, string id, out bool dstDisabled)
	{
		dstDisabled = false;
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones\\" + id, writable: false);
		if (registryKey == null)
		{
			return false;
		}
		if (!TryGetTimeZoneEntryFromRegistry(registryKey, "TZI", out var dtzi))
		{
			return false;
		}
		if (!TryCompareStandardDate(in timeZone, in dtzi))
		{
			return false;
		}
		bool flag = dstDisabled || CheckDaylightSavingTimeNotSupported(in timeZone) || (timeZone.DaylightBias == dtzi.DaylightBias && timeZone.DaylightDate.Equals(in dtzi.DaylightDate));
		if (flag)
		{
			string a = registryKey.GetValue("Std", string.Empty) as string;
			flag = string.Equals(a, timeZone.GetStandardName(), StringComparison.Ordinal);
		}
		return flag;
	}

	private static string GetLocalizedNameByMuiNativeResource(string resource)
	{
		if (string.IsNullOrEmpty(resource) || (GlobalizationMode.Invariant && GlobalizationMode.PredefinedCulturesOnly))
		{
			return string.Empty;
		}
		string[] array = resource.Split(',');
		if (array.Length != 2)
		{
			return string.Empty;
		}
		if (!int.TryParse(array[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return string.Empty;
		}
		result = -result;
		CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
		string systemDirectory = Environment.SystemDirectory;
		string path = $"{array[0].AsSpan().TrimStart('@')}.mui";
		try
		{
			while (cultureInfo.Name.Length != 0)
			{
				string text = Path.Join(systemDirectory, cultureInfo.Name, path);
				if (File.Exists(text))
				{
					return GetLocalizedNameByNativeResource(text, result);
				}
				cultureInfo = cultureInfo.Parent;
			}
		}
		catch (ArgumentException)
		{
		}
		return string.Empty;
	}

	private unsafe static string GetLocalizedNameByNativeResource(string filePath, int resource)
	{
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			intPtr = Interop.Kernel32.LoadLibraryEx(filePath, IntPtr.Zero, 2);
			if (intPtr != IntPtr.Zero)
			{
				char* ptr = stackalloc char[500];
				int num = Interop.User32.LoadString(intPtr, (uint)resource, ptr, 500);
				if (num != 0)
				{
					return new string(ptr, 0, num);
				}
			}
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Interop.Kernel32.FreeLibrary(intPtr);
			}
		}
		return string.Empty;
	}

	private static void GetLocalizedNamesByRegistryKey(RegistryKey key, out string displayName, out string standardName, out string daylightName)
	{
		displayName = string.Empty;
		standardName = string.Empty;
		daylightName = string.Empty;
		string text = key.GetValue("MUI_Display", string.Empty) as string;
		string text2 = key.GetValue("MUI_Std", string.Empty) as string;
		string text3 = key.GetValue("MUI_Dlt", string.Empty) as string;
		if (!string.IsNullOrEmpty(text))
		{
			displayName = GetLocalizedNameByMuiNativeResource(text);
		}
		if (!string.IsNullOrEmpty(text2))
		{
			standardName = GetLocalizedNameByMuiNativeResource(text2);
		}
		if (!string.IsNullOrEmpty(text3))
		{
			daylightName = GetLocalizedNameByMuiNativeResource(text3);
		}
		if (string.IsNullOrEmpty(displayName))
		{
			displayName = key.GetValue("Display", string.Empty) as string;
		}
		if (string.IsNullOrEmpty(standardName))
		{
			standardName = key.GetValue("Std", string.Empty) as string;
		}
		if (string.IsNullOrEmpty(daylightName))
		{
			daylightName = key.GetValue("Dlt", string.Empty) as string;
		}
	}

	private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachine(string id, out TimeZoneInfo value, out Exception e)
	{
		e = null;
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones\\" + id, writable: false);
		if (registryKey == null)
		{
			value = null;
			return TimeZoneInfoResult.TimeZoneNotFoundException;
		}
		if (!TryGetTimeZoneEntryFromRegistry(registryKey, "TZI", out var dtzi))
		{
			value = null;
			return TimeZoneInfoResult.InvalidTimeZoneException;
		}
		if (!TryCreateAdjustmentRules(id, in dtzi, out var rules, out e, dtzi.Bias))
		{
			value = null;
			return TimeZoneInfoResult.InvalidTimeZoneException;
		}
		GetLocalizedNamesByRegistryKey(registryKey, out var displayName, out var standardName, out var daylightName);
		try
		{
			value = new TimeZoneInfo(id, new TimeSpan(0, -dtzi.Bias, 0), displayName, standardName, daylightName, rules, disableDaylightSavingTime: false);
			return TimeZoneInfoResult.Success;
		}
		catch (ArgumentException ex)
		{
			value = null;
			e = ex;
			return TimeZoneInfoResult.InvalidTimeZoneException;
		}
		catch (InvalidTimeZoneException ex2)
		{
			value = null;
			e = ex2;
			return TimeZoneInfoResult.InvalidTimeZoneException;
		}
	}

	private static string GetUtcStandardDisplayName()
	{
		CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
		if (currentUICulture.Name.Length == 0 || currentUICulture.TwoLetterISOLanguageName == "en")
		{
			return "Coordinated Universal Time";
		}
		string text = null;
		using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones\\UTC", writable: false))
		{
			if (registryKey != null)
			{
				string text2 = registryKey.GetValue("MUI_Std", string.Empty) as string;
				if (!string.IsNullOrEmpty(text2))
				{
					text = GetLocalizedNameByMuiNativeResource(text2);
				}
				if (string.IsNullOrEmpty(text))
				{
					text = registryKey.GetValue("Std", string.Empty) as string;
				}
			}
		}
		switch (text)
		{
		case null:
		case "GMT":
		case "UTC":
			text = "Coordinated Universal Time";
			break;
		}
		return text;
	}

	private static string GetUtcFullDisplayName(string timeZoneId, string standardDisplayName)
	{
		return "(UTC) " + standardDisplayName;
	}
}
