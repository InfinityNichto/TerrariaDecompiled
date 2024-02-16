using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics;

[EventSource(Name = "Microsoft-Diagnostics-DiagnosticSource")]
[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2113:ReflectionToRequiresUnreferencedCode", Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves methods on Delegate and MulticastDelegate because the nested type OverrideEventProvider's base type EventProvider defines a delegate. This includes Delegate and MulticastDelegate methods which require unreferenced code, but EnsureDescriptorsInitialized does not access these members and is safe to call.")]
[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2115:ReflectionToDynamicallyAccessedMembers", Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves methods on Delegate and MulticastDelegate because the nested type OverrideEventProvider's base type EventProvider defines a delegate. This includes Delegate and MulticastDelegate methods which have dynamically accessed members requirements, but EnsureDescriptorsInitialized does not access these members and is safe to call.")]
internal sealed class DiagnosticSourceEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Messages = (EventKeywords)1L;

		public const EventKeywords Events = (EventKeywords)2L;

		public const EventKeywords IgnoreShortCutKeywords = (EventKeywords)2048L;

		public const EventKeywords AspNetCoreHosting = (EventKeywords)4096L;

		public const EventKeywords EntityFrameworkCoreCommands = (EventKeywords)8192L;
	}

	[Flags]
	internal enum ActivityEvents
	{
		None = 0,
		ActivityStart = 1,
		ActivityStop = 2,
		All = 3
	}

	internal sealed class FilterAndTransform
	{
		public FilterAndTransform Next;

		private IDisposable _diagnosticsListenersSubscription;

		private Subscriptions _liveSubscriptions;

		private readonly bool _noImplicitTransforms;

		private ImplicitTransformEntry _firstImplicitTransformsEntry;

		private ConcurrentDictionary<Type, TransformSpec> _implicitTransformsTable;

		private readonly TransformSpec _explicitTransforms;

		private readonly DiagnosticSourceEventSource _eventSource;

		internal string SourceName { get; set; }

		internal string ActivityName { get; set; }

		internal ActivityEvents Events { get; set; }

		internal ActivitySamplingResult SamplingResult { get; set; }

		public static void CreateFilterAndTransformList(ref FilterAndTransform specList, string filterAndPayloadSpecs, DiagnosticSourceEventSource eventSource)
		{
			DestroyFilterAndTransformList(ref specList, eventSource);
			if (filterAndPayloadSpecs == null)
			{
				filterAndPayloadSpecs = "";
			}
			int num = filterAndPayloadSpecs.Length;
			while (true)
			{
				if (0 < num && char.IsWhiteSpace(filterAndPayloadSpecs[num - 1]))
				{
					num--;
					continue;
				}
				int num2 = filterAndPayloadSpecs.LastIndexOf('\n', num - 1, num);
				int i = 0;
				if (0 <= num2)
				{
					i = num2 + 1;
				}
				for (; i < num && char.IsWhiteSpace(filterAndPayloadSpecs[i]); i++)
				{
				}
				if (IsActivitySourceEntry(filterAndPayloadSpecs, i, num))
				{
					AddNewActivitySourceTransform(filterAndPayloadSpecs, i, num, eventSource);
				}
				else
				{
					specList = new FilterAndTransform(filterAndPayloadSpecs, i, num, eventSource, specList);
				}
				num = num2;
				if (num < 0)
				{
					break;
				}
			}
			if (eventSource._activitySourceSpecs != null)
			{
				NormalizeActivitySourceSpecsList(eventSource);
				CreateActivityListener(eventSource);
			}
		}

		public static void DestroyFilterAndTransformList(ref FilterAndTransform specList, DiagnosticSourceEventSource eventSource)
		{
			eventSource._activityListener?.Dispose();
			eventSource._activityListener = null;
			eventSource._activitySourceSpecs = null;
			FilterAndTransform filterAndTransform = specList;
			specList = null;
			while (filterAndTransform != null)
			{
				filterAndTransform.Dispose();
				filterAndTransform = filterAndTransform.Next;
			}
		}

		public FilterAndTransform(string filterAndPayloadSpec, int startIdx, int endIdx, DiagnosticSourceEventSource eventSource, FilterAndTransform next)
		{
			FilterAndTransform filterAndTransform = this;
			Next = next;
			_eventSource = eventSource;
			string listenerNameFilter = null;
			string eventNameFilter = null;
			string text = null;
			int num = startIdx;
			int num2 = endIdx;
			int num3 = filterAndPayloadSpec.IndexOf(':', startIdx, endIdx - startIdx);
			if (0 <= num3)
			{
				num2 = num3;
				num = num3 + 1;
			}
			int num4 = filterAndPayloadSpec.IndexOf('/', startIdx, num2 - startIdx);
			if (0 <= num4)
			{
				listenerNameFilter = filterAndPayloadSpec.Substring(startIdx, num4 - startIdx);
				int num5 = filterAndPayloadSpec.IndexOf('@', num4 + 1, num2 - num4 - 1);
				if (0 <= num5)
				{
					text = filterAndPayloadSpec.Substring(num5 + 1, num2 - num5 - 1);
					eventNameFilter = filterAndPayloadSpec.Substring(num4 + 1, num5 - num4 - 1);
				}
				else
				{
					eventNameFilter = filterAndPayloadSpec.Substring(num4 + 1, num2 - num4 - 1);
				}
			}
			else if (startIdx < num2)
			{
				listenerNameFilter = filterAndPayloadSpec.Substring(startIdx, num2 - startIdx);
			}
			_eventSource.Message("DiagnosticSource: Enabling '" + (listenerNameFilter ?? "*") + "/" + (eventNameFilter ?? "*") + "'");
			if (num < endIdx && filterAndPayloadSpec[num] == '-')
			{
				_eventSource.Message("DiagnosticSource: suppressing implicit transforms.");
				_noImplicitTransforms = true;
				num++;
			}
			if (num < endIdx)
			{
				while (true)
				{
					int num6 = num;
					int num7 = filterAndPayloadSpec.LastIndexOf(';', endIdx - 1, endIdx - num);
					if (0 <= num7)
					{
						num6 = num7 + 1;
					}
					if (num6 < endIdx)
					{
						if (_eventSource.IsEnabled(EventLevel.Informational, (EventKeywords)1L))
						{
							_eventSource.Message("DiagnosticSource: Parsing Explicit Transform '" + filterAndPayloadSpec.Substring(num6, endIdx - num6) + "'");
						}
						_explicitTransforms = new TransformSpec(filterAndPayloadSpec, num6, endIdx, _explicitTransforms);
					}
					if (num == num6)
					{
						break;
					}
					endIdx = num7;
				}
			}
			Action<string, string, IEnumerable<KeyValuePair<string, string>>> writeEvent = null;
			if (text != null && text.Contains("Activity"))
			{
				writeEvent = text switch
				{
					"Activity1Start" => _eventSource.Activity1Start, 
					"Activity1Stop" => _eventSource.Activity1Stop, 
					"Activity2Start" => _eventSource.Activity2Start, 
					"Activity2Stop" => _eventSource.Activity2Stop, 
					"RecursiveActivity1Start" => _eventSource.RecursiveActivity1Start, 
					"RecursiveActivity1Stop" => _eventSource.RecursiveActivity1Stop, 
					_ => null, 
				};
				if (writeEvent == null)
				{
					_eventSource.Message("DiagnosticSource: Could not find Event to log Activity " + text);
				}
			}
			if (writeEvent == null)
			{
				writeEvent = _eventSource.Event;
			}
			_diagnosticsListenersSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(delegate(DiagnosticListener newListener)
			{
				if (listenerNameFilter == null || listenerNameFilter == newListener.Name)
				{
					filterAndTransform._eventSource.NewDiagnosticListener(newListener.Name);
					Predicate<string> isEnabled = null;
					if (eventNameFilter != null)
					{
						isEnabled = (string eventName) => eventNameFilter == eventName;
					}
					IDisposable subscription = newListener.Subscribe(new CallbackObserver<KeyValuePair<string, object>>(OnEventWritten), isEnabled);
					filterAndTransform._liveSubscriptions = new Subscriptions(subscription, filterAndTransform._liveSubscriptions);
				}
				[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "DiagnosticSource.Write is marked with RequiresUnreferencedCode.")]
				void OnEventWritten(KeyValuePair<string, object> evnt)
				{
					if (eventNameFilter == null || !(eventNameFilter != evnt.Key))
					{
						List<KeyValuePair<string, string>> arg = filterAndTransform.Morph(evnt.Value);
						string key = evnt.Key;
						writeEvent(newListener.Name, key, arg);
					}
				}
			}));
		}

		internal FilterAndTransform(string filterAndPayloadSpec, int endIdx, int colonIdx, string activitySourceName, string activityName, ActivityEvents events, ActivitySamplingResult samplingResult, DiagnosticSourceEventSource eventSource)
		{
			_eventSource = eventSource;
			Next = _eventSource._activitySourceSpecs;
			_eventSource._activitySourceSpecs = this;
			SourceName = activitySourceName;
			ActivityName = activityName;
			Events = events;
			SamplingResult = samplingResult;
			if (colonIdx < 0)
			{
				return;
			}
			int num = colonIdx + 1;
			if (num < endIdx && filterAndPayloadSpec[num] == '-')
			{
				_eventSource.Message("DiagnosticSource: suppressing implicit transforms.");
				_noImplicitTransforms = true;
				num++;
			}
			if (num >= endIdx)
			{
				return;
			}
			while (true)
			{
				int num2 = num;
				int num3 = filterAndPayloadSpec.LastIndexOf(';', endIdx - 1, endIdx - num);
				if (0 <= num3)
				{
					num2 = num3 + 1;
				}
				if (num2 < endIdx)
				{
					if (_eventSource.IsEnabled(EventLevel.Informational, (EventKeywords)1L))
					{
						_eventSource.Message("DiagnosticSource: Parsing Explicit Transform '" + filterAndPayloadSpec.Substring(num2, endIdx - num2) + "'");
					}
					_explicitTransforms = new TransformSpec(filterAndPayloadSpec, num2, endIdx, _explicitTransforms);
				}
				if (num != num2)
				{
					endIdx = num3;
					continue;
				}
				break;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsActivitySourceEntry(string filterAndPayloadSpec, int startIdx, int endIdx)
		{
			return filterAndPayloadSpec.AsSpan(startIdx, endIdx - startIdx).StartsWith("[AS]".AsSpan(), StringComparison.Ordinal);
		}

		internal static void AddNewActivitySourceTransform(string filterAndPayloadSpec, int startIdx, int endIdx, DiagnosticSourceEventSource eventSource)
		{
			ActivityEvents events = ActivityEvents.All;
			ActivitySamplingResult samplingResult = ActivitySamplingResult.AllDataAndRecorded;
			int num = filterAndPayloadSpec.IndexOf(':', startIdx + "[AS]".Length, endIdx - startIdx - "[AS]".Length);
			ReadOnlySpan<char> readOnlySpan = filterAndPayloadSpec.AsSpan(startIdx + "[AS]".Length, ((num >= 0) ? num : endIdx) - startIdx - "[AS]".Length).Trim();
			int num2 = readOnlySpan.IndexOf('/');
			ReadOnlySpan<char> span;
			if (num2 >= 0)
			{
				span = readOnlySpan.Slice(0, num2).Trim();
				ReadOnlySpan<char> readOnlySpan2 = readOnlySpan.Slice(num2 + 1, readOnlySpan.Length - num2 - 1).Trim();
				int num3 = readOnlySpan2.IndexOf('-');
				ReadOnlySpan<char> span2;
				if (num3 >= 0)
				{
					span2 = readOnlySpan2.Slice(0, num3).Trim();
					readOnlySpan2 = readOnlySpan2.Slice(num3 + 1, readOnlySpan2.Length - num3 - 1).Trim();
					if (readOnlySpan2.Length > 0)
					{
						if (MemoryExtensions.Equals(readOnlySpan2, "Propagate".AsSpan(), StringComparison.OrdinalIgnoreCase))
						{
							samplingResult = ActivitySamplingResult.PropagationData;
						}
						else
						{
							if (!MemoryExtensions.Equals(readOnlySpan2, "Record".AsSpan(), StringComparison.OrdinalIgnoreCase))
							{
								return;
							}
							samplingResult = ActivitySamplingResult.AllData;
						}
					}
				}
				else
				{
					span2 = readOnlySpan2;
				}
				if (span2.Length > 0)
				{
					if (MemoryExtensions.Equals(span2, "Start".AsSpan(), StringComparison.OrdinalIgnoreCase))
					{
						events = ActivityEvents.ActivityStart;
					}
					else
					{
						if (!MemoryExtensions.Equals(span2, "Stop".AsSpan(), StringComparison.OrdinalIgnoreCase))
						{
							return;
						}
						events = ActivityEvents.ActivityStop;
					}
				}
			}
			else
			{
				span = readOnlySpan;
			}
			string activityName = null;
			int num4 = span.IndexOf('+');
			if (num4 >= 0)
			{
				activityName = span.Slice(num4 + 1).Trim().ToString();
				span = span.Slice(0, num4).Trim();
			}
			FilterAndTransform filterAndTransform = new FilterAndTransform(filterAndPayloadSpec, endIdx, num, span.ToString(), activityName, events, samplingResult, eventSource);
		}

		private static ActivitySamplingResult Sample(string activitySourceName, string activityName, DiagnosticSourceEventSource eventSource)
		{
			FilterAndTransform filterAndTransform = eventSource._activitySourceSpecs;
			ActivitySamplingResult activitySamplingResult = ActivitySamplingResult.None;
			ActivitySamplingResult activitySamplingResult2 = ActivitySamplingResult.None;
			while (filterAndTransform != null)
			{
				if (filterAndTransform.ActivityName == null || filterAndTransform.ActivityName == activityName)
				{
					if (activitySourceName == filterAndTransform.SourceName)
					{
						if (filterAndTransform.SamplingResult > activitySamplingResult)
						{
							activitySamplingResult = filterAndTransform.SamplingResult;
						}
						if (activitySamplingResult >= ActivitySamplingResult.AllDataAndRecorded)
						{
							return activitySamplingResult;
						}
					}
					else if (filterAndTransform.SourceName == "*")
					{
						if (activitySamplingResult != 0)
						{
							return activitySamplingResult;
						}
						if (filterAndTransform.SamplingResult > activitySamplingResult2)
						{
							activitySamplingResult2 = filterAndTransform.SamplingResult;
						}
					}
				}
				filterAndTransform = filterAndTransform.Next;
			}
			if (activitySamplingResult == ActivitySamplingResult.None)
			{
				return activitySamplingResult2;
			}
			return activitySamplingResult;
		}

		internal static void CreateActivityListener(DiagnosticSourceEventSource eventSource)
		{
			eventSource._activityListener = new ActivityListener();
			eventSource._activityListener.SampleUsingParentId = delegate(ref ActivityCreationOptions<string> activityOptions)
			{
				return Sample(activityOptions.Source.Name, activityOptions.Name, eventSource);
			};
			eventSource._activityListener.Sample = delegate(ref ActivityCreationOptions<ActivityContext> activityOptions)
			{
				return Sample(activityOptions.Source.Name, activityOptions.Name, eventSource);
			};
			eventSource._activityListener.ShouldListenTo = delegate(ActivitySource activitySource)
			{
				for (FilterAndTransform filterAndTransform = eventSource._activitySourceSpecs; filterAndTransform != null; filterAndTransform = filterAndTransform.Next)
				{
					if (activitySource.Name == filterAndTransform.SourceName || filterAndTransform.SourceName == "*")
					{
						return true;
					}
				}
				return false;
			};
			eventSource._activityListener.ActivityStarted = delegate(Activity activity)
			{
				OnActivityStarted(eventSource, activity);
			};
			eventSource._activityListener.ActivityStopped = delegate(Activity activity)
			{
				OnActivityStopped(eventSource, activity);
			};
			ActivitySource.AddActivityListener(eventSource._activityListener);
		}

		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Activity))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityContext))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityEvent))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityLink))]
		[DynamicDependency("Ticks", typeof(DateTime))]
		[DynamicDependency("Ticks", typeof(TimeSpan))]
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
		private static void OnActivityStarted(DiagnosticSourceEventSource eventSource, Activity activity)
		{
			for (FilterAndTransform filterAndTransform = eventSource._activitySourceSpecs; filterAndTransform != null; filterAndTransform = filterAndTransform.Next)
			{
				if ((filterAndTransform.Events & ActivityEvents.ActivityStart) != 0 && (activity.Source.Name == filterAndTransform.SourceName || filterAndTransform.SourceName == "*") && (filterAndTransform.ActivityName == null || filterAndTransform.ActivityName == activity.OperationName))
				{
					eventSource.ActivityStart(activity.Source.Name, activity.OperationName, filterAndTransform.Morph(activity));
					break;
				}
			}
		}

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
		private static void OnActivityStopped(DiagnosticSourceEventSource eventSource, Activity activity)
		{
			for (FilterAndTransform filterAndTransform = eventSource._activitySourceSpecs; filterAndTransform != null; filterAndTransform = filterAndTransform.Next)
			{
				if ((filterAndTransform.Events & ActivityEvents.ActivityStop) != 0 && (activity.Source.Name == filterAndTransform.SourceName || filterAndTransform.SourceName == "*") && (filterAndTransform.ActivityName == null || filterAndTransform.ActivityName == activity.OperationName))
				{
					eventSource.ActivityStop(activity.Source.Name, activity.OperationName, filterAndTransform.Morph(activity));
					break;
				}
			}
		}

		internal static void NormalizeActivitySourceSpecsList(DiagnosticSourceEventSource eventSource)
		{
			FilterAndTransform filterAndTransform = eventSource._activitySourceSpecs;
			FilterAndTransform filterAndTransform2 = null;
			FilterAndTransform filterAndTransform3 = null;
			FilterAndTransform filterAndTransform4 = null;
			FilterAndTransform filterAndTransform5 = null;
			while (filterAndTransform != null)
			{
				if (filterAndTransform.SourceName == "*")
				{
					if (filterAndTransform4 == null)
					{
						filterAndTransform4 = (filterAndTransform5 = filterAndTransform);
					}
					else
					{
						filterAndTransform5.Next = filterAndTransform;
						filterAndTransform5 = filterAndTransform;
					}
				}
				else if (filterAndTransform2 == null)
				{
					filterAndTransform2 = (filterAndTransform3 = filterAndTransform);
				}
				else
				{
					filterAndTransform3.Next = filterAndTransform;
					filterAndTransform3 = filterAndTransform;
				}
				filterAndTransform = filterAndTransform.Next;
			}
			if (filterAndTransform2 != null && filterAndTransform4 != null)
			{
				filterAndTransform3.Next = filterAndTransform4;
				filterAndTransform5.Next = null;
				eventSource._activitySourceSpecs = filterAndTransform2;
			}
		}

		private void Dispose()
		{
			if (_diagnosticsListenersSubscription != null)
			{
				_diagnosticsListenersSubscription.Dispose();
				_diagnosticsListenersSubscription = null;
			}
			if (_liveSubscriptions != null)
			{
				Subscriptions subscriptions = _liveSubscriptions;
				_liveSubscriptions = null;
				while (subscriptions != null)
				{
					subscriptions.Subscription.Dispose();
					subscriptions = subscriptions.Next;
				}
			}
		}

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
		[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
		public List<KeyValuePair<string, string>> Morph(object args)
		{
			List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
			if (args != null)
			{
				if (!_noImplicitTransforms)
				{
					Type type2 = args.GetType();
					ImplicitTransformEntry firstImplicitTransformsEntry = _firstImplicitTransformsEntry;
					TransformSpec transformSpec;
					if (firstImplicitTransformsEntry != null && firstImplicitTransformsEntry.Type == type2)
					{
						transformSpec = firstImplicitTransformsEntry.Transforms;
					}
					else if (firstImplicitTransformsEntry == null)
					{
						transformSpec = MakeImplicitTransforms(type2);
						Interlocked.CompareExchange(ref _firstImplicitTransformsEntry, new ImplicitTransformEntry
						{
							Type = type2,
							Transforms = transformSpec
						}, null);
					}
					else
					{
						if (_implicitTransformsTable == null)
						{
							Interlocked.CompareExchange(ref _implicitTransformsTable, new ConcurrentDictionary<Type, TransformSpec>(1, 8), null);
						}
						transformSpec = _implicitTransformsTable.GetOrAdd(type2, (Type type) => MakeImplicitTransformsWrapper(type));
					}
					if (transformSpec != null)
					{
						for (TransformSpec transformSpec2 = transformSpec; transformSpec2 != null; transformSpec2 = transformSpec2.Next)
						{
							list.Add(transformSpec2.Morph(args));
						}
					}
				}
				if (_explicitTransforms != null)
				{
					for (TransformSpec transformSpec3 = _explicitTransforms; transformSpec3 != null; transformSpec3 = transformSpec3.Next)
					{
						KeyValuePair<string, string> item = transformSpec3.Morph(args);
						if (item.Value != null)
						{
							list.Add(item);
						}
					}
				}
			}
			return list;
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The Morph method has RequiresUnreferencedCode, but the trimmer can't see through lamdba calls.")]
			static TransformSpec MakeImplicitTransformsWrapper(Type transformType)
			{
				return MakeImplicitTransforms(transformType);
			}
		}

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
		[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
		private static TransformSpec MakeImplicitTransforms(Type type)
		{
			TransformSpec transformSpec = null;
			TypeInfo typeInfo = type.GetTypeInfo();
			PropertyInfo[] properties = typeInfo.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (!(propertyInfo.GetMethod == null) && propertyInfo.GetMethod.GetParameters().Length == 0)
				{
					transformSpec = new TransformSpec(propertyInfo.Name, 0, propertyInfo.Name.Length, transformSpec);
				}
			}
			return Reverse(transformSpec);
		}

		private static TransformSpec Reverse(TransformSpec list)
		{
			TransformSpec transformSpec = null;
			while (list != null)
			{
				TransformSpec next = list.Next;
				list.Next = transformSpec;
				transformSpec = list;
				list = next;
			}
			return transformSpec;
		}
	}

	internal sealed class ImplicitTransformEntry
	{
		public Type Type;

		public TransformSpec Transforms;
	}

	internal sealed class TransformSpec
	{
		internal sealed class PropertySpec
		{
			private class PropertyFetch
			{
				private sealed class RefTypedFetchProperty<TObject, TProperty> : PropertyFetch
				{
					private readonly Func<TObject, TProperty> _propertyFetch;

					public RefTypedFetchProperty(Type type, PropertyInfo property)
						: base(type)
					{
						_propertyFetch = (Func<TObject, TProperty>)property.GetMethod.CreateDelegate(typeof(Func<TObject, TProperty>));
					}

					public override object Fetch(object obj)
					{
						return _propertyFetch((TObject)obj);
					}
				}

				private delegate TProperty StructFunc<TStruct, TProperty>(ref TStruct thisArg);

				private sealed class ValueTypedFetchProperty<TStruct, TProperty> : PropertyFetch
				{
					private readonly StructFunc<TStruct, TProperty> _propertyFetch;

					public ValueTypedFetchProperty(Type type, PropertyInfo property)
						: base(type)
					{
						_propertyFetch = (StructFunc<TStruct, TProperty>)property.GetMethod.CreateDelegate(typeof(StructFunc<TStruct, TProperty>));
					}

					public override object Fetch(object obj)
					{
						TStruct thisArg = (TStruct)obj;
						return _propertyFetch(ref thisArg);
					}
				}

				private sealed class CurrentActivityPropertyFetch : PropertyFetch
				{
					public CurrentActivityPropertyFetch()
						: base(null)
					{
					}

					public override object Fetch(object obj)
					{
						return Activity.Current;
					}
				}

				private sealed class EnumeratePropertyFetch<ElementType> : PropertyFetch
				{
					public EnumeratePropertyFetch(Type type)
						: base(type)
					{
					}

					public override object Fetch(object obj)
					{
						return string.Join(",", (IEnumerable<ElementType>)obj);
					}
				}

				internal Type Type { get; }

				public PropertyFetch(Type type)
				{
					Type = type;
				}

				[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
				[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
				public static PropertyFetch FetcherForProperty(Type type, string propertyName)
				{
					if (propertyName == null)
					{
						return new PropertyFetch(type);
					}
					if (propertyName == "*Activity")
					{
						return new CurrentActivityPropertyFetch();
					}
					TypeInfo typeInfo = type.GetTypeInfo();
					if (propertyName == "*Enumerate")
					{
						Type[] interfaces = typeInfo.GetInterfaces();
						foreach (Type type2 in interfaces)
						{
							TypeInfo typeInfo2 = type2.GetTypeInfo();
							if (typeInfo2.IsGenericType && !(typeInfo2.GetGenericTypeDefinition() != typeof(IEnumerable<>)))
							{
								Type type3 = typeInfo2.GetGenericArguments()[0];
								Type type4 = typeof(EnumeratePropertyFetch<>).GetTypeInfo().MakeGenericType(type3);
								return (PropertyFetch)Activator.CreateInstance(type4, type);
							}
						}
						Log.Message($"*Enumerate applied to non-enumerable type {type}");
						return new PropertyFetch(type);
					}
					PropertyInfo propertyInfo = typeInfo.GetDeclaredProperty(propertyName);
					if (propertyInfo == null)
					{
						PropertyInfo[] properties = typeInfo.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
						foreach (PropertyInfo propertyInfo2 in properties)
						{
							if (propertyInfo2.Name == propertyName)
							{
								propertyInfo = propertyInfo2;
								break;
							}
						}
					}
					if (propertyInfo == null)
					{
						Log.Message($"Property {propertyName} not found on {type}. Ensure the name is spelled correctly. If you published the application with PublishTrimmed=true, ensure the property was not trimmed away.");
						return new PropertyFetch(type);
					}
					MethodInfo? getMethod = propertyInfo.GetMethod;
					if ((object)getMethod == null || !getMethod.IsStatic)
					{
						MethodInfo? setMethod = propertyInfo.SetMethod;
						if ((object)setMethod == null || !setMethod.IsStatic)
						{
							Type type5 = (typeInfo.IsValueType ? typeof(ValueTypedFetchProperty<, >) : typeof(RefTypedFetchProperty<, >));
							Type type6 = type5.GetTypeInfo().MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
							return (PropertyFetch)Activator.CreateInstance(type6, type, propertyInfo);
						}
					}
					Log.Message("Property " + propertyName + " is static.");
					return new PropertyFetch(type);
				}

				public virtual object Fetch(object obj)
				{
					return null;
				}
			}

			public PropertySpec Next;

			private readonly string _propertyName;

			private volatile PropertyFetch _fetchForExpectedType;

			public bool IsStatic { get; private set; }

			public PropertySpec(string propertyName, PropertySpec next)
			{
				Next = next;
				_propertyName = propertyName;
				if (_propertyName == "*Activity")
				{
					IsStatic = true;
				}
			}

			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
			[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
			public object Fetch(object obj)
			{
				PropertyFetch propertyFetch = _fetchForExpectedType;
				Type type = obj?.GetType();
				if (propertyFetch == null || propertyFetch.Type != type)
				{
					propertyFetch = (_fetchForExpectedType = PropertyFetch.FetcherForProperty(type, _propertyName));
				}
				object result = null;
				try
				{
					result = propertyFetch.Fetch(obj);
				}
				catch (Exception value)
				{
					Log.Message($"Property {type}.{_propertyName} threw the exception {value}");
				}
				return result;
			}
		}

		public TransformSpec Next;

		private readonly string _outputName;

		private readonly PropertySpec _fetches;

		public TransformSpec(string transformSpec, int startIdx, int endIdx, TransformSpec next = null)
		{
			Next = next;
			int num = transformSpec.IndexOf('=', startIdx, endIdx - startIdx);
			if (0 <= num)
			{
				_outputName = transformSpec.Substring(startIdx, num - startIdx);
				startIdx = num + 1;
			}
			while (startIdx < endIdx)
			{
				int num2 = transformSpec.LastIndexOf('.', endIdx - 1, endIdx - startIdx);
				int num3 = startIdx;
				if (0 <= num2)
				{
					num3 = num2 + 1;
				}
				string text = transformSpec.Substring(num3, endIdx - num3);
				_fetches = new PropertySpec(text, _fetches);
				if (_outputName == null)
				{
					_outputName = text;
				}
				endIdx = num2;
			}
		}

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
		[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
		public KeyValuePair<string, string> Morph(object obj)
		{
			for (PropertySpec propertySpec = _fetches; propertySpec != null; propertySpec = propertySpec.Next)
			{
				if (obj != null || propertySpec.IsStatic)
				{
					obj = propertySpec.Fetch(obj);
				}
			}
			return new KeyValuePair<string, string>(_outputName, obj?.ToString());
		}
	}

	internal sealed class CallbackObserver<T> : IObserver<T>
	{
		private readonly Action<T> _callback;

		public CallbackObserver(Action<T> callback)
		{
			_callback = callback;
		}

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(T value)
		{
			_callback(value);
		}
	}

	internal sealed class Subscriptions
	{
		public IDisposable Subscription;

		public Subscriptions Next;

		public Subscriptions(IDisposable subscription, Subscriptions next)
		{
			Subscription = subscription;
			Next = next;
		}
	}

	public static DiagnosticSourceEventSource Log = new DiagnosticSourceEventSource();

	private readonly string AspNetCoreHostingKeywordValue = "Microsoft.AspNetCore/Microsoft.AspNetCore.Hosting.BeginRequest@Activity1Start:-httpContext.Request.Method;httpContext.Request.Host;httpContext.Request.Path;httpContext.Request.QueryString\nMicrosoft.AspNetCore/Microsoft.AspNetCore.Hosting.EndRequest@Activity1Stop:-httpContext.TraceIdentifier;httpContext.Response.StatusCode";

	private readonly string EntityFrameworkCoreCommandsKeywordValue = "Microsoft.EntityFrameworkCore/Microsoft.EntityFrameworkCore.BeforeExecuteCommand@Activity2Start:-Command.Connection.DataSource;Command.Connection.Database;Command.CommandText\nMicrosoft.EntityFrameworkCore/Microsoft.EntityFrameworkCore.AfterExecuteCommand@Activity2Stop:-";

	private volatile bool _false;

	private FilterAndTransform _specs;

	private FilterAndTransform _activitySourceSpecs;

	private ActivityListener _activityListener;

	[Event(1, Keywords = (EventKeywords)1L)]
	public void Message(string Message)
	{
		WriteEvent(1, Message);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(2, Keywords = (EventKeywords)2L)]
	private void Event(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(2, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(4, Keywords = (EventKeywords)2L)]
	private void Activity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(4, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(5, Keywords = (EventKeywords)2L)]
	private void Activity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(5, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(6, Keywords = (EventKeywords)2L)]
	private void Activity2Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(6, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(7, Keywords = (EventKeywords)2L)]
	private void Activity2Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(7, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(8, Keywords = (EventKeywords)2L, ActivityOptions = EventActivityOptions.Recursive)]
	private void RecursiveActivity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(8, SourceName, EventName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(9, Keywords = (EventKeywords)2L, ActivityOptions = EventActivityOptions.Recursive)]
	private void RecursiveActivity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(9, SourceName, EventName, Arguments);
	}

	[Event(10, Keywords = (EventKeywords)2L)]
	private void NewDiagnosticListener(string SourceName)
	{
		WriteEvent(10, SourceName);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(11, Keywords = (EventKeywords)2L, ActivityOptions = EventActivityOptions.Recursive)]
	private void ActivityStart(string SourceName, string ActivityName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(11, SourceName, ActivityName, Arguments);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Arguments parameter is trimmer safe")]
	[Event(12, Keywords = (EventKeywords)2L, ActivityOptions = EventActivityOptions.Recursive)]
	private void ActivityStop(string SourceName, string ActivityName, IEnumerable<KeyValuePair<string, string>> Arguments)
	{
		WriteEvent(12, SourceName, ActivityName, Arguments);
	}

	private DiagnosticSourceEventSource()
		: base(EventSourceSettings.EtwSelfDescribingEventFormat)
	{
	}

	[NonEvent]
	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		BreakPointWithDebuggerFuncEval();
		lock (this)
		{
			if ((command.Command == EventCommand.Update || command.Command == EventCommand.Enable) && IsEnabled(EventLevel.Informational, (EventKeywords)2L))
			{
				string value = null;
				command.Arguments.TryGetValue("FilterAndPayloadSpecs", out value);
				if (!IsEnabled(EventLevel.Informational, (EventKeywords)2048L))
				{
					if (IsEnabled(EventLevel.Informational, (EventKeywords)4096L))
					{
						value = NewLineSeparate(value, AspNetCoreHostingKeywordValue);
					}
					if (IsEnabled(EventLevel.Informational, (EventKeywords)8192L))
					{
						value = NewLineSeparate(value, EntityFrameworkCoreCommandsKeywordValue);
					}
				}
				FilterAndTransform.CreateFilterAndTransformList(ref _specs, value, this);
			}
			else if (command.Command == EventCommand.Update || command.Command == EventCommand.Disable)
			{
				FilterAndTransform.DestroyFilterAndTransformList(ref _specs, this);
			}
		}
	}

	private static string NewLineSeparate(string str1, string str2)
	{
		if (string.IsNullOrEmpty(str1))
		{
			return str2;
		}
		return str1 + "\n" + str2;
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	[NonEvent]
	private void BreakPointWithDebuggerFuncEval()
	{
		new object();
		while (_false)
		{
			_false = false;
		}
	}
}
