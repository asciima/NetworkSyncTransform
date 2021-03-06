﻿//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER

/// ----------------------------------------   PUN 2    -----------------------------------------------------
/// ----------------------------------------   PUN 2    -----------------------------------------------------
/// ----------------------------------------   PUN 2    -----------------------------------------------------

#region PUN 2

using UnityEngine;
using emotitron.Compression;
using emotitron.Utilities.GUIUtilities;
using System.Collections.Generic;
using System;
using Photon;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace emotitron.NST
{
	// ver 1
	/// <summary>
	/// The UNET version of this interface for the NSTMaster - unifying code to work with both UNET and Photon.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	public class MasterNetAdapter : MonoBehaviourPunCallbacks, IOnEventCallback // Photon.PunBehaviour //, INSTMasterAdapter
	{
		[HideInInspector]
		public static bool networkStarted;

		public static MasterNetAdapter single;
		public const string ADAPTER_NAME = "PUN2";

		public const NetworkLibrary NET_LIB = NetworkLibrary.PUN2;

		/// <summary>
		/// Attribute for getting the NET_LIB value, without throwing warnings about unreachable code.
		/// </summary>		
		public static NetworkLibrary NetLib { get { return NET_LIB; } }

		public const NetworkModel NET_MODEL = NetworkModel.MasterClient;

		//private NSTMasterSettings nstMasterSettings;

		// TODO this likely needs an actual test
		public static int MasterClientId { get { return PhotonNetwork.MasterClient.ActorNumber; } }

		// Interfaced fields
		public NetworkLibrary NetLibrary { get { return NetworkLibrary.PUN2; } }
		public static NetworkLibrary NetworkLibrary { get { return NetworkLibrary.PUN2; } }

		public static bool Connected { get { return PhotonNetwork.IsConnected; } }
		public static bool ReadyToSend { get { return PhotonNetwork.IsMasterClient || PhotonNetwork.IsConnectedAndReady; } }
		//public static bool ReadyToSend { get { return PhotonNetwork.isMasterClient || PhotonNetwork.isNonMasterClientInRoom; } }
		public static bool ServerIsActive { get { return PhotonNetwork.IsMasterClient; } }
		//public static bool ClientIsActive { get { return PhotonNetwork.isNonMasterClientInRoom; } }
		public static bool ClientIsActive { get { return PhotonNetwork.InRoom; } }
		public static bool NetworkIsActive { get { return PhotonNetwork.IsMasterClient || PhotonNetwork.InRoom; } }
		/// <summary> Cached value for defaultAuthority since this is hotpath </summary>

		public const byte LowestMsgTypeId = 0;
		public const byte HighestMsgTypeId = 199;
		public const byte DefaultMsgTypeId = 190;

		private static bool isServerClient;

		public bool IsRegistered { get { return isRegistered; } set { isRegistered = value; } }

#region Callback Interfaces

		[HideInInspector] public static List<Component> iNetEvents = new List<Component>();
		[HideInInspector] public static List<Component> iOnConnect = new List<Component>();
		[HideInInspector] public static List<Component> iOnStartServer = new List<Component>();
		[HideInInspector] public static List<Component> iOnStartClient = new List<Component>();
		[HideInInspector] public static List<Component> iOnStartLocalPlayer = new List<Component>();
		[HideInInspector] public static List<Component> iOnNetworkDestroy = new List<Component>();
		[HideInInspector] public static List<Component> iOnJoinRoom = new List<Component>();
		[HideInInspector] public static List<Component> iOnJoinRoomFailed = new List<Component>();

		public static void RegisterCallbackInterfaces(Component obj)
		{
			MasterNetCommon.RegisterCallbackInterfaces(obj);
		}

		public static void UnregisterCallbackInterfaces(Component obj)
		{
			MasterNetCommon.UnregisterCallbackInterfaces(obj);
		}

#endregion

		// Statics
		private static short masterMsgTypeId;
		private static bool isRegistered;

#if UNITY_EDITOR

		/// <summary>
		/// Add a NetworkIdentity to the supplied NSTMaster gameobject. Sets localPlayerAuth to false (master isn't a player)
		/// </summary>
		/// <param name="go"></param>
		public static bool AddRequiredEntityComponentToMaster(GameObject go)
		{
			// PUN doesn't need a PhotonView on the master
			return false;
		}

		public static void PurgeLibSpecificComponents()
		{
			NetAdapterTools.PurgeTypeFromEverywhere<PhotonTransformView>();
			NetAdapterTools.PurgeTypeFromEverywhere<PhotonRigidbodyView>();
			NetAdapterTools.PurgeTypeFromEverywhere<PhotonAnimatorView>();
			NetAdapterTools.PurgeTypeFromEverywhere<PhotonView>();
		}

		public static void AddNstEntityComponentsEverywhere()
		{
			NetAdapterTools.AddComponentsWhereverOtherComponentIsFound<NetworkSyncTransform, NSTNetAdapter, PhotonView>();
		}

		public static void AddLibrarySpecificEntityComponent(GameObject go)
		{
			if (!go.GetComponent<PhotonView>())
				go.AddComponent<PhotonView>();
		}


#endif

		static RaiseEventOptions optsOthers;
		static RaiseEventOptions optsSvr;
		static SendOptions sendOpts;

		private void Awake()
		{
			isServerClient = NetLibrarySettings.Single.defaultAuthority == DefaultAuthority.ServerAuthority;

			optsOthers = new RaiseEventOptions();
			//optsOthers.Encrypt = false;
			optsOthers.Receivers = ReceiverGroup.Others;

			optsSvr = new RaiseEventOptions();
			//optsSvr.Encrypt = false;
			optsSvr.Receivers = ReceiverGroup.MasterClient;

			sendOpts = new SendOptions();

		}

		public override void OnEnable()
		{
			base.OnEnable();

			if (isRegistered)
				return;

			isRegistered = true;

			//PhotonNetwork.OnEventCall -= this.OnEventHandler;
			//PhotonNetwork.OnEventCall += this.OnEventHandler;
		}

		public override void OnDisable()
		{
			base.OnDisable();
			//PhotonNetwork.OnEventCall -= this.OnEventHandler;
			isRegistered = false;
		}




		//public override void OnConnected()
		//{
		//	//Debug.Log("OnConnectedToPhoton");
		//}

		public override void OnConnectedToMaster()
		{
			foreach (INetEvents cb in iNetEvents)
				cb.OnConnect(ServerClient.Master);

			foreach (IOnConnect cb in iOnConnect)
				cb.OnConnect(ServerClient.Master);
		}

		public override void OnDisconnected(DisconnectCause cause)
		{
			networkStarted = false;

			if (iNetEvents != null)
				foreach (INetEvents cb in iNetEvents)
					cb.OnNetworkDestroy();

			if (iOnNetworkDestroy != null)
				foreach (IOnNetworkDestroy cb in iOnNetworkDestroy)
					cb.OnNetworkDestroy();
		}

		public override void OnJoinRoomFailed(short returnCode, string message)
		{
			XDebug.LogWarning("Failed to connect. " + message);
		}

		//public override void OnFailedToConnectToPhoton(DisconnectCause cause)
		//{
		//	XDebug.LogWarning("Failed to connect to Photon. " + cause);
		//}
		public override void OnJoinedRoom()
		{
			foreach (IOnJoinRoom cb in iOnJoinRoom)
				cb.OnJoinRoom();
		}

		public override void OnJoinRandomFailed(short returnCode, string message)
		{
			foreach (IOnJoinRoomFailed cb in iOnJoinRoomFailed)
				cb.OnJoinRoomFailed();
		}


		/// <summary>
		/// Capture incoming Photon messages here. If it is the one we are interested in - pass it to NSTMaster
		/// </summary>
		public void OnEvent(EventData photonEvent)
		{
		//	photonEvent.Sender
		//}

		//private void OnEventHandler(byte eventCode, object content, int senderId)
		//{
			if (photonEvent.Code != DefaultMsgTypeId)
				return;

			// ignore messages from self.
			if (ServerIsActive && PhotonNetwork.MasterClient.ActorNumber == photonEvent.Sender)
			{
				XDebug.Log("Master Client talking to self? Normal occurance for a few seconds after Master leaves the game and a new master is selected.");
				return;
			}

			UdpBitStream bitstream = new UdpBitStream(photonEvent.CustomData as byte[]);
			UdpBitStream outstream = new UdpBitStream(NSTMaster.outstreamByteArray);

			bool mirror = PhotonNetwork.IsMasterClient && NetLibrarySettings.single.defaultAuthority == DefaultAuthority.ServerAuthority;

			NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, mirror, photonEvent.Sender);

			if (mirror)// authorityModel == DefaultAuthority.ServerClient)
			{
				byte[] outbytes = new byte[outstream.BytesUsed];
				Array.Copy(outstream.Data, outbytes, outbytes.Length);
				PhotonNetwork.NetworkingClient.OpRaiseEvent(DefaultMsgTypeId, outbytes, optsOthers, sendOpts);
				PhotonNetwork.NetworkingClient.Service();

				
			}
		}

		public static void SendUpdate(ref UdpBitStream bitstream, ref UdpBitStream outstream)
		{
			//TODO replace this GC generating mess with something prealloc
			byte[] streambytes = new byte[bitstream.BytesUsed];
			Array.Copy(bitstream.Data, streambytes, streambytes.Length);
			PhotonNetwork.NetworkingClient.OpRaiseEvent(DefaultMsgTypeId, streambytes, (isServerClient && !PhotonNetwork.IsMasterClient) ? optsSvr : optsOthers, sendOpts);
			PhotonNetwork.NetworkingClient.Service();
			

			// MasterClient send to self - may are may not need this in the future.
			if (PhotonNetwork.IsMasterClient)
				NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, false, PhotonNetwork.MasterClient.ActorNumber);
		}

