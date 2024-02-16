namespace System.ComponentModel.Design;

public class StandardCommands
{
	private static class ShellGuids
	{
		internal static readonly Guid VSStandardCommandSet97 = new Guid("{5efc7975-14bc-11cf-9b2b-00aa00573819}");

		internal static readonly Guid guidDsdCmdId = new Guid("{1F0FD094-8e53-11d2-8f9c-0060089fc486}");

		internal static readonly Guid SID_SOleComponentUIManager = new Guid("{5efc7974-14bc-11cf-9b2b-00aa00573819}");

		internal static readonly Guid GUID_VSTASKCATEGORY_DATADESIGNER = new Guid("{6B32EAED-13BB-11d3-A64F-00C04F683820}");

		internal static readonly Guid GUID_PropertyBrowserToolWindow = new Guid(-285584864, -7528, 4560, new byte[8] { 143, 120, 0, 160, 201, 17, 0, 87 });
	}

	private static readonly Guid s_standardCommandSet = ShellGuids.VSStandardCommandSet97;

	private static readonly Guid s_ndpCommandSet = new Guid("{74D21313-2AEE-11d1-8BFB-00A0C90F26F7}");

	public static readonly CommandID AlignBottom = new CommandID(s_standardCommandSet, 1);

	public static readonly CommandID AlignHorizontalCenters = new CommandID(s_standardCommandSet, 2);

	public static readonly CommandID AlignLeft = new CommandID(s_standardCommandSet, 3);

	public static readonly CommandID AlignRight = new CommandID(s_standardCommandSet, 4);

	public static readonly CommandID AlignToGrid = new CommandID(s_standardCommandSet, 5);

	public static readonly CommandID AlignTop = new CommandID(s_standardCommandSet, 6);

	public static readonly CommandID AlignVerticalCenters = new CommandID(s_standardCommandSet, 7);

	public static readonly CommandID ArrangeBottom = new CommandID(s_standardCommandSet, 8);

	public static readonly CommandID ArrangeRight = new CommandID(s_standardCommandSet, 9);

	public static readonly CommandID BringForward = new CommandID(s_standardCommandSet, 10);

	public static readonly CommandID BringToFront = new CommandID(s_standardCommandSet, 11);

	public static readonly CommandID CenterHorizontally = new CommandID(s_standardCommandSet, 12);

	public static readonly CommandID CenterVertically = new CommandID(s_standardCommandSet, 13);

	public static readonly CommandID ViewCode = new CommandID(s_standardCommandSet, 333);

	public static readonly CommandID DocumentOutline = new CommandID(s_standardCommandSet, 239);

	public static readonly CommandID Copy = new CommandID(s_standardCommandSet, 15);

	public static readonly CommandID Cut = new CommandID(s_standardCommandSet, 16);

	public static readonly CommandID Delete = new CommandID(s_standardCommandSet, 17);

	public static readonly CommandID Group = new CommandID(s_standardCommandSet, 20);

	public static readonly CommandID HorizSpaceConcatenate = new CommandID(s_standardCommandSet, 21);

	public static readonly CommandID HorizSpaceDecrease = new CommandID(s_standardCommandSet, 22);

	public static readonly CommandID HorizSpaceIncrease = new CommandID(s_standardCommandSet, 23);

	public static readonly CommandID HorizSpaceMakeEqual = new CommandID(s_standardCommandSet, 24);

	public static readonly CommandID Paste = new CommandID(s_standardCommandSet, 26);

	public static readonly CommandID Properties = new CommandID(s_standardCommandSet, 28);

	public static readonly CommandID Redo = new CommandID(s_standardCommandSet, 29);

	public static readonly CommandID MultiLevelRedo = new CommandID(s_standardCommandSet, 30);

	public static readonly CommandID SelectAll = new CommandID(s_standardCommandSet, 31);

	public static readonly CommandID SendBackward = new CommandID(s_standardCommandSet, 32);

	public static readonly CommandID SendToBack = new CommandID(s_standardCommandSet, 33);

	public static readonly CommandID SizeToControl = new CommandID(s_standardCommandSet, 35);

	public static readonly CommandID SizeToControlHeight = new CommandID(s_standardCommandSet, 36);

	public static readonly CommandID SizeToControlWidth = new CommandID(s_standardCommandSet, 37);

	public static readonly CommandID SizeToFit = new CommandID(s_standardCommandSet, 38);

	public static readonly CommandID SizeToGrid = new CommandID(s_standardCommandSet, 39);

	public static readonly CommandID SnapToGrid = new CommandID(s_standardCommandSet, 40);

	public static readonly CommandID TabOrder = new CommandID(s_standardCommandSet, 41);

	public static readonly CommandID Undo = new CommandID(s_standardCommandSet, 43);

	public static readonly CommandID MultiLevelUndo = new CommandID(s_standardCommandSet, 44);

	public static readonly CommandID Ungroup = new CommandID(s_standardCommandSet, 45);

	public static readonly CommandID VertSpaceConcatenate = new CommandID(s_standardCommandSet, 46);

	public static readonly CommandID VertSpaceDecrease = new CommandID(s_standardCommandSet, 47);

	public static readonly CommandID VertSpaceIncrease = new CommandID(s_standardCommandSet, 48);

	public static readonly CommandID VertSpaceMakeEqual = new CommandID(s_standardCommandSet, 49);

	public static readonly CommandID ShowGrid = new CommandID(s_standardCommandSet, 103);

	public static readonly CommandID ViewGrid = new CommandID(s_standardCommandSet, 125);

	public static readonly CommandID Replace = new CommandID(s_standardCommandSet, 230);

	public static readonly CommandID PropertiesWindow = new CommandID(s_standardCommandSet, 235);

	public static readonly CommandID LockControls = new CommandID(s_standardCommandSet, 369);

	public static readonly CommandID F1Help = new CommandID(s_standardCommandSet, 377);

	public static readonly CommandID ArrangeIcons = new CommandID(s_ndpCommandSet, 12298);

	public static readonly CommandID LineupIcons = new CommandID(s_ndpCommandSet, 12299);

	public static readonly CommandID ShowLargeIcons = new CommandID(s_ndpCommandSet, 12300);

	public static readonly CommandID VerbFirst = new CommandID(s_ndpCommandSet, 8192);

	public static readonly CommandID VerbLast = new CommandID(s_ndpCommandSet, 8448);
}
