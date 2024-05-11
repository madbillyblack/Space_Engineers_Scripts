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
        const string UNCONNECT_MSG = "Searching...";

        const string DF_CONNECT_COLOR = "0,127,0";
        const string DF_DISCONNECT_COLOR = "24,24,24";

        //IMyBroadcastListener _listener;
        bool _commsEnabled;

        public static Dictionary<string, ICommsScreen> _receiverScreens;
        public static Dictionary<string, ICommsScreen> _broadcasterScreens;

        public int _currentBcScreen = 0;
        public int _currentRcScreen = 0;

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
            
            public IMyBroadcastListener Listener { get; set; }

            public string BroadcastTag { get; set; }

            public string LastMessage { get; set; }

            public bool IsConnected { get; set; }

            public Color ConnectedColor { get; set; }
            public Color DisconnectColor { get; set; }

            public LcdRecieverScreen(IMyTextSurface textSurface, string broadcastTag, Color connected, Color disconnected)
            {
                TextSurface = textSurface;
                TextSurface.ContentType = ContentType.TEXT_AND_IMAGE;
                BroadcastTag = broadcastTag;
                ConnectedColor = connected;
                DisconnectColor = disconnected;
                TextSurface.FontColor = DisconnectColor;
                IsConnected = false;
                LastLcdReceipt = DateTime.MinValue;
                LoadLastMessage();

                MessageToScreen(LastMessage, true);
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

                LastLcdReceipt = DateTime.Now;
                LastMessage = message;
                TextSurface.WriteText(BroadcastTag + " - " + status + "\n" + LastMessage);

                if (!IsConnected)
                {
                    IsConnected = true;
                    TextSurface.FontColor = ConnectedColor;
                    TextSurface.ClearImagesFromSelection();
                }
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
                        Color connectedColor = ParseColor(IniHandler.GetKey(COMMS_HEADER, CONNECT_LABEL, DF_CONNECT_COLOR));
                        Color disconnectColor = ParseColor(IniHandler.GetKey(COMMS_HEADER, DISCONNECT_LABEL, DF_DISCONNECT_COLOR));

                        _receiverScreens.Add(channel, new LcdRecieverScreen(surface, channel, connectedColor, disconnectColor));
                    }
                        
                }
                catch
                {
                    _statusMessage += "Error Adding "+ Block.CustomName +"\n At: " + channel + "\n";
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
            _receiverScreens = new Dictionary<string, ICommsScreen>();
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

            _commsEnabled = _broadcasterScreens.Count() + _receiverScreens.Count() > 0;

            RegisterReceivers();
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

            CheckCurrentReceiverConnection();
        }





        public void ShowBroadcastData()
        {
            if(!_commsEnabled) return;

            Echo("Broadcast Screen: " + _broadcasterScreens.Count());
            Echo("Receiver Screens: " + _receiverScreens.Count());
        }

        public int GetCurrentBroadCasterIndex()
        {
            _currentBcScreen++;
            if(_currentBcScreen >= _broadcasterScreens.Count)
                _currentBcScreen = 0;

            return _currentBcScreen;
        }


        public int GetCurrentRecieverIndex()
        {
            _currentRcScreen++;
            if(_currentRcScreen >= _receiverScreens.Count)
                _currentRcScreen = 0;

            return _currentRcScreen;
        }


        public void BroadcastCurrentScreen()
        {
            List<string> keys = _broadcasterScreens.Keys.ToList();
            ICommsScreen screen = _broadcasterScreens[keys[GetCurrentBroadCasterIndex()]];

            StringBuilder stringBuilder = new StringBuilder();
            screen.TextSurface.ReadText(stringBuilder);
            string message = stringBuilder.ToString();

            IGC.SendBroadcastMessage(screen.BroadcastTag, message);
        }

        public void RegisterReceiver(LcdRecieverScreen receiver)
        {
            receiver.Listener = IGC.RegisterBroadcastListener(receiver.BroadcastTag);
            receiver.Listener.SetMessageCallback(receiver.BroadcastTag);
        }


        public void RegisterReceivers()
        {
            if (_receiverScreens.Count < 1)
                return;

            foreach(string key in _receiverScreens.Keys)
                RegisterReceiver(_receiverScreens[key] as LcdRecieverScreen);
        }


        public void ReceiveMessages()
        {
            if( _receiverScreens.Count < 1) return;

            foreach (LcdRecieverScreen screen in _receiverScreens.Values)
            {
                while (screen.Listener.HasPendingMessage)
                {
                    MyIGCMessage message = screen.Listener.AcceptMessage();
                    screen.MessageToScreen(message.Data.ToString());
                }
            }
        }


        public void CheckCurrentReceiverConnection()
        {
            if (_receiverScreens.Count < 1) return;

            List<string> keys = _receiverScreens.Keys.ToList();
            LcdRecieverScreen screen = _receiverScreens[keys[GetCurrentRecieverIndex()]] as LcdRecieverScreen;

            if(screen.IsConnected && screen.IsTimedOut())
            {
                screen.IsConnected = false;
                screen.TextSurface.FontColor = screen.DisconnectColor;
                screen.TextSurface.AddImageToSelection("Danger");
                screen.TextSurface.WriteText(screen.BroadcastTag + " - "+ DISCONNECT_MSG + "\n" + screen.LastMessage);
            }
        }
    }
}