#region UNET Specific methods

		public static Transform UNET_GetPlayerSpawnPoint() { return null; }
		public static void UNET_RegisterStartPosition(Transform tr) { }
		public static void UNET_UnRegisterStartPosition(Transform tr) { }
		public static GameObject UNET_GetRegisteredPlayerPrefab() { return null; }

#endregion

#region PUN Specific methods

		//public static bool PUN_AutoJoinLobby
		//{
		//	get { return PhotonNetwork.autoJoinLobby; }
		//	set { PhotonNetwork.autoJoinLobby = value; }

		//}
		public static bool PUN_AutomaticallySyncScene
		{
			get { return PhotonNetwork.AutomaticallySyncScene; }
			set { PhotonNetwork.AutomaticallySyncScene = value; }

		}
		public static bool PUN_Connected
		{
			get { return PhotonNetwork.IsConnected; }
		}

		public static void PUN_ConnectUsingSettings(string gameversion = null)
		{
			PhotonNetwork.ConnectUsingSettings();
		}

		public static void PUN_JoinRandomRoom()
		{
			PhotonNetwork.JoinRandomRoom();
		}

		public static void PUN_LoadLevel(string scenename)
		{
			PhotonNetwork.LoadLevel(scenename);
		}

		public static void PUN_CreateRoom(string roomname, byte maxPlayers)
		{
			PhotonNetwork.CreateRoom(roomname, new RoomOptions() { MaxPlayers = maxPlayers }, null);
		}

