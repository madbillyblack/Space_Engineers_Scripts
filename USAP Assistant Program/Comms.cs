using Sandbox.Game.Debugging;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        const string COMMS_HEADER = "USAP: Comms";
        const string DF_LCD_COMTAG = "LFS Open Channel";
        const string LCD_COMMS_LABEL = "LCD Channel";
        const string BROADCAST = "BROADCAST";
        const string LISTEN = "LISTEN";
        const string CONNECT_LABEL = "Connected Color";
        const string DISCONNECT_LABEL = "Disconnected Color";
        const string DISCONNECT_MSG = "||| DISCONNECTED |||";
        const string UNCONNECT_MSG = "Loading...";

        const string DF_CONNECT_COLOR = "0,127,0";
        const string DF_DISCONNECT_COLOR = "24,24,24";

        //IMyBroadcastListener _listener;
        bool _commsEnabled;

        public static Dictionary<int, ICommsScreen> _receiverScreens;
        public static Dictionary<string, ICommsScreen> _broadcasterScreens;

        public static List<string> _broadcasterKeys;
        public static List<int> _receiverKeys;

        public int _currentBcScreen = 0;
        public int _currentRcScreen = 0;
        public int _broadcastTick = -1; // Tick on which messages will be broadcast

        // ICOMMSSCREEN //
        public interface ICommsScreen
        {
            IMyTextSurface TextSurface { get; set; }

            string BroadcastTag { get; set; }
        }

        // LCD RECEIVER SCREEN //
        public class LcdRecieverScreen : ICommsScreen
        {
            public IMyTextSurface TextSurface { get; set; }
            public DateTime LastLcdReceipt { get; set; }
            
            //public IMyBroadcastListener Listener { get; set; }

            public string BroadcastTag { get; set; }

            public string LastMessage { get; set; }

            public bool IsConnected { get; set; }

            public Color ConnectedColor { get; set; }
            public Color DisconnectColor { get; set; }

            CommsScreenBlock Parent { get; set; }

            int ReceiverKey { get; set; }
            int ScreenIndex { get; set; }

            public LcdRecieverScreen(IMyTextSurface textSurface, string broadcastTag, Color connected, Color disconnected, CommsScreenBlock parent, int receiverKey, int screenIndex)
            {
                
                TextSurface = textSurface;
                TextSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                BroadcastTag = broadcastTag;
                ConnectedColor = connected;
                DisconnectColor = disconnected;
                TextSurface.FontColor = DisconnectColor;
                IsConnected = false;
                LastLcdReceipt = DateTime.MinValue;
                ScreenIndex = screenIndex;

                LoadLastMessage();
                MessageToScreen(LastMessage, true);

                Parent = parent;
                ReceiverKey = receiverKey;
            }

            public bool IsTimedOut()
            {
                return DateTime.Now - LastLcdReceipt > TimeSpan.FromSeconds(BROADCAST_TIMEOUT);
            }

            private void LoadLastMessage()
            {

                StringBuilder stringBuilder = new StringBuilder();
                TextSurface.ReadText(stringBuilder);
                string oldMessage = stringBuilder.ToString();
                string msg = "";


                string[] lines = oldMessage.Split('\n');

                if (lines.Length > 1)
                {
                    for(int i = 1; i <lines.Length; i++)
                    {
                        msg += lines[i] + "\n";
                    }
                }

                LastMessage = msg;
            }


            public void MessageToScreen(string message, bool firstRun = false)
            {
                string status;
                if (firstRun)
                    status = UNCONNECT_MSG;
                else
                    status = "Connected";

                //LastLcdReceipt = DateTime.Now;
                LastMessage = message;
                TextSurface.WriteText(BroadcastTag + " - " + status + "\n" + LastMessage);

                if (!IsConnected)
                {
                    IsConnected = true;
                    TextSurface.FontColor = ConnectedColor;
                    TextSurface.ClearImagesFromSelection();
                }
            }

            public void Disconnect(string reason)
            {
                IsConnected = false;
                TextSurface.FontColor = DisconnectColor;
                TextSurface.AddImageToSelection("Danger");
                TextSurface.WriteText(BroadcastTag + " - " + DISCONNECT_MSG + "\n" + reason + "\n" + LastMessage);
            }


            public void CycleChannel(bool previous = false)
            {
                BroadcastTag = CycleListener(BroadcastTag, previous);
                Parent.IniHandler.SetKey(COMMS_HEADER, "Screen Channel " + ScreenIndex, LISTEN + ":" + BroadcastTag);
            }
        }


        // LCD BROADCASTER SCREEN //
        public class LcdBroadcasterScreen : ICommsScreen
        {
            public IMyTextSurface TextSurface { get; set; }
            public string BroadcastTag { get; set; }

            public LcdBroadcasterScreen(IMyTextSurface textSurface, string broadcastTag)
            {
                TextSurface = textSurface;
                TextSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                BroadcastTag = broadcastTag;
            }
        }


        public class CommsScreenBlock
        {
            public IMyTerminalBlock Block { get; set; }
            public MyIniHandler IniHandler { get; set; }
            //public Dictionary<int, ICommsScreen> Screens { get; set; } // Could be used later for automation of config
            public int SurfaceCount { get; set; }

            public CommsScreenBlock(IMyTerminalBlock block, MyIniHandler iniHandler)
            {
                Block = block;
                SurfaceCount = (Block as IMyTextSurfaceProvider).SurfaceCount;
                IniHandler = iniHandler;
                //AssignScreens();
            }

            public void AssignScreens()
            {
                if(SurfaceCount == 1)
                    AssignSingleScreen();
                else if(SurfaceCount > 1)
                    AssignMultiScreen();
            }

            private void AssignSingleScreen(int index = 0, bool multiscreen = false, bool useDefaultValue = true)
            {
                string screenId;
                if (multiscreen)
                    screenId = " " + index.ToString();
                else
                    screenId = "";

                string defaultValue;
                if (useDefaultValue)
                    defaultValue = BROADCAST + ":" + DF_LCD_COMTAG + screenId;
                else
                    defaultValue = "";


                IMyTextSurface surface = (Block as IMyTextSurfaceProvider).GetSurface(index);

                string[] channelData = IniHandler.GetKey(COMMS_HEADER, "Screen Channel" + screenId, defaultValue).Split(':');

                if (channelData.Length != 2)
                    return;

                string cmd = channelData[0].ToUpper();
                string channel = channelData[1];

                try
                {


                    if (cmd == BROADCAST)
                    {
                        _broadcasterScreens.Add(channel, new LcdBroadcasterScreen(surface, channel));
                    }
                    else if (cmd == LISTEN)
                    {
                        string screenHeader = COMMS_HEADER + " Screen " + index;
                        Color connectedColor = ParseColor(IniHandler.GetKey(screenHeader, CONNECT_LABEL, DF_CONNECT_COLOR));
                        Color disconnectColor = ParseColor(IniHandler.GetKey(screenHeader, DISCONNECT_LABEL, DF_DISCONNECT_COLOR));
                        //string channelList = IniHandler.GetKey(COMMS_HEADER, "Channels", channel);
                        int receiverKey = ParseInt(IniHandler.GetKey(screenHeader, "Screen " + index + " ID", _receiverScreens.Count().ToString()), _receiverScreens.Count());

                        _receiverScreens.Add(receiverKey, new LcdRecieverScreen(surface, channel, connectedColor, disconnectColor, this, receiverKey, index));
                    }                        
                }
                catch (Exception ex)
                {
                    _statusMessage += "Error Adding "+ Block.CustomName +"\n At: " + channel + "\n" + ex.Message + "\n";
                }
            }

            private void AssignMultiScreen()
            {
                bool isFirstScreen = true;

                for(int i = 0; i < SurfaceCount; i++)
                {
                    string screenId = " " + i.ToString();
                    AssignSingleScreen(i, true, isFirstScreen);

                    // Set to false, so default value isn't written to later screens
                    isFirstScreen = false;
                }
            }
        }


        public void AssignComms()
        {
            AssignChannelListeners();

            _receiverScreens = new Dictionary<int, ICommsScreen>();
            _broadcasterScreens = new Dictionary<string, ICommsScreen>();

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(COMMS_SCREEN_TAG, blocks);

            if(blocks.Count > 0)
            {
                foreach(IMyTerminalBlock block in blocks)
                {
                    MyIniHandler ini = new MyIniHandler(block);
                    if(ini.HasSameGridId())
                    {
                        CommsScreenBlock screenBlock = new CommsScreenBlock(block, ini);
                        screenBlock.AssignScreens();
                    }
                }
            }

            _receiverKeys = _receiverScreens.Keys.ToList<int>();
            _commsEnabled = _broadcasterScreens.Count() + _receiverScreens.Count() > 0;

            if (_broadcasterScreens.Count > 0)
            {
                _broadcastTick = Math.Abs((int)Me.CubeGrid.EntityId) % _breather.Length;
                _broadcasterKeys = _broadcasterScreens.Keys.ToList();
            }
        }

        public void ExecuteComms(UpdateType updateSource)
        {
            if ((updateSource & UpdateType.IGC) > 0)
            {
                ReceiveMessages();
            }
            else if(_broadcasterScreens.Count > 0)
            {
                BroadcastCurrentScreen();
            }

            UpdateCurrentReceiver();
        }


        public void ShowBroadcastData()
        {
            if(!_commsEnabled) return;

            Echo("Broadcast Screen: " + _broadcasterScreens.Count());
            Echo("Receiver Screens: " + _receiverScreens.Count());
            Echo("Broadcast Tick:" + _broadcastTick);

            if (_breath == _broadcastTick)
                Echo("BROADCASTING");
        }

        public int GetCurrentBroadCasterIndex()
        {
            _currentBcScreen++;
            if(_currentBcScreen >= _broadcasterScreens.Count)
                _currentBcScreen = 0;

            return _currentBcScreen;
        }


        public int GetCurrentRecieverKey()
        {
            _currentRcScreen++;
            if(_currentRcScreen >= _receiverKeys.Count)
                _currentRcScreen = 0;

            return _receiverKeys[_currentRcScreen];
        }


        public void BroadcastCurrentScreen()
        {
            try
            {
                if (_breath != _broadcastTick)
                    return;

                ICommsScreen screen = _broadcasterScreens[_broadcasterKeys[GetCurrentBroadCasterIndex()]];

                StringBuilder stringBuilder = new StringBuilder();
                screen.TextSurface.ReadText(stringBuilder);
                string message = stringBuilder.ToString();

                IGC.SendBroadcastMessage(screen.BroadcastTag, message);
            }
            catch (Exception ex)
            {
                _statusMessage += ex.Message + "\n";
            }
        }


        public void ReceiveMessages()
        {
            if( _receiverScreens.Count < 1 || _listeners.Count < 1) return;

            foreach (ChannelListener channelListener in _listeners.Values)
            {
                while (channelListener.Listener.HasPendingMessage)
                {
                    MyIGCMessage message = channelListener.Listener.AcceptMessage();
                    channelListener.Message = message.Data.ToString();
                    channelListener.LastReceived = DateTime.Now;
                }
            }
        }


        public void UpdateCurrentReceiver()
        {
            if (_receiverScreens.Count < 1) return;

            LcdRecieverScreen screen = _receiverScreens[GetCurrentRecieverKey()] as LcdRecieverScreen;

            if(!_listeners.ContainsKey(screen.BroadcastTag))
            {
                screen.Disconnect("Channel Not Found");
                return;
            }

            ChannelListener channelListener = _listeners[screen.BroadcastTag];

            if (channelListener.IsTimedOut())
            {
                screen.Disconnect("Signal Timed Out");
                return;
            }
                
            screen.MessageToScreen(channelListener.Message);
        }

        public LcdRecieverScreen GetReceiver(int receiverId)
        {
            if(_receiverScreens.Count < 1) {
                _statusMessage += "No Recievers Detected!\n";
                return null;
            }

            if (receiverId == -1)
                return _receiverScreens[_receiverScreens.Keys.Min()] as LcdRecieverScreen;

            if (_receiverScreens.Keys.Contains(receiverId))
                return _receiverScreens[receiverId] as LcdRecieverScreen;

            _statusMessage += "Receiver " + receiverId + " Not Found!\n";

            return null;
        }


        public void CycleReceiverChannel(string receiverId, bool previous)
        {
            int id;

            if (receiverId == "")
                id = -1;
            else
                id = ParseInt(receiverId, -2);

            LcdRecieverScreen receiverScreen = GetReceiver(id);

            if (receiverScreen == null) return;

            receiverScreen.CycleChannel(previous);
        }
    }
}
