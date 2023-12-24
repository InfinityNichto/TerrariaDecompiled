using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteelSeries.GameSense;

namespace ReLogic.Peripherals.RGB.SteelSeries;

public class SteelSeriesDeviceGroup : RgbDeviceGroup
{
	public static class EventNames
	{
		public const string DoRainbows = "DO_RAINBOWS";
	}

	private readonly List<RgbDevice> _devices = new List<RgbDevice>();

	private readonly List<IGameSenseUpdater> _miscEvents = new List<IGameSenseUpdater>();

	private const int ThrottleLoopSize = 12;

	private readonly List<RgbDevice> _devicesThatDontStagger = new List<RgbDevice>();

	private readonly List<List<RgbDevice>> _devicesThatStaggerByFrameGroup = new List<List<RgbDevice>>(12);

	private int _throttleCounter;

	private bool _isInitialized;

	private bool _hasConnectionToGameSense;

	private readonly VendorColorProfile _colorProfiles;

	private Bind_Event[] _bindEvents;

	private GameSenseConnection _gameSenseConnection;

	private JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings
	{
		TypeNameHandling = (TypeNameHandling)4,
		MetadataPropertyHandling = (MetadataPropertyHandling)1,
		Formatting = (Formatting)0
	});

	public SteelSeriesDeviceGroup(VendorColorProfile colorProfiles, string gameNameIdInAllCaps, string gameDisplayName, IconColor iconColor)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		_gameSenseConnection = new GameSenseConnection
		{
			GameName = gameNameIdInAllCaps,
			GameDisplayName = gameDisplayName,
			IconColor = iconColor
		};
		_colorProfiles = colorProfiles;
		GameSenseConnection gameSenseConnection = _gameSenseConnection;
		gameSenseConnection.OnConnectionBecameActive = (GameSenseConnection.ClientStateEvent)Delegate.Combine(gameSenseConnection.OnConnectionBecameActive, new GameSenseConnection.ClientStateEvent(_gameSenseConnection_OnConnectionBecameActive));
		GameSenseConnection gameSenseConnection2 = _gameSenseConnection;
		gameSenseConnection2.OnConnectionBecameInactive = (GameSenseConnection.ClientStateEvent)Delegate.Combine(gameSenseConnection2.OnConnectionBecameInactive, new GameSenseConnection.ClientStateEvent(_gameSenseConnection_OnConnectionBecameInactive));
		_gameSenseConnection.SetEvents();
	}

	private void Application_ApplicationExit(object sender, EventArgs e)
	{
		_gameSenseConnection_OnConnectionBecameInactive();
	}

	protected override void Initialize()
	{
		if (!_isInitialized)
		{
			ConnectToGameSense();
			_isInitialized = true;
			for (int i = 0; i < 12; i++)
			{
				_devicesThatStaggerByFrameGroup.Add(new List<RgbDevice>());
			}
			TrackDeviceAndAddItToList(_devicesThatDontStagger, new SteelSeriesKeyboard(_colorProfiles.Keyboard));
			int num = 27;
			int num2 = 0;
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[0], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "one", num + 1, num2 + 4));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[1], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "two", num + 2, num2 + 4));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[2], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "three", num + 3, num2 + 4));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[3], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "four", num + 4, num2 + 3));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[4], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "five", num + 4, num2 + 2));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[5], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "six", num + 4, num2 + 1));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[6], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "seven", num + 3, num2));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[7], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "eight", num + 2, num2));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[8], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "nine", num + 1, num2));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[9], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "ten", num, num2 + 1));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[10], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "eleven", num, num2 + 2));
			TrackDeviceAndAddItToList(_devicesThatStaggerByFrameGroup[11], new SteelSeriesSecondaryDeviceByZone(_colorProfiles.Generic, RgbDeviceType.Generic, "twelve", num2, num2 + 3));
		}
	}

	protected void TrackDeviceAndAddItToList(List<RgbDevice> deviceList, RgbDevice device)
	{
		_devices.Add(device);
		deviceList.Add(device);
	}

	protected override void Uninitialize()
	{
		if (_isInitialized)
		{
			_hasConnectionToGameSense = false;
			DisconnectFromGameSense();
		}
	}

	public override void LoadSpecialRules(object specialRulesObject)
	{
		if (!(specialRulesObject is GameSenseSpecificInfo gameSenseSpecificInfo))
		{
			return;
		}
		_bindEvents = gameSenseSpecificInfo.EventsToBind.ToArray();
		ARgbGameValueTracker[] array = gameSenseSpecificInfo.MiscEvents.ToArray();
		_gameSenseConnection.TryRegisteringEvents(_bindEvents);
		foreach (RgbDevice device in _devices)
		{
			if (device is IGameSenseDevice gameSenseDevice)
			{
				gameSenseDevice.CollectEventsToTrack(_bindEvents, array);
			}
		}
		ARgbGameValueTracker[] array2 = array;
		foreach (ARgbGameValueTracker theEvent in array2)
		{
			_miscEvents.Add(new SteelSeriesEventRelay(theEvent));
		}
	}

	private void SendUpdatesForBindEvents()
	{
		Bind_Event[] bindEvents = _bindEvents;
		foreach (Bind_Event bind_Event in bindEvents)
		{
			_gameSenseConnection.SendEvent(bind_Event.eventName, 1);
		}
	}

	public override IEnumerator<RgbDevice> GetEnumerator()
	{
		return _devices.GetEnumerator();
	}

	public override void OnceProcessed()
	{
		SendRelevantChangesToGameSense();
	}

	private void SendRelevantChangesToGameSense()
	{
		if (_hasConnectionToGameSense)
		{
			UpdateDeviceList(_devicesThatDontStagger);
			_throttleCounter++;
			_throttleCounter %= 12;
			UpdateDeviceList(_devicesThatStaggerByFrameGroup[_throttleCounter]);
		}
	}

	private void UpdateDeviceList(List<RgbDevice> deviceList)
	{
		foreach (RgbDevice device in deviceList)
		{
			if (!(device is IGameSenseDevice gameSenseDevice))
			{
				continue;
			}
			List<JObject> list = gameSenseDevice.TryGetEventUpdateRequest();
			if (list == null)
			{
				continue;
			}
			for (int i = 0; i < list.Count; i++)
			{
				JObject jObject = list[i];
				if (jObject != null)
				{
					jObject.Add("game", JToken.op_Implicit(_gameSenseConnection.GameName));
					string fullEventJson = ((JToken)jObject).ToString((Formatting)0, Array.Empty<JsonConverter>());
					_gameSenseConnection.SendEvent(fullEventJson);
				}
			}
		}
		foreach (IGameSenseUpdater miscEvent in _miscEvents)
		{
			List<JObject> list2 = miscEvent.TryGetEventUpdateRequest();
			if (list2 == null)
			{
				continue;
			}
			for (int j = 0; j < list2.Count; j++)
			{
				JObject jObject2 = list2[j];
				if (jObject2 != null)
				{
					jObject2.Add("game", JToken.op_Implicit(_gameSenseConnection.GameName));
					string fullEventJson2 = ((JToken)jObject2).ToString((Formatting)0, Array.Empty<JsonConverter>());
					_gameSenseConnection.SendEvent(fullEventJson2);
				}
			}
		}
	}

	private void ConnectToGameSense()
	{
		_gameSenseConnection.BeginConnection();
	}

	private void DisconnectFromGameSense()
	{
		_gameSenseConnection.EndConnection();
	}

	private void _gameSenseConnection_OnConnectionBecameActive()
	{
		_hasConnectionToGameSense = true;
	}

	private void _gameSenseConnection_OnConnectionBecameInactive()
	{
		_hasConnectionToGameSense = false;
		Uninitialize();
	}
}