#endregion


		public static void ServerChangeScene(string sceneName)
		{
			if (PhotonNetwork.IsMasterClient)
				PhotonNetwork.LoadLevel(sceneName);
		}



		public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
		{
			/// Attempt to add a photonview on the fly if missing. Likely to get cranky results.
			PhotonView pv = prefab.GetComponent<PhotonView>();
			if (!pv)
				prefab.AddComponent<PhotonView>();

			GameObject go = PhotonNetwork.Instantiate(prefab.name, position, rotation, 0);
			go.transform.parent = parent;
			return go;
		}

		public static void UnSpawn(GameObject obj)
		{
			if (obj.GetComponent<PhotonView>().IsMine && PhotonNetwork.IsConnected)
			{
				PhotonNetwork.Destroy(obj);
			}
		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(MasterNetAdapter))]
	[CanEditMultipleObjects]
	public class MasterNetAdapterPEditor : NSTHeaderEditorBase
	{
		public override void OnEnable()
		{
			headerColor = HeaderSettingsColor;
			headerName = HeaderMasterName;
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("This is the Adapter for Photon. To work with UNET, switch the Network Library.", MessageType.None);
		}
	}

#endif
}

#endregion

#elif PUN

/// ----------------------------------------   PUN 1    -----------------------------------------------------
/// ----------------------------------------   PUN 1    -----------------------------------------------------
/// ----------------------------------------   PUN 1    -----------------------------------------------------

#region PUN


