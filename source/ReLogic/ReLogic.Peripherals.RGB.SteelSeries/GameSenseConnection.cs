using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using FullSerializer;
using Newtonsoft.Json.Linq;
using ReLogic.OS;
using SteelSeries.GameSense;

namespace ReLogic.Peripherals.RGB.SteelSeries;

public class GameSenseConnection
{
	public delegate void ClientStateEvent();

	public string GameName;

	public string GameDisplayName;

	public IconColor IconColor;

	private Bind_Event[] Events;

	private const string _SceneObjName = "GameSenseManager_Auto";

	private const string _GameSenseObjName = "GameSenseManager";

	private const uint _MsgQueueSize = 100u;

	private const int _ServerProbeInterval = 5000;

	private const int _MsgCheckInterval = 10;

	private const long _MaxIdleTimeBeforeHeartbeat = 1000L;

	private Thread _gameSenseThread;

	private bool _mGameSenseWrkShouldRun;

	private Uri _uriBase;

	private LocklessQueue<QueueMsg> _mMsgQueue;

	private ClientState _mClientState;

	private string _mServerPort;

	private fsSerializer _mSerializer;

	public ClientStateEvent OnConnectionBecameActive;

	public ClientStateEvent OnConnectionBecameInactive;

	private HttpWebRequest _currentRequest;

	public void SetEvents(params Bind_Event[] bindEvents)
	{
		Events = bindEvents;
	}

