using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Text.RegularExpressions;

internal sealed class RegexCache
{
	internal readonly struct Key : IEquatable<Key>
	{
		private readonly string _pattern;

		private readonly string _culture;

		private readonly RegexOptions _options;

		private readonly TimeSpan _matchTimeout;

		public Key(string pattern, string culture, RegexOptions options, TimeSpan matchTimeout)
		{
			_pattern = pattern;
			_culture = culture;
			_options = options;
			_matchTimeout = matchTimeout;
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is Key other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(Key other)
		{
			if (_pattern.Equals(other._pattern) && _culture.Equals(other._culture) && _options == other._options)
			{
				return _matchTimeout == other._matchTimeout;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return _pattern.GetHashCode() ^ (int)_options;
		}
	}

	private sealed class Node
	{
		public readonly Key Key;

		public readonly Regex Regex;

		public long LastAccessStamp;

		public Node(Key key, Regex regex)
		{
			Key = key;
			Regex = regex;
		}
	}

	private static volatile Node s_lastAccessed;

	private static readonly ConcurrentDictionary<Key, Node> s_cacheDictionary = new ConcurrentDictionary<Key, Node>(1, 31);

	private static readonly List<Node> s_cacheList = new List<Node>(15);

	private static readonly Random s_random = new Random();

	private static int s_maxCacheSize = 15;

	private static object SyncObj => s_cacheDictionary;

	public static int MaxCacheSize
	{
		get
		{
			return s_maxCacheSize;
		}
		set
		{
			lock (SyncObj)
			{
				s_maxCacheSize = value;
				if (value == 0)
				{
					s_cacheDictionary.Clear();
					s_cacheList.Clear();
					s_lastAccessed = null;
				}
				else if (value < s_cacheList.Count)
				{
					s_cacheList.Sort((Node n1, Node n2) => Volatile.Read(ref n2.LastAccessStamp).CompareTo(Volatile.Read(ref n1.LastAccessStamp)));
					s_lastAccessed = s_cacheList[0];
					for (int i = value; i < s_cacheList.Count; i++)
					{
						s_cacheDictionary.TryRemove(s_cacheList[i].Key, out var _);
					}
					s_cacheList.RemoveRange(value, s_cacheList.Count - value);
				}
			}
		}
	}

	public static Regex GetOrAdd(string pattern)
	{
		Regex.ValidatePattern(pattern);
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		Key key = new Key(pattern, currentCulture.ToString(), RegexOptions.None, Regex.s_defaultMatchTimeout);
		Regex regex = Get(key);
		if (regex == null)
		{
			regex = new Regex(pattern, currentCulture);
			Add(key, regex);
		}
		return regex;
	}

	public static Regex GetOrAdd(string pattern, RegexOptions options, TimeSpan matchTimeout)
	{
		Regex.ValidatePattern(pattern);
		Regex.ValidateOptions(options);
		Regex.ValidateMatchTimeout(matchTimeout);
		CultureInfo cultureInfo = (((options & RegexOptions.CultureInvariant) != 0) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
		Key key = new Key(pattern, cultureInfo.ToString(), options, matchTimeout);
		Regex regex = Get(key);
		if (regex == null)
		{
			regex = new Regex(pattern, options, matchTimeout, cultureInfo);
			Add(key, regex);
		}
		return regex;
	}

	private static Regex Get(Key key)
	{
		long num = 0L;
		Node node = s_lastAccessed;
		if (node != null)
		{
			if (key.Equals(node.Key))
			{
				return node.Regex;
			}
			num = Volatile.Read(ref node.LastAccessStamp);
		}
		if (s_maxCacheSize != 0 && s_cacheDictionary.TryGetValue(key, out var value))
		{
			Volatile.Write(ref value.LastAccessStamp, num + 1);
			s_lastAccessed = value;
			return value.Regex;
		}
		return null;
	}

	private static void Add(Key key, Regex regex)
	{
		lock (SyncObj)
		{
			if (s_maxCacheSize == 0 || s_cacheDictionary.TryGetValue(key, out var value))
			{
				return;
			}
			if (s_cacheList.Count == s_maxCacheSize)
			{
				int num;
				bool flag;
				if (s_maxCacheSize <= 30)
				{
					num = s_cacheList.Count;
					flag = false;
				}
				else
				{
					num = 30;
					flag = true;
				}
				int index = (flag ? s_random.Next(s_cacheList.Count) : 0);
				long num2 = Volatile.Read(ref s_cacheList[index].LastAccessStamp);
				for (int i = 1; i < num; i++)
				{
					int num3 = (flag ? s_random.Next(s_cacheList.Count) : i);
					long num4 = Volatile.Read(ref s_cacheList[num3].LastAccessStamp);
					if (num4 < num2)
					{
						index = num3;
						num2 = num4;
					}
				}
				s_cacheDictionary.TryRemove(s_cacheList[index].Key, out value);
				s_cacheList.RemoveAt(index);
			}
			Node node = (s_lastAccessed = new Node(key, regex));
			s_cacheList.Add(node);
			s_cacheDictionary.TryAdd(key, node);
		}
	}
}