using UnityEngine;
using emotitron.Compression;
using emotitron.Debugging;
using emotitron.Utilities.GUIUtilities;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace emotitron.NST
{
	// ver 1
	/// <summary>
	/// The UNET version of this interface for the NSTMaster - unifying code to work with both UNET and Photon.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	public class MasterNetAdapter : Photon.PunBehaviour //, INSTMasterAdapter
	{
		[HideInInspector]
		public static bool networkStarted;
		
		public static MasterNetAdapter single;
		public const string ADAPTER_NAME = "PUN";

		public const NetworkLibrary NET_LIB = NetworkLibrary.PUN;
		
		/// <summary>
		/// Attribute for getting the NET_LIB value, without throwing warnings about unreachable code.
		/// </summary>		
		public static NetworkLibrary NetLib { get { return NET_LIB; } }

		public const NetworkModel NET_MODEL = NetworkModel.MasterClient;

		//private NSTMasterSettings nstMasterSettings;

		// TODO this likely needs an actual test
		public static int MasterClientId { get { return PhotonNetwork.masterClient.ID; } }

		// Interfaced fields
		public NetworkLibrary NetLibrary { get { return NetworkLibrary.PUN; } }
		public static NetworkLibrary NetworkLibrary { get { return NetworkLibrary.PUN; } }

		public static bool Connected { get { return PhotonNetwork.connected; } }
		public static bool ReadyToSend { get { return PhotonNetwork.isMasterClient || PhotonNetwork.isNonMasterClientInRoom; } }
		public static bool ServerIsActive { get { return PhotonNetwork.isMasterClient; } }
		public static bool ClientIsActive { get { return PhotonNetwork.isNonMasterClientInRoom; } }
		public static bool NetworkIsActive { get { return PhotonNetwork.isMasterClient || PhotonNetwork.isNonMasterClientInRoom; } }
		/// <summary> Cached value for defaultAuthority since this is hotpath </summary>

		public const byte LowestMsgTypeId = 0;
		public const byte HighestMsgTypeId = 199;
		public const byte DefaultMsgTypeId = 190;

		private static bool isServerClient;

		public bool IsRegistered { get { return isRegistered; } set { isRegistered = value; } }

#region Callback Interfaces

		[HideInInspector] public static List<Component> iNetEvents = new List<Component>();
		[HideInInspector] public static List<Component> iOnConnect = new List<Component>();
		[HideInInspector] public static List<Component> iOnStartServer = new List<Component>();
		[HideInInspector] public static List<Component> iOnStartClient = new List<Component>();
		[HideInInspector] public static List<Component> iOnStartLocalPlayer = new List<Component>();
		[HideInInspector] public static List<Component> iOnNetworkDestroy = new List<Component>();
		[HideInInspector] public static List<Component> iOnJoinRoom = new List<Component>();
		[HideInInspector] public static List<Component> iOnJoinRoomFailed = new List<Component>();

		public static void RegisterCallbackInterfaces(Component obj)
		{
			MasterNetCommon.RegisterCallbackInterfaces(obj);
		}

		public static void UnregisterCallbackInterfaces(Component obj)
		{
			MasterNetCommon.UnregisterCallbackInterfaces(obj);
		}

#endregion

		// Statics
		private static short masterMsgTypeId;
		private static bool isRegistered;

#if UNITY_EDITOR

		/// <summary>
		/// Add a NetworkIdentity to the supplied NSTMaster gameobject. Sets localPlayerAuth to false (master isn't a player)
		/// </summary>
		/// <param name="go"></param>
		public static bool AddRequiredEntityComponentToMaster(GameObject go)
		{
			// PUN doesn't need a PhotonView on the master
			return false;
		}

		public static void PurgeLibSpecificComponents()
		{
			NetAdapterTools.PurgeTypeFromEverywhere<PhotonView>();
		}

		public static void AddNstEntityComponentsEverywhere()
		{
			NetAdapterTools.AddComponentsWhereverOtherComponentIsFound<NetworkSyncTransform, NSTNetAdapter, PhotonView>();
		}

		public static void AddLibrarySpecificEntityComponent(GameObject go)
		{
			if (!go.GetComponent<PhotonView>())
				go.AddComponent<PhotonView>();
		}


#endif

		static RaiseEventOptions optsOthers;
		static RaiseEventOptions optsSvr;
		private void Awake()
		{
			isServerClient = NetLibrarySettings.Single.defaultAuthority == DefaultAuthority.ServerAuthority;

			optsOthers = new RaiseEventOptions();
			optsOthers.Encrypt = false;
			optsOthers.Receivers = ReceiverGroup.Others;

			optsSvr = new RaiseEventOptions();
			optsSvr.Encrypt = false;
			optsSvr.Receivers = ReceiverGroup.MasterClient;
		}

		void OnEnable()
		{
			if (isRegistered)
				return;

			isRegistered = true;

			PhotonNetwork.OnEventCall -= this.OnEventHandler;
			PhotonNetwork.OnEventCall += this.OnEventHandler;
		}

		private void OnDisable()
		{
			PhotonNetwork.OnEventCall -= this.OnEventHandler;
			isRegistered = false;
		}


		public override void OnConnectedToPhoton()
		{
			//Debug.Log("OnConnectedToPhoton");
		}

		public override void OnConnectedToMaster()
		{
			foreach (INetEvents cb in iNetEvents)
				cb.OnConnect(ServerClient.Master);

			foreach (IOnConnect cb in iOnConnect)
				cb.OnConnect(ServerClient.Master);
		}

		public override void OnDisconnectedFromPhoton()
		{
			networkStarted = false;
			
			if (iNetEvents != null)
				foreach (INetEvents cb in iNetEvents)
					cb.OnNetworkDestroy();

			if (iOnNetworkDestroy != null)
				foreach (IOnNetworkDestroy cb in iOnNetworkDestroy)
					cb.OnNetworkDestroy();
		}

		public override void OnConnectionFail(DisconnectCause cause)
		{
			base.OnConnectionFail(cause);
			XDebug.LogWarning("Failed to connect. " + cause);
		}

		public override void OnFailedToConnectToPhoton(DisconnectCause cause)
		{
			base.OnFailedToConnectToPhoton(cause);
			XDebug.LogWarning("Failed to connect to Photon. " + cause);
		}
		public override void OnJoinedRoom()
		{
			foreach (IOnJoinRoom cb in iOnJoinRoom)
				cb.OnJoinRoom();
		}

		public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
		{
			foreach (IOnJoinRoomFailed cb in iOnJoinRoomFailed)
				cb.OnJoinRoomFailed();
		}

		/// <summary>
		/// Capture incoming Photon messages here. If it is the one we are interested in - pass it to NSTMaster
		/// </summary>
		private void OnEventHandler(byte eventCode, object content, int senderId)
		{
			if (eventCode != DefaultMsgTypeId)
				return;

			// ignore messages from self.
			if (ServerIsActive && PhotonNetwork.masterClient.ID == senderId)
			{
				XDebug.Log("Master Client talking to self? Normal occurance for a few seconds after Master leaves the game and a new master is selected.");
				return;
			}

			UdpBitStream bitstream = new UdpBitStream(content as byte[]);
			UdpBitStream outstream = new UdpBitStream(NSTMaster.outstreamByteArray);

			bool mirror = PhotonNetwork.isMasterClient && NetLibrarySettings.single.defaultAuthority == DefaultAuthority.ServerAuthority;

			NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, mirror, senderId);

			if (mirror)// authorityModel == DefaultAuthority.ServerClient)
			{
				byte[] outbytes = new byte[outstream.BytesUsed];
				Array.Copy(outstream.Data, outbytes, outbytes.Length);
				PhotonNetwork.networkingPeer.OpRaiseEvent(DefaultMsgTypeId, outbytes, false, optsOthers);
				PhotonNetwork.networkingPeer.Service();
			}
		}

		public static void SendUpdate(ref UdpBitStream bitstream, ref UdpBitStream outstream)
		{
			//TODO replace this GC generating mess with something prealloc
			byte[] streambytes = new byte[bitstream.BytesUsed];
			Array.Copy(bitstream.Data, streambytes, streambytes.Length);
			PhotonNetwork.networkingPeer.OpRaiseEvent(DefaultMsgTypeId, streambytes, false, (isServerClient && !PhotonNetwork.isMasterClient) ? optsSvr : optsOthers);
			PhotonNetwork.networkingPeer.Service();

			// MasterClient send to self - may are may not need this in the future.
			if (PhotonNetwork.isMasterClient)
				NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, false, PhotonNetwork.masterClient.ID);
		}

#region UNET Specific methods

		public static Transform UNET_GetPlayerSpawnPoint(){	return null;}
		public static void UNET_RegisterStartPosition(Transform tr){}
		public static void UNET_UnRegisterStartPosition(Transform tr){}
		public static GameObject UNET_GetRegisteredPlayerPrefab(){ return null; }

#endregion

