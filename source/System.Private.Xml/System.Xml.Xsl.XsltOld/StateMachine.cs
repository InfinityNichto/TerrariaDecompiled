using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class StateMachine
{
	private int _State;

	private static readonly int[][] s_BeginTransitions = new int[10][]
	{
		new int[12]
		{
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
			16, 16
		},
		new int[12]
		{
			40961, 42241, 16, 16, 41985, 16, 16, 41985, 40961, 106497,
			16, 16
		},
		new int[12]
		{
			16, 261, 16, 16, 5, 16, 16, 5, 16, 16,
			16, 16
		},
		new int[12]
		{
			16, 258, 16, 16, 2, 16, 16, 16, 16, 16,
			16, 16
		},
		new int[12]
		{
			8200, 9480, 259, 3, 9224, 262, 6, 9224, 8, 73736,
			10, 11
		},
		new int[12]
		{
			8200, 9480, 259, 3, 9224, 262, 6, 9224, 8, 73736,
			10, 11
		},
		new int[12]
		{
			8200, 9480, 259, 3, 9224, 262, 6, 9224, 8, 73736,
			10, 11
		},
		new int[12]
		{
			8203, 9483, 16, 16, 9227, 16, 16, 9227, 8203, 73739,
			16, 16
		},
		new int[12]
		{
			8202, 9482, 16, 16, 9226, 16, 16, 9226, 8202, 73738,
			16, 16
		},
		new int[12]
		{
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
			16, 16
		}
	};

	private static readonly int[][] s_EndTransitions = new int[10][]
	{
		new int[12]
		{
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
			48, 48
		},
		new int[12]
		{
			48, 94217, 48, 48, 94729, 48, 48, 94729, 92681, 92681,
			48, 48
		},
		new int[12]
		{
			48, 48, 48, 48, 48, 7, 519, 48, 48, 48,
			48, 48
		},
		new int[12]
		{
			48, 48, 4, 516, 48, 48, 48, 48, 48, 48,
			48, 48
		},
		new int[12]
		{
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
			48, 48
		},
		new int[12]
		{
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
			48, 48
		},
		new int[12]
		{
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
			48, 48
		},
		new int[12]
		{
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
			48, 16393
		},
		new int[12]
		{
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
			16393, 48
		},
		new int[12]
		{
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
			48, 48
		}
	};

	internal int State
	{
		get
		{
			return _State;
		}
		set
		{
			_State = value;
		}
	}

	internal StateMachine()
	{
		_State = 0;
	}

	internal void Reset()
	{
		_State = 0;
	}

	internal int BeginOutlook(XPathNodeType nodeType)
	{
		return s_BeginTransitions[(int)nodeType][_State];
	}

	internal int Begin(XPathNodeType nodeType)
	{
		int num = s_BeginTransitions[(int)nodeType][_State];
		if (num != 16 && num != 32)
		{
			_State = num & 0xF;
		}
		return num;
	}

	internal int EndOutlook(XPathNodeType nodeType)
	{
		return s_EndTransitions[(int)nodeType][_State];
	}

	internal int End(XPathNodeType nodeType)
	{
		int num = s_EndTransitions[(int)nodeType][_State];
		if (num != 16 && num != 32)
		{
			_State = num & 0xF;
		}
		return num;
	}
}
