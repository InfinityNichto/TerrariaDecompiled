namespace System.Net.Http;

internal static class GlobalHttpSettings
{
	internal static class DiagnosticsHandler
	{
		public static bool EnableActivityPropagation { get; } = RuntimeSettingParser.QueryRuntimeSettingSwitch("System.Net.Http.EnableActivityPropagation", "DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION", defaultValue: true);

	}

	internal static class SocketsHttpHandler
	{
		public static bool AllowHttp2 { get; } = RuntimeSettingParser.QueryRuntimeSettingSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT", defaultValue: true);


		public static bool AllowHttp3 { get; } = RuntimeSettingParser.QueryRuntimeSettingSwitch("System.Net.SocketsHttpHandler.Http3Support", "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP3SUPPORT", defaultValue: false);


		public static bool DisableDynamicHttp2WindowSizing { get; } = RuntimeSettingParser.QueryRuntimeSettingSwitch("System.Net.SocketsHttpHandler.Http2FlowControl.DisableDynamicWindowSizing", "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2FLOWCONTROL_DISABLEDYNAMICWINDOWSIZING", defaultValue: false);


		public static int MaxHttp2StreamWindowSize { get; } = GetMaxHttp2StreamWindowSize();


		public static double Http2StreamWindowScaleThresholdMultiplier { get; } = GetHttp2StreamWindowScaleThresholdMultiplier();


		private static int GetMaxHttp2StreamWindowSize()
		{
			int num = RuntimeSettingParser.ParseInt32EnvironmentVariableValue("DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_MAXSTREAMWINDOWSIZE", 16777216);
			if (num < 65535)
			{
				num = 65535;
			}
			return num;
		}

		private static double GetHttp2StreamWindowScaleThresholdMultiplier()
		{
			double num = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue("DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_FLOWCONTROL_STREAMWINDOWSCALETHRESHOLDMULTIPLIER", 1.0);
			if (num < 0.0)
			{
				num = 1.0;
			}
			return num;
		}
	}
}