#region PUN Specific methods

		public static bool PUN_AutoJoinLobby
		{
			get { return PhotonNetwork.autoJoinLobby; }
			set { PhotonNetwork.autoJoinLobby = value; }

		}
		public static bool PUN_AutomaticallySyncScene
		{
			get { return PhotonNetwork.automaticallySyncScene; }
			set { PhotonNetwork.automaticallySyncScene = value; }

		}
		public static bool PUN_Connected
		{
			get { return PhotonNetwork.connected; }
		}

		public static void PUN_ConnectUsingSettings(string gameversion)
		{
			PhotonNetwork.ConnectUsingSettings(gameversion);
		}

		public static void PUN_JoinRandomRoom()
		{
			PhotonNetwork.JoinRandomRoom();
		}

		public static void PUN_LoadLevel(string scenename)
		{
			PhotonNetwork.LoadLevel(scenename);
		}

		public static void PUN_CreateRoom(string roomname, byte maxPlayers)
		{
			PhotonNetwork.CreateRoom(roomname, new RoomOptions() { MaxPlayers = maxPlayers }, null);
		}

#endregion

	
		public static void ServerChangeScene(string sceneName)
		{
			if (PhotonNetwork.isMasterClient)
				PhotonNetwork.LoadLevel(sceneName);
		}


		public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
		{
			/// Attempt to add a photonview on the fly if missing. Likely to get cranky results.
			PhotonView pv = prefab.GetComponent<PhotonView>();
			if (!pv)
				prefab.AddComponent<PhotonView>();

			GameObject go = PhotonNetwork.Instantiate(prefab.name, position, rotation, 0);
			go.transform.parent = parent;
			return go;
		}

		public static void UnSpawn(GameObject obj)
		{
			if (obj.GetComponent<PhotonView>().isMine && PhotonNetwork.connected)
			{
				PhotonNetwork.Destroy(obj);
			}
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(MasterNetAdapter))]
	[CanEditMultipleObjects]
	public class MasterNetAdapterPEditor : NSTHeaderEditorBase
	{
		public override void OnEnable()
		{
			headerColor = HeaderSettingsColor;
			headerName = HeaderMasterName;
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("This is the Adapter for Photon. To work with UNET, switch the Network Library.", MessageType.None);
		}
	}

#endif
}

#endregion // END PUN

#else

/// ------------------------------------   UNET / MIRROR    -------------------------------------------------
/// ------------------------------------   UNET / MIRROR    -------------------------------------------------
/// ------------------------------------   UNET / MIRROR    -------------------------------------------------

#region UNET / MIRROR



using UnityEngine;
using emotitron.Compression;
using System.Collections.Generic;

#if MIRROR
using Mirror;
#else
using UnityEngine.Networking;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable CS0618 // UNET obsolete

namespace emotitron.NST
{
	/// <summary>
	///  Nonalloc message for Mirror, since we can't directly send writers with Mirror.
	/// </summary>
	public class BytesMessageNonalloc : MessageBase
	{
		public byte[] buffer;
		public ushort length;

		public BytesMessageNonalloc()
		{

		}
		public BytesMessageNonalloc(byte[] nonalloc)
		{
			this.buffer = nonalloc;
		}

		public BytesMessageNonalloc(byte[] nonalloc, ushort length)
		{
			this.buffer = nonalloc;
			this.length = length;
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(length);
			for (int i = 0; i < length; i++)
				writer.Write(buffer[i]);
		}

		public override void Deserialize(NetworkReader reader)
		{
			length = reader.ReadUInt16();
			for (int i = 0; i < length; i++)
				buffer[i] = reader.ReadByte();
		}

	}

	/// <summary>
	/// The UNET version of this interface for the NSTMaster - unifying code to work with both UNET and Photon.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	//[RequireComponent(typeof(NetworkIdentity))]
	//[NetworkSettings(sendInterval = .000001f)]

	public class MasterNetAdapter : MonoBehaviour //, INSTMasterAdapter
	{
		[HideInInspector]
		public static bool networkStarted;

		public static MasterNetAdapter single;
		private static NetworkClient cachedNetworkClient;

#if MIRROR
		public const string ADAPTER_NAME = "MIRROR";
#else
		public const string ADAPTER_NAME = "UNET";
#endif

		public const NetworkLibrary NET_LIB = NetworkLibrary.UNET;

		/// <summary>
		/// Attribute for getting the NET_LIB value, without throwing warnings about unreachable code.
		/// </summary>		
		public static NetworkLibrary NetLib { get { return NET_LIB; } }

		public const NetworkModel NET_MODEL = NetworkModel.ServerClient;

		// TODO this likely needs an actual test
		public static int MasterClientId = 0;

		// Interfaced fields
		public NetworkLibrary NetLibrary { get { return NET_LIB; } }
		public static NetworkLibrary NetworkLibrary { get { return NET_LIB; } }

		// TODO: Attempt to cache this (if UNET makes that possible of course)
		//public static bool Connected { get { return NetworkServer.active || (NetworkManager.singleton.client != null && NetworkManager.singleton.client.isConnected); } }
		public static bool Connected { get { return NetworkServer.active || NetworkClient.active; } }
#if MIRROR_3_0_OR_NEWER
		public static bool ReadyToSend { get { return NetworkServer.active || (NetworkManager.singleton && NetworkManager.singleton.client != null && NetworkClient.isConnected); } }
#else
		public static bool ReadyToSend { get { return NetworkServer.active || (NetworkManager.singleton && NetworkManager.singleton.client != null && NetworkManager.singleton.client.isConnected); } }
#endif

