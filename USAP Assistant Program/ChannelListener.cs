using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        static Dictionary<string, ChannelListener> _listeners;

        const string LISTENER_KEY = "Receiver Channels";
        static string _defaultChannels = DF_LCD_COMTAG + "0\n" +
                                DF_LCD_COMTAG + "1\n" +
                                DF_LCD_COMTAG + "2";

        public class ChannelListener
        {
            public string BroadcastTag { get; set; }
            public DateTime LastReceived {  get; set; }
            public string Message { get; set; }
            public IMyBroadcastListener Listener { get; set; }
            public ChannelListener(string broadcastTag)
            {
                BroadcastTag = broadcastTag;
                LastReceived = DateTime.MinValue;
                Message = "";

            }

            public bool IsTimedOut()
            {
                return DateTime.Now - LastReceived > TimeSpan.FromSeconds(BROADCAST_TIMEOUT);
            }
        }


        public void RegisterListener(ChannelListener channelListener)
        {
            channelListener.Listener = IGC.RegisterBroadcastListener(channelListener.BroadcastTag);
            channelListener.Listener.SetMessageCallback(channelListener.BroadcastTag);
        }


        public void AssignChannelListeners()
        {
            _listeners = new Dictionary<string, ChannelListener>();
            string [] channels = GetProgramKey(LISTENER_KEY, _defaultChannels).Split('\n');

            foreach (string channel in channels)
            {
                if (channel.Trim() == "")
                    continue;

                ChannelListener listener = new ChannelListener(channel);

                _listeners.Add(channel, listener);
                RegisterListener(listener);
            }
        }
    }
}
