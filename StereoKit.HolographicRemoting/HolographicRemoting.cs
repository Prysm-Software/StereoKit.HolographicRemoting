﻿using StereoKit.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StereoKit.HolographicRemoting
{
	public class HolographicRemoting : IStepper
	{
		static readonly string remotingExtName = "XR_MSFT_holographic_remoting";

		private string _ipAddress;
		private ushort _port;

		private bool _enabled;
		public  bool Enabled => _enabled;

		public HolographicRemoting(string ipAddress, ushort port = 8265)
		{
			_ipAddress = ipAddress;
			_port      = port;

			if (SK.IsInitialized)
				Log.Err("HolographicRemoting must be constructed before StereoKit is initialized!");

			Backend.OpenXR.RequestExt(remotingExtName);
			Backend.OpenXR.OnPreCreateSession += OnPreCreateSession;

			// Set up the OpenXR manifest for the remoting runtime!
			string runtimePath = Path.GetDirectoryName(typeof(HolographicRemoting).Assembly.Location);
			runtimePath = Path.Combine(runtimePath, "RemotingXR.json");
			Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", runtimePath);
		}

		public bool Initialize() => _enabled;
		public void Shutdown  () { }
		public void Step      () { }

		void OnPreCreateSession()
		{
			_enabled = Backend.OpenXR.ExtEnabled(remotingExtName);
			if (!_enabled) return;

			NativeAPI.LoadFunctions();

			Log.Info($"Connecting to Holographic Remoting Player at {_ipAddress}...");

			XrRemotingRemoteContextPropertiesMSFT contextProperties = new XrRemotingRemoteContextPropertiesMSFT();
			contextProperties.type                        = XrStructureType.REMOTING_REMOTE_CONTEXT_PROPERTIES_MSFT;
			contextProperties.enableAudio                 = 1;
			contextProperties.maxBitrateKbps              = 20000;
			contextProperties.videoCodec                  = XrRemotingVideoCodecMSFT.H265;
			contextProperties.depthBufferStreamResolution = XrRemotingDepthBufferStreamResolutionMSFT.HALF;
			if (NativeAPI.xrRemotingSetContextPropertiesMSFT(Backend.OpenXR.Instance, Backend.OpenXR.SystemId, contextProperties) != XrResult.Success)
			{
				Log.Warn("xrRemotingSetContextPropertiesMSFT failed!");
			}

			XrRemotingConnectInfoMSFT connectInfo = new XrRemotingConnectInfoMSFT();
			connectInfo.type             = XrStructureType.REMOTING_CONNECT_INFO_MSFT;
			connectInfo.remoteHostName   = Marshal.StringToHGlobalAnsi(_ipAddress);
			connectInfo.remotePort       = _port;
			connectInfo.secureConnection = 0;
			XrResult result = NativeAPI.xrRemotingConnectMSFT(Backend.OpenXR.Instance, Backend.OpenXR.SystemId, connectInfo);
			if (result != XrResult.Success)
			{
				Log.Warn("xrRemotingConnectMSFT failed! " + result);
			}
			Marshal.FreeHGlobal(connectInfo.remoteHostName);
		}
	}
}