		public static bool ServerIsActive { get { return NetworkServer.active; } }
		public static bool ClientIsActive { get { return NetworkClient.active; } }
		public static bool NetworkIsActive { get { return NetworkClient.active || NetworkServer.active; } }

		public const short LowestMsgTypeId = (short)MsgType.Highest;
		public const short HighestMsgTypeId = short.MaxValue;
		public const short DefaultMsgTypeId = 190;

		//public bool IsRegistered { get { return isRegistered; } set { isRegistered = value; } }

		#region Callback Interfaces

		[HideInInspector] public static List<Component> iNetEvents = new List<Component>();
		[HideInInspector] public static List<Component> iOnConnect = new List<Component>();
		[HideInInspector] public static List<Component> iOnStartLocalPlayer = new List<Component>();
		[HideInInspector] public static List<Component> iOnNetworkDestroy = new List<Component>();
		[HideInInspector] public static List<Component> iOnJoinRoom = new List<Component>();
		[HideInInspector] public static List<Component> iOnJoinRoomFailed = new List<Component>();

		public static void RegisterCallbackInterfaces(Component obj)
		{
			MasterNetCommon.RegisterCallbackInterfaces(obj);
		}

		public static void UnregisterCallbackInterfaces(Component obj)
		{
			MasterNetCommon.UnregisterCallbackInterfaces(obj);
		}

		#endregion

		// Statics
		//private static NetworkWriter writer = new NetworkWriter();
		private static short masterMsgTypeId;
		//private static bool isRegistered;

		private void Awake()
		{
			if (!EnforceSingleton())
			{
				return;
			}
		}

		// Run RegisterHandlers again in Start in case the adapter was added late and OnStartServer and OnStartClient never ran.
		private void OnEnable()
		{
			if (isInvalidSingleton)
				return;

			networkStarted = NetworkServer.active || NetworkClient.active;
			RegisterHanders();
		}

		private bool isInvalidSingleton;
		/// <summary>
		/// Returns true if this is the singleton, false if we had to destroy it.
		/// </summary>
		private bool EnforceSingleton()
		{

			if (single && single != this)
			{
				isInvalidSingleton = true;
				Destroy(this);
				return false;
			}

			isInvalidSingleton = false;
			single = this;
			return true;
		}

		private bool ServerRegistered;
		private bool ClientRegistered;

		/// <summary>
		/// Constantly check for changes in network status (UNET callbacks are pretty terrible)
		/// </summary>
		private void Update()
		{
			if (!ServerRegistered && NetworkServer.active)
			{
				OnStartServer();
				ServerRegistered = true;
			}
			else if (ServerRegistered && !NetworkServer.active)
			{
				OnNetworkDestroy();
			}

			if (!ClientRegistered && NetworkClient.active)
			{
				OnStartClient();
				ClientRegistered = true;
			}
			else if (ClientRegistered && !NetworkClient.active)
			{
				OnNetworkDestroy();
			}
		}

		public void OnStartServer()
		{
			if (isInvalidSingleton)
				return;

			gameObject.SetActive(true);

			RegisterHanders();

			networkStarted = true;

			foreach (INetEvents cb in iNetEvents)
				cb.OnConnect(ServerClient.Server);

			foreach (IOnConnect cb in iOnConnect)
				cb.OnConnect(ServerClient.Server);
		}
		public void OnStartClient()
		{
			if (isInvalidSingleton)
				return;

			cachedNetworkClient = NetworkManager.singleton.client;

			gameObject.SetActive(true);

			RegisterHanders();

			networkStarted = true;

			foreach (INetEvents cb in iNetEvents)
				cb.OnConnect(ServerClient.Client);

			foreach (IOnConnect cb in iOnConnect)
				cb.OnConnect(ServerClient.Client);
		}

		private void Start()
		{

#if MIRROR_1726_OR_NEWER && UNITY_EDITOR

			if (Transport.activeTransport is TelepathyTransport)
				Debug.LogWarning("<b><color=red>Network Sync Transform is designed for Unreliable UDP transports.</color></b> The current transport is Telepathy(TCP-Reliable) and will perform badly under real internet conditions at high tick rates. Consider using Ignorance or LiteNetLib for Mirror transports.");

#elif MIRROR && UNITY_EDITOR

			if (NetworkManager.singleton.transport is TelepathyTransport)
				Debug.LogWarning("<b><color=red>Network Sync Transform is designed for Unreliable UDP transports.</color></b> The current transport is Telepathy(TCP-Reliable) and will perform badly under real internet conditions.");
#endif
		}

		public void OnDestroy()
		{
			OnNetworkDestroy();
		}

		public void OnDisable()
		{
			OnNetworkDestroy();
		}

		public void OnNetworkDestroy()
		{
			if (isInvalidSingleton)
				return;

			networkStarted = false;

			if (iNetEvents != null)
				foreach (INetEvents cb in iNetEvents)
					cb.OnNetworkDestroy();

			if (iOnNetworkDestroy != null)
				foreach (IOnNetworkDestroy cb in iOnNetworkDestroy)
					cb.OnNetworkDestroy();

			//isRegistered = false;
			if (NetworkServer.handlers.ContainsKey(masterMsgTypeId) && NetworkServer.handlers[masterMsgTypeId] == ReceiveUpdate)
				NetworkServer.UnregisterHandler(HeaderSettings.Single.masterMsgTypeId);
#if MIRROR_3_0_OR_NEWER
			NetworkClient.UnregisterHandler(masterMsgTypeId);
#else
			if (NetworkManager.singleton && NetworkManager.singleton.client != null)
				NetworkManager.singleton.client.UnregisterHandler(masterMsgTypeId);
#endif


			ServerRegistered = false;
			ClientRegistered = false;


		}