	public void BeginConnection()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		_mSerializer = new fsSerializer();
		_mMsgQueue = new LocklessQueue<QueueMsg>(100u);
		_gameSenseThread = new Thread(_gamesenseWrk);
		_mGameSenseWrkShouldRun = true;
		_setClientState((ClientState)1);
		try
		{
			_gameSenseThread.Start();
			_addGUIDefinedEvents();
		}
		catch (Exception e)
		{
			_logException("Could not start the client thread", e);
			_setClientState((ClientState)2);
		}
	}

	private void _addGUIDefinedEvents()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (GameName == null || GameDisplayName == null || Events == null)
		{
			_logWarning("Incomplete game registration form");
			_setClientState((ClientState)2);
		}
		else
		{
			RegisterGame(GameName, GameDisplayName, IconColor);
			RegisterEvents(Events);
		}
	}

	public void TryRegisteringEvents(Bind_Event[] theEvents)
	{
		RegisterEvents(theEvents);
	}

	private void RegisterEvents(Bind_Event[] theEvents)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		foreach (Bind_Event bind_Event in theEvents)
		{
			QueueMsg queueMsg;
			if (bind_Event.handlers == null || bind_Event.handlers.Length == 0)
			{
				queueMsg = (QueueMsg)new QueueMsgRegisterEvent();
				queueMsg.data = (object)new Register_Event(GameName, bind_Event.eventName, bind_Event.minValue, bind_Event.maxValue, bind_Event.iconId);
			}
			else
			{
				bind_Event.game = GameName;
				queueMsg = (QueueMsg)new QueueMsgBindEvent();
				queueMsg.data = bind_Event;
			}
			_mMsgQueue.PEnqueue(queueMsg);
		}
	}

	public void RegisterGame(string name, string displayName, IconColor iconColor)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		GameName = name.ToUpper();
		GameDisplayName = displayName;
		IconColor = iconColor;
		Register_Game register_Game = new Register_Game();
		register_Game.game = GameName;
		register_Game.game_display_name = GameDisplayName;
		register_Game.icon_color_id = iconColor;
		QueueMsgRegisterGame queueMsgRegisterGame = new QueueMsgRegisterGame();
		((QueueMsg)queueMsgRegisterGame).data = register_Game;
		_mMsgQueue.PEnqueue((QueueMsg)(object)queueMsgRegisterGame);
	}

	public void RemoveGame(string name)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		GameName = name.ToUpper();
		QueueMsgRemoveGame queueMsgRemoveGame = new QueueMsgRemoveGame();
		((QueueMsg)queueMsgRemoveGame).data = (object)new Game(GameName);
		_mMsgQueue.PEnqueue((QueueMsg)(object)queueMsgRemoveGame);
	}

	public void EndConnection()
	{
		_logDbgMsg("Ending Connection");
		_mGameSenseWrkShouldRun = false;
		if (_currentRequest != null)
		{
			_currentRequest.Abort();
		}
	}

	private void _gamesenseWrk()
	{
		//IL_0089: Expected O, but got Unknown
		//IL_00a5: Expected O, but got Unknown
		//IL_010c: Expected O, but got Unknown
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected I4, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		QueueMsg queueMsg = null;
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		while (_mGameSenseWrkShouldRun)
		{
			ClientState mClientState = _mClientState;
			switch ((int)mClientState)
			{
			case 0:
			{
				QueueMsg queueMsg2;
				while ((queueMsg2 = _mMsgQueue.CDequeue()) == null)
				{
					Thread.Sleep(10);
					if (stopwatch.ElapsedMilliseconds > 1000)
					{
						queueMsg2 = (QueueMsg)new QueueMsgSendHeartbeat();
						queueMsg2.data = (object)new Game(GameName);
						break;
					}
				}
				try
				{
					_sendMsg(queueMsg2);
					stopwatch.Reset();
					stopwatch.Start();
				}
				catch (ServerDownException val2)
				{
					ServerDownException e2 = val2;
					_logException("Failed connecting to GameSense server", (Exception)(object)e2);
					queueMsg = queueMsg2;
					_setClientState((ClientState)1);
				}
				catch (CriticalMessageIllFormedException val3)
				{
					CriticalMessageIllFormedException e3 = val3;
					_logException("Message ill-formed", (Exception)(object)e3);
					_setClientState((ClientState)2);
				}
				catch (Exception e4)
				{
					_logException("Failed processing msg", e4);
				}
				break;
			}
			case 1:
				_mServerPort = _getServerPort();
				if (_mServerPort == null)
				{
					_logWarning("Failed to obtain GameSense server port. GameSense will not function");
					_setClientState((ClientState)2);
					break;
				}
				_initializeUris();
				if (queueMsg != null)
				{
					try
					{
						_sendMsg(queueMsg);
						queueMsg = null;
					}
					catch (ServerDownException val)
					{
						ServerDownException e = val;
						_logException("Failed connecting to GameSense server", (Exception)(object)e);
						_logDbgMsg("Retrying in 5 seconds...");
						Thread.Sleep(5000);
						break;
					}
				}
				_setClientState((ClientState)0);
				break;
			case 2:
				_logDbgMsg("Entering inactive state");
				_mGameSenseWrkShouldRun = false;
				break;
			default:
				_logErrorMsg("Unknown GameSense client state");
				_setClientState((ClientState)2);
				break;
			}
		}
		_logDbgMsg("Worker exiting");
	}

	private void _setClientState(ClientState state)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		if (_mClientState != state)
		{
			_mClientState = state;
			if ((int)state == 0 && OnConnectionBecameActive != null)
			{
				OnConnectionBecameActive();
			}
			if ((int)state == 2 && OnConnectionBecameInactive != null)
			{
				OnConnectionBecameInactive();
			}
		}
	}

	private static void _logException(string msg, Exception e)
	{
	}

	private static void _logWarning(string msg)
	{
	}

	private static void _logDbgMsg(string msg)
	{
	}

	private static void _logErrorMsg(string msg)
	{
	}

	private void _sendMsg(QueueMsg msg)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		string text = _toJSON(msg.data);
		JsonMsg jsonMsg = (JsonMsg)(object)((msg is JsonMsg) ? msg : null);
		if (jsonMsg != null)
		{
			text = jsonMsg.JsonText;
		}
		_logDbgMsg(text);
		try
		{
			_sendServer(msg.uri, text);
		}
		catch (WebException ex)
		{
			switch (ex.Status)
			{
			case WebExceptionStatus.ProtocolError:
				if (msg.IsCritical())
				{
					Stream responseStream = ex.Response.GetResponseStream();
					string text2 = new StreamReader(responseStream, Encoding.UTF8).ReadToEnd();
					responseStream.Close();
					throw new CriticalMessageIllFormedException(text2);
				}
				break;
			case WebExceptionStatus.ConnectFailure:
				throw new ServerDownException(ex.Message);
			default:
				_logException("Unexpected status", ex);
				break;
			}
		}
	}

	private void SendJson(Uri uri, string data, bool isCritical)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		_logDbgMsg(data);
		try
		{
			_sendServer(uri, data);
		}
		catch (WebException ex)
		{
			switch (ex.Status)
			{
			case WebExceptionStatus.ProtocolError:
				if (isCritical)
				{
					Stream responseStream = ex.Response.GetResponseStream();
					string text = new StreamReader(responseStream, Encoding.UTF8).ReadToEnd();
					responseStream.Close();
					throw new CriticalMessageIllFormedException(text);
				}
				break;
			case WebExceptionStatus.ConnectFailure:
				throw new ServerDownException(ex.Message);
			default:
				_logException("Unexpected status", ex);
				throw;
			}
		}
	}

	private static string _getPropsPath()
	{
		if (Platform.IsWindows)
		{
			return Environment.ExpandEnvironmentVariables("%PROGRAMDATA%/SteelSeries/SteelSeries Engine 3/coreProps.json");
		}
		return "/Library/Application Support/SteelSeries Engine 3/coreProps.json";
	}

	private static string _readProps()
	{
		string path = _getPropsPath();
		string result = null;
		try
		{
			if (File.Exists(path))
			{
				result = File.ReadAllText(path);
				return result;
			}
			_logErrorMsg("Could not read server props file, because it can't be found");
			return result;
		}
		catch (Exception e)
		{
			_logException("Could not read server props file", e);
			return result;
		}
	}

	private static string _getServerPort()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		string result = null;
		string text = _readProps();
		if (text != null)
		{
			try
			{
				JObject jObject = JObject.Parse(text);
				coreProps coreProps = default(coreProps);
				coreProps.address = (string)jObject.GetValue("address");
				string[] array = coreProps.address.Split(':');
				Convert.ToUInt16(array[1]);
				result = array[1];
				return result;
			}
			catch (Exception e)
			{
				_logException("Cannot parse port information", e);
				return result;
			}
		}
		return result;
	}

	private void _initializeUris()
	{
		_uriBase = new Uri("http://127.0.0.1:" + _mServerPort);
		QueueMsgRegisterGame._uri = new Uri(_uriBase, "game_metadata");
		QueueMsgBindEvent._uri = new Uri(_uriBase, "bind_game_event");
		QueueMsgRegisterEvent._uri = new Uri(_uriBase, "register_game_event");
		QueueMsgSendEvent._uri = new Uri(_uriBase, "game_event");
		QueueMsgSendHeartbeat._uri = new Uri(_uriBase, "game_heartbeat");
		QueueMsgRemoveGame._uri = new Uri(_uriBase, "remove_game");
		JsonMsg._bitmapEventUri = new Uri(_uriBase, "bitmap_event");
	}

	[DebuggerNonUserCode]
	private void _sendServer(Uri uri, string data)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(data);
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
		httpWebRequest.ContentType = "application/json";
		httpWebRequest.Method = "POST";
		Stream requestStream = httpWebRequest.GetRequestStream();
		requestStream.Write(bytes, 0, bytes.Length);
		requestStream.Close();
		_currentRequest = httpWebRequest;
		try
		{
			((HttpWebResponse)httpWebRequest.GetResponse()).Close();
		}
		catch
		{
		}
	}

	private string _toJSON<T>(T obj)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		fsData data = default(fsData);
		fsResult val = _mSerializer.TrySerialize<T>(obj, ref data);
		if (((fsResult)(ref val)).Succeeded)
		{
			return fsJsonPrinter.CompressedJson(data);
		}
		throw new Exception("Failed serializing object: " + obj.ToString());
	}

	private bool _isClientActive()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		return (int)_mClientState == 0;
	}

	public void SendEvent(string fullEventJson)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		if (_isClientActive())
		{
			_mMsgQueue.PEnqueue((QueueMsg)new JsonMsg
			{
				JsonText = fullEventJson
			});
		}
	}

	public void SendEvent(string upperCaseEventName, int value)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		if (_isClientActive())
		{
			Send_Event send_Event = new Send_Event();
			send_Event.game = GameName;
			send_Event.event_name = upperCaseEventName;
			send_Event.data.value = value;
			QueueMsgSendEvent queueMsgSendEvent = new QueueMsgSendEvent();
			((QueueMsg)queueMsgSendEvent).data = send_Event;
			_mMsgQueue.PEnqueue((QueueMsg)(object)queueMsgSendEvent);
		}
	}

	public void RegisterEvent(string upperCaseEventName, int minValue = 0, int maxValue = 100, EventIconId iconId = 0)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		if (_isClientActive())
		{
			QueueMsgRegisterEvent queueMsgRegisterEvent = new QueueMsgRegisterEvent();
			((QueueMsg)queueMsgRegisterEvent).data = (object)new Register_Event(GameName, upperCaseEventName, minValue, maxValue, iconId);
			_mMsgQueue.PEnqueue((QueueMsg)(object)queueMsgRegisterEvent);
		}
	}
}
