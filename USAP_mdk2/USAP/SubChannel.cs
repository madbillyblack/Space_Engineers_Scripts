using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
        static Dictionary<string, Channel> _channels;

        const string LISTENER_KEY = "Receiver Channels";
        static string _defaultChannels = DF_LCD_COMTAG + "0\n" +
                                DF_LCD_COMTAG + "1\n" +
                                DF_LCD_COMTAG + "2\n" +
                                DF_LCD_COMTAG + "3\n" +
                                DF_LCD_COMTAG + "4";

        public class SubChannel
        {
            public string BroadcastID { get; set; }
            public string DisplayName { get; set; }
            public DateTime LastReceived {  get; set; }
            public string Message { get; set; }
            public IMyBroadcastListener Listener { get; set; }
            public SubChannel(string broadcastId, string displayName, string message = "")
            {
                BroadcastID = broadcastId;
                DisplayName = displayName;
                Message = message;
                LastReceived = DateTime.MinValue;
            }


            public bool IsTimedOut()
            {
                return DateTime.Now - LastReceived > TimeSpan.FromSeconds(_listenerTimeOut);
            }


            public void ReceiveMessage(string message)
            {
                Message = message;
                LastReceived = DateTime.Now;
            }
        }


        public class Channel
        {
            public string Name { get; set; }
            public IMyBroadcastListener Listener { get; set; }
            public Dictionary<string, SubChannel> SubChannels;


            public Channel(string name)
            {
                Name = name;           
                SubChannels = new Dictionary<string, SubChannel>();
            }

            public string CycleSubChannel(string currentChannelTag, bool previous = false)
            {
                if (SubChannels.Count < 1)
                    return "";

                List<string> subChannelTags = SubChannels.Keys.ToList();

                if(subChannelTags.Count == 1)
                    return subChannelTags[0];

                for(int i = 0; i < subChannelTags.Count; i++)
                {
                    if (subChannelTags[i] == currentChannelTag)
                    {
                        if (previous)
                            i--;
                        else
                            i++;

                        if (i >= subChannelTags.Count)
                            i = 0;
                        else if(i < 0)
                            i = subChannelTags.Count - 1;

                        return subChannelTags[i];
                    }
                }

                return currentChannelTag;
            }
        }


        public void RegisterListener(Channel channel)
        {
            channel.Listener = IGC.RegisterBroadcastListener(channel.Name);
            channel.Listener.SetMessageCallback(channel.Name);
        }


        public void AssignChannels()
        {
            _channels = new Dictionary<string, Channel>();
            string [] channels = _programIniHandler.GetKey(COMMS_HEADER,LISTENER_KEY, _defaultChannels).Split('\n');
            _listenerTimeOut = ParseInt(_programIniHandler.GetKey(COMMS_HEADER, "Listener Time Out", _listenerTimeOut.ToString()), _listenerTimeOut);

            foreach (string channel in channels)
            {
                if (channel.Trim() == "")
                    continue;

                Channel listener = new Channel(channel);

                _channels.Add(channel, listener);
                RegisterListener(listener);
            }
        }
    }
}