		private void RegisterHanders()
		{
			if (isInvalidSingleton)
				return;

			//if (IsRegistered)
			//	return;

			masterMsgTypeId = HeaderSettings.Single.masterMsgTypeId;

			if (NetworkServer.active)
			{
				///// Unregister just in case of edge cases where Unregister never gets called
				//NetworkServer.UnregisterHandler(masterMsgTypeId);
				if (!NetworkServer.handlers.ContainsKey(masterMsgTypeId))
					NetworkServer.RegisterHandler(masterMsgTypeId, ReceiveUpdate);

				/// Mirror (at least Telepathy) needs a dummy handler for Host talking to itself
#if MIRROR_3_0_OR_NEWER
				if (NetworkClient.active)
					if (!NetworkClient.handlers.ContainsKey(masterMsgTypeId))
						NetworkClient.RegisterHandler(masterMsgTypeId, ReceiveDummy);
#elif MIRROR
				if (NetworkClient.active)
					if (!NetworkManager.singleton.client.handlers.ContainsKey(masterMsgTypeId))
						NetworkManager.singleton.client.RegisterHandler(masterMsgTypeId, ReceiveDummy);
#endif
				//isRegistered = true;
			}

			else if (NetworkClient.active)
			{
				///// Unregister just in case of edge cases where Unregister never gets called
				//NetworkManager.singleton.client.UnregisterHandler(masterMsgTypeId);
#if MIRROR_3_0_OR_NEWER
				if (!NetworkClient.handlers.ContainsKey(masterMsgTypeId))
					NetworkClient.RegisterHandler(masterMsgTypeId, ReceiveUpdate);
#else
				if (!NetworkManager.singleton.client.handlers.ContainsKey(masterMsgTypeId))
					NetworkManager.singleton.client.RegisterHandler(masterMsgTypeId, ReceiveUpdate);

#endif
				//isRegistered = true;
			}
		}

		/// Reuse the MessageBase
		private static readonly BytesMessageNonalloc bytesmsg = new BytesMessageNonalloc() { buffer = NSTMaster.bitstreamByteArray };
		private static readonly BytesMessageNonalloc outbytemsg = new BytesMessageNonalloc() { buffer = NSTMaster.outstreamByteArray };

#if MIRROR
		// Mirror SendToAll seems to send from Host server to its own client, and will flood the log with errors if no handler is set up.
		public static void ReceiveDummy(NetworkMessage msg)
		{
		}

#endif

		/// <summary>
		///  Updates over the network arrive here - AFTER the Update() runs (not tested for all platforms... thanks unet for the great docs.) 
		///  The incoming bitstream is read
		/// </summary>
		/// <param name="msg"></param>
		private static void ReceiveUpdate(NetworkMessage msg)
		{
			bytesmsg.Deserialize(msg.reader);

			UdpBitStream bitstream = new UdpBitStream(bytesmsg.buffer, bytesmsg.length);
			UdpBitStream outstream = new UdpBitStream(NSTMaster.outstreamByteArray);

			NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, NetworkServer.active, msg.conn.connectionId);

			outbytemsg.length = (ushort)outstream.BytesUsed;

			// Write a clone message and pass it to all the clients if this is the server receiving
			if (NetworkServer.active)
			{
				NetworkServer.SendToAll(msg.msgType, outbytemsg);
			}
		}

