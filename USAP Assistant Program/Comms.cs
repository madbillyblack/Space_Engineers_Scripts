using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

            public LcdRecieverScreen(IMyTextSurface textSurface, string broadcastTag)
            {
                TextSurface = textSurface;
                BroadcastTag = broadcastTag;
                LastLcdReceipt = DateTime.MinValue;
                LastMessage = "";
            }

            public bool IsConnected()
            {
                return DateTime.Now - LastLcdReceipt < TimeSpan.FromSeconds(BROADCAST_TIMEOUT);
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

            private void AssignSingleScreen(int index = 0, bool multiscreen = false)
            {
                string screenId;
                if (multiscreen)
                    screenId = " " + index.ToString();
                else
                    screenId = "";

                IMyTextSurface surface = (Block as IMyTextSurfaceProvider).GetSurface(index);

                string[] channelData = IniHandler.GetKey(COMMS_HEADER, "Screen Channel" + screenId, BROADCAST + ":" + DF_LCD_COMTAG + screenId).Split(':');

                if (channelData.Length != 2)
                    return;

                string cmd = channelData[0].ToUpper();
                string channel = channelData[1];

                if(cmd == BROADCAST)
                    _broadcasterScreens.Add(channel, new LcdBroadcasterScreen(surface,channel));
                else if (cmd == LISTEN)
                    _receiverScreens.Add(channel, new LcdRecieverScreen(surface,channel));
            }

            private void AssignMultiScreen()
            {
                for(int i = 0; i < SurfaceCount; i++)
                {
                    string screenId = " " + i.ToString();

                    AssignSingleScreen(i, true);
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


        public void MessageToScreen(LcdRecieverScreen screen, MyIGCMessage message)
        {
            screen.LastLcdReceipt = DateTime.Now;
            screen.LastMessage = message.Data.ToString();
            screen.TextSurface.WriteText(screen.BroadcastTag + " - Connected\n" + screen.LastMessage);
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
                    MessageToScreen(screen, message);
                }
            }
        }


        public void CheckCurrentReceiverConnection()
        {
            if (_receiverScreens.Count < 1) return;

            List<string> keys = _receiverScreens.Keys.ToList();
            LcdRecieverScreen screen = _receiverScreens[keys[GetCurrentRecieverIndex()]] as LcdRecieverScreen;

            if (!screen.IsConnected())
                screen.TextSurface.WriteText(screen.BroadcastTag + " - DISCONNECTED\n" + screen.LastMessage);
        }
    }
}
