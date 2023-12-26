using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ReLogic.Reflection;

public class IdDictionary
{
	private readonly Dictionary<string, int> _nameToId = new Dictionary<string, int>();

	private Dictionary<int, string> _idToName;

	public readonly int Count;

	public IEnumerable<string> Names => _nameToId.Keys;

	private IdDictionary(int count)
	{
		Count = count;
	}

	public bool TryGetName(int id, out string name)
	{
		return _idToName.TryGetValue(id, out name);
	}

	public bool TryGetId(string name, out int id)
	{
		return _nameToId.TryGetValue(name, out id);
	}

	public bool ContainsName(string name)
	{
		return _nameToId.ContainsKey(name);
	}

	public bool ContainsId(int id)
	{
		return _idToName.ContainsKey(id);
	}

	public string GetName(int id)
	{
		return _idToName[id];
	}

	public int GetId(string name)
	{
		return _nameToId[name];
	}

	public void Add(string name, int id)
	{
		_idToName.Add(id, name);
		_nameToId.Add(name, id);
	}

	public void Remove(string name)
	{
		_idToName.Remove(_nameToId[name]);
		_nameToId.Remove(name);
	}

	public void Remove(int id)
	{
		_nameToId.Remove(_idToName[id]);
		_idToName.Remove(id);
	}

	public static IdDictionary Create(Type idClass, Type idType)
	{
		int num = int.MaxValue;
		FieldInfo fieldInfo = idClass.GetFields().FirstOrDefault((FieldInfo field) => field.Name == "Count");
		if (fieldInfo != null)
		{
			num = Convert.ToInt32(fieldInfo.GetValue(null));
			if (num == 0)
			{
				throw new Exception("IdDictionary cannot be created before Count field is initialized. Move to bottom of static class");
			}
		}
		IdDictionary dictionary = new IdDictionary(num);
		(from f in idClass.GetFields(BindingFlags.Static | BindingFlags.Public)
			where f.FieldType == idType
			where f.GetCustomAttribute<ObsoleteAttribute>() == null
			select f).ToList().ForEach(delegate(FieldInfo field)
		{
			int num2 = Convert.ToInt32(field.GetValue(null));
			if (num2 < dictionary.Count)
			{
				dictionary._nameToId.Add(field.Name, num2);
			}
		});
		dictionary._idToName = dictionary._nameToId.ToDictionary((KeyValuePair<string, int> kp) => kp.Value, (KeyValuePair<string, int> kp) => kp.Key);
		return dictionary;
	}

	public static IdDictionary Create<IdClass, IdType>()
	{
		return Create(typeof(IdClass), typeof(IdType));
	}
}
