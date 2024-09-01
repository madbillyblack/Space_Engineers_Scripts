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
using System.Reflection;
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
        const string COMMS_HEADER = "USAP Comms";
        const string DF_LCD_COMTAG = "CH.";
        const string LCD_COMMS_LABEL = "LCD Channel";
        const string SUB_TAG = "SubChannel";
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

        public static int _listenerTimeOut = 20;

        static string _bcID;

        // ICOMMSSCREEN //
        public interface ICommsScreen
        {
            IMyTextSurface TextSurface { get; set; }

            string ChannelTag { get; set; }
        }

        // LCD RECEIVER SCREEN //
        public class LcdRecieverScreen : ICommsScreen
        {
            public IMyTextSurface TextSurface { get; set; }
            public DateTime LastLcdReceipt { get; set; }
            
            //public IMyBroadcastListener Listener { get; set; }

            public string ChannelTag { get; set; }
            public string SubChannelTag { get; set; }

            public string LastMessage { get; set; }

            public bool IsConnected { get; set; }

            public Color ConnectedColor { get; set; }
            public Color DisconnectColor { get; set; }

            public CommsScreenBlock Parent { get; set; }

            public int ReceiverKey { get; set; }
            public int ScreenIndex { get; set; }

            public LcdRecieverScreen(IMyTextSurface textSurface, string channelTag, string subChannelTag, Color connected, Color disconnected, CommsScreenBlock parent, int receiverKey, int screenIndex)
            {
                
                TextSurface = textSurface;
                TextSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                ChannelTag = channelTag;
                SubChannelTag = subChannelTag;
                ConnectedColor = connected;
                DisconnectColor = disconnected;
                TextSurface.FontColor = DisconnectColor;
                IsConnected = false;
                LastLcdReceipt = DateTime.MinValue;
                ScreenIndex = screenIndex;

                LoadLastMessage();
                MessageToScreen(LastMessage, "",true);

                Parent = parent;
                ReceiverKey = receiverKey;
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


            public void SetSubChannel(string subChannelTag)
            {
                SubChannelTag = subChannelTag;
                Parent.IniHandler.SetKey(COMMS_HEADER + " Screen " + ScreenIndex, SUB_TAG, subChannelTag);
            }


            public void MessageToScreen(string message, string displayName,bool firstRun = false)
            {
                string status;
                if (firstRun)
                    status = UNCONNECT_MSG;
                else
                    status = "Connected";

                //LastLcdReceipt = DateTime.Now;
                string id = "";
                if (_receiverScreens.Count > 1)
                    id = "(" + ReceiverKey + ") ";


                LastMessage = message;
                TextSurface.WriteText(id + ChannelTag + " : " + displayName + " - " + status + "\n" + LastMessage);

                if (!IsConnected)
                {
                    IsConnected = true;
                    TextSurface.FontColor = ConnectedColor;
                    TextSurface.ClearImagesFromSelection();
                }
            }

            public void Disconnect(string reason)
            {
                string id = "";
                if (_receiverScreens.Count > 1)
                    id = "(" + ReceiverKey + ") ";

                IsConnected = false;
                TextSurface.FontColor = DisconnectColor;
                TextSurface.ClearImagesFromSelection();
                TextSurface.AddImageToSelection("Danger");
                TextSurface.WriteText(id + ChannelTag + " - " + DISCONNECT_MSG + "\n" + reason + "\n" + LastMessage);
            }


            public void CycleChannel(bool previous = false)
            {
                string screenLabel = "Screen Channel";
                if ((Parent.Block as IMyTextSurfaceProvider).SurfaceCount > 1)
                    screenLabel += " " + ScreenIndex;

                ChannelTag = CycleListener(ChannelTag, previous);
                Parent.IniHandler.SetKey(COMMS_HEADER, screenLabel, LISTEN + ":" + ChannelTag);
            }


            public void CycleSubChannel(bool previous = false)
            {
                if(_channels.Count < 1)
                {
                    _statusMessage += "No Channels Detected!\n";
                    return;
                }

                if (!_channels.Keys.Contains(ChannelTag))
                {
                    CycleChannel(previous);
                }

                Channel channel = _channels[ChannelTag];
                SubChannelTag = channel.CycleSubChannel(SubChannelTag, previous);
            }
        }


        // LCD BROADCASTER SCREEN //
        public class LcdBroadcasterScreen : ICommsScreen
        {
            public IMyTextSurface TextSurface { get; set; }
            public string ChannelTag { get; set; }

            public LcdBroadcasterScreen(IMyTextSurface textSurface, string broadcastTag)
            {
                TextSurface = textSurface;
                TextSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                ChannelTag = broadcastTag;
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
                {
                    if (Block.CustomName.Contains(RECEIVER_TAG))
                        defaultValue = LISTEN;
                    else
                        defaultValue = BROADCAST;

                    defaultValue += ":" + DF_LCD_COMTAG + index;
                }
                else
                {
                    defaultValue = "";
                }

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
                        string subChannel = IniHandler.GetKey(screenHeader, SUB_TAG, "");
                        int receiverKey = ParseInt(IniHandler.GetKey(screenHeader, "Screen " + index + " ID", _receiverScreens.Keys.Count().ToString()), _receiverScreens.Keys.Count());

                        if(_receiverScreens.ContainsKey(receiverKey))
                        {
                            receiverKey = GetUnusedKey(receiverKey, _receiverScreens);
                        }


                        _receiverScreens.Add(receiverKey, new LcdRecieverScreen(surface, channel, subChannel, connectedColor, disconnectColor, this, receiverKey, index));
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

                AssignBroadCastID();
            }

            if(_receiverScreens.Count > 0)
                AssignChannels();
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

                IGC.SendBroadcastMessage(screen.ChannelTag, _bcID + message);
            }
            catch (Exception ex)
            {
                _statusMessage += ex.Message + "\n";
            }
        }


        public void ReceiveMessages()
        {
            if( _receiverScreens.Count < 1 || _channels.Count < 1) return;

            foreach (Channel Channel in _channels.Values)
            {
                while (Channel.Listener.HasPendingMessage)
                {
                    MyIGCMessage rawMessage = Channel.Listener.AcceptMessage();

                    string [] data = rawMessage.Data.ToString().Split(COMMS_SEPARATOR);

                    string key = data[0];
                    string name = data[1];
                    string message = data[2];

                    if (Channel.SubChannels.ContainsKey(key))
                        Channel.SubChannels[key].ReceiveMessage(message);
                    else
                        Channel.SubChannels.Add(key, new SubChannel(key, name, message));
                }
            }
        }


        public void UpdateCurrentReceiver()
        {
            if (_receiverScreens.Count < 1) return;

            LcdRecieverScreen screen = _receiverScreens[GetCurrentRecieverKey()] as LcdRecieverScreen;

            if(!_channels.ContainsKey(screen.ChannelTag))
            {
                screen.Disconnect("Channel Not Found");
                return;
            }

            Channel channel = _channels[screen.ChannelTag];

            if(channel.SubChannels.Count < 1)
            {
                screen.Disconnect("Channel Not Detected");
                return;
            }

            if(screen.SubChannelTag == "" || !channel.SubChannels.ContainsKey(screen.SubChannelTag))
            {
                // If no subchannel specified, set to the first instance in dictionary
                List<string> keys = channel.SubChannels.Keys.ToList();
                screen.SetSubChannel(keys[0]);
            }

            SubChannel subChannel = channel.SubChannels[screen.SubChannelTag];

            if (subChannel.IsTimedOut())
            {
                screen.Disconnect("Signal Timed Out");
                return;
            }
                
            screen.MessageToScreen(subChannel.Message, subChannel.DisplayName);
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


        public LcdRecieverScreen GetRecieverByArg(string receiverId)
        {
            int id;

            if (receiverId == "")
                id = -1;
            else
                id = ParseInt(receiverId, -2);

            return GetReceiver(id);
        }


        public void CycleReceiverChannel(string receiverId, bool previous)
        {
            LcdRecieverScreen receiverScreen = GetRecieverByArg(receiverId);
            if (receiverScreen == null) return;

            receiverScreen.CycleChannel(previous);
        }


        public void CycleReceiverSubChannel(string receiverId, bool previous)
        {
            LcdRecieverScreen receiverScreen = GetRecieverByArg(receiverId);
            if (receiverScreen == null) return;

            receiverScreen.CycleSubChannel(previous);
        }





        static int GetUnusedKey(int currentKey, Dictionary<int, ICommsScreen> commsAdded)
        {
            do
            {
                currentKey++;
            }
            while (commsAdded.ContainsKey(currentKey));

            return currentKey;
        }


        void AssignBroadCastID()
        {
            string displayID =  "";

            List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);

            if (antennas.Count > 0)
            {
                displayID = antennas[0].HudText;
            }

            if (displayID.Trim() == "")
            {
                displayID = Me.CubeGrid.CustomName;
            }

            _bcID = Me.CubeGrid.EntityId.ToString() + COMMS_SEPARATOR + displayID + COMMS_SEPARATOR;
        }
    }
}