		public static void SendUpdate(ref UdpBitStream bitstream, ref UdpBitStream outstream)
		{
			bytesmsg.length = (ushort)bitstream.BytesUsed;

			// if this is the server - send to all.
			if (NetworkServer.active)
			{

				//writer.SendPayloadArrayToAllClients(masterMsgTypeId, Channels.DefaultUnreliable);
				NetworkServer.SendToAll(masterMsgTypeId, bytesmsg);

				// If this is the server as client, run the ReceiveUpdate since local won't get this run.
				//if (NetworkClient.active)
				NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, false, 0);

			}
			// if this is a client send to server.
			else
			{
#if MIRROR_3_0_OR_NEWER
				if (cachedNetworkClient != null && NetworkClient.isConnected)
				{
					NetworkClient.Send(masterMsgTypeId, bytesmsg);
				}
#else
				// TODO: find reliable way to cache this condition - Is here to eliminate some shut down warnings, and not critical
				if (cachedNetworkClient != null && cachedNetworkClient.isConnected)
				{
					NetworkManager.singleton.client.Send(masterMsgTypeId, bytesmsg);
				}
#endif

			}
		}

		///// <summary>
		/////  Updates over the network arrive here - AFTER the Update() runs (not tested for all platforms... thanks unet for the great docs.) 
		/////  The incoming bitstream is read
		///// </summary>
		///// <param name="msg"></param>
		//private static void ReceiveUpdate(NetworkMessage msg)
		//{

		//	UdpBitStream bitstream = new UdpBitStream(msg.reader.ReadBytesNonAlloc(NSTMaster.bitstreamByteArray, msg.reader.Length), msg.reader.Length);
		//	UdpBitStream outstream = new UdpBitStream(NSTMaster.outstreamByteArray);

		//	NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, NetworkServer.active, msg.conn.connectionId);

		//	// Write a clone message and pass it to all the clients if this is the server receiving
		//	if (NetworkServer.active) // && msg.conn == nst.NI.clientAuthorityOwner)
		//	{
		//		writer.StartMessage(msg.msgType);
		//		writer.WriteUncountedByteArray(outstream.Data, outstream.BytesUsed);
		//		writer.SendPayloadArrayToAllClients(msg.msgType);
		//		if (NetworkServer.connections[0] != null)
		//			NetworkServer.connections[0].FlushChannels();
		//	}
		//}

		//public static void SendUpdate(ref UdpBitStream bitstream, ref UdpBitStream outstream)
		//{
		//	// Send the bitstream to the UNET writer
		//	writer.StartMessage(masterMsgTypeId);
		//	writer.WriteUncountedByteArray(NSTMaster.bitstreamByteArray, bitstream.BytesUsed);
		//	writer.FinishMessage();

		//	// if this is the server - send to all.
		//	if (NetworkServer.active)
		//	{
		//		writer.SendPayloadArrayToAllClients(masterMsgTypeId, Channels.DefaultUnreliable);
		//		//NetworkServer.connections[0].FlushChannels();

		//		// If this is the server as client, run the ReceiveUpdate since local won't get this run.
		//		//if (NetworkClient.active)
		//		NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, false, 0);

		//	}
		//	// if this is a client send to server.
		//	else
		//	{
		//		// TODO: find reliable way to cache this condition - Is here to eliminate some shut down warnings, and not critical
		//		if (cachedNetworkClient != null && cachedNetworkClient.isConnected)
		//			NetworkManager.singleton.client.SendWriter(writer, Channels.DefaultUnreliable);
		//		//NetworkManager.singleton.client.connection.FlushChannels();
		//	}
		//}

		#region UNET Specific methods

		public static Transform UNET_GetPlayerSpawnPoint()
		{
			return NetworkManager.singleton.GetStartPosition();
		}

		public static void UNET_RegisterStartPosition(Transform tr)
		{
			NetworkManager.RegisterStartPosition(tr);
		}

		public static void UNET_UnRegisterStartPosition(Transform tr)
		{
			NetworkManager.UnRegisterStartPosition(tr);
		}

		public static GameObject UNET_GetRegisteredPlayerPrefab()
		{
			if (NetworkManager.singleton == null)
				NetworkManager.singleton = FindObjectOfType<NetworkManager>();

			if (NetworkManager.singleton != null)
			{
				return NetworkManager.singleton.playerPrefab;
			}
			return null;
		}

		#endregion

		#region PUN Specific relays

		public static bool PUN_AutoJoinLobby { get { return false; } set { } }
		public static bool PUN_AutomaticallySyncScene { get { return false; } set { } }
		public static bool PUN_Connected { get { return false; } }
		public static void PUN_ConnectUsingSettings(string gameversion) { }
		public static void PUN_JoinRandomRoom() { }
		public static void PUN_LoadLevel(string scenename) { }
		public static void PUN_CreateRoom(string roomname, int maxPlayer) { }

		#endregion


		public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
		{
			GameObject go = Instantiate(prefab, position, rotation, parent);
			NetworkServer.Spawn(go);
			return go;
		}

		public static void UnSpawn(GameObject go)
		{
			if (NetworkServer.active)
				NetworkServer.UnSpawn(go);
		}


		public static void ServerChangeScene(string sceneName)
		{
			if (NetworkServer.active)
				NetworkManager.singleton.ServerChangeScene(sceneName);
		}

#if UNITY_EDITOR
		/// <summary>
		/// Add a NetworkIdentity to the supplied NSTMaster gameobject. Sets localPlayerAuth to false (master isn't a player)
		/// </summary>
		/// <param name="go"></param>
		public static bool AddRequiredEntityComponentToMaster(GameObject go)
		{
			//if (!go.GetComponent<NetworkIdentity>())
			//{
			//	NetworkIdentity ni = EditorUtils.EnsureRootComponentExists<NetworkIdentity>(go);
			//	ni.localPlayerAuthority = false;
			//	return true;
			//}
			//return false;
			return true;
		}

		public static void PurgeLibSpecificComponents()
		{
			NetAdapterTools.PurgeTypeFromEverywhere<NetworkIdentity>();
			NetAdapterTools.PurgeTypeFromEverywhere<NetworkManager>(true);
		}

		public static void AddNstEntityComponentsEverywhere()
		{
			NetAdapterTools.AddComponentsWhereverOtherComponentIsFound<NetworkSyncTransform, NSTNetAdapter, NetworkIdentity>();
		}

		public static void AddLibrarySpecificEntityComponent(GameObject go)
		{
			//if (!go.GetComponent<NetworkIdentity>())
			//	go.AddComponent<NetworkIdentity>().assetId.IsValid();
		}



#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(MasterNetAdapter))]
	[CanEditMultipleObjects]
	public class MasterNetAdapterEditor : NSTHeaderEditorBase
	{
		//NetworkIdentity ni;

		public override void OnEnable()
		{
			headerColor = HeaderSettingsColor;
			headerName = HeaderMasterName;
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			MasterNetAdapter _target = (MasterNetAdapter)target;

			NetAdapterTools.EnsureSceneNetLibDependencies();

			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("This is the " + MasterNetAdapter.ADAPTER_NAME + " adapter. To work with Photon PUN, switch the Network Library.", MessageType.None);
			NetLibrarySettings.Single.DrawGui(target, true, false, true);
		}
	}

#endif
}

#pragma warning restore CS0618 // UNET obsolete

#endregion // UNET / MIRROR

#endif  // END UNET / MIRROR