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
        const string DF_LCD_COMTAG = "LFS Open Channel";
        const string LCD_COMMS_LABEL = "LCD Channel";

        IMyBroadcastListener _listener;
        bool _commsEnabled;

        public Dictionary<string, ICommsScreen> _receiverScreens;
        public Dictionary<string, ICommsScreen> _broadcasterScreens;

        public interface ICommsScreen
        {
            IMyTextSurface TextSurface { get; set; }
            string BroadcastTag { get; set; }
        }

        // Class to store date for messages from individual lcd sender (at the listener)
        public class LcdRecieverScreen : ICommsScreen
        {
            public IMyTextSurface TextSurface { get; set; }
            public DateTime LastLcdReceipt { get; set; }
            
            public string BroadcastTag { get; set; }

            public LcdRecieverScreen(IMyTextSurface textSurface, string broadcastTag)
            {
                TextSurface = textSurface;
                BroadcastTag = broadcastTag;
            }

            public bool IsConnected()
            {
                return DateTime.Now - LastLcdReceipt < TimeSpan.FromSeconds(BROADCAST_TIMEOUT);
            }
        }

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


        public void AddReceiverScreens()
        {
            _receiverScreens = new Dictionary<string, ICommsScreen>();

            List<IMyTextSurfaceProvider> providers = new List<IMyTextSurfaceProvider>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(providers);

            if (providers.Count < 1) return;

            foreach (IMyTextSurfaceProvider provider in providers)
            {
                if ((provider as IMyTerminalBlock).CustomName.Contains(DF_RC_SCREEN_TAG))
                    AddCommsScreensFromBlock(provider, _receiverScreens);
            }
        }

        public void AddBroadcasterScreens()
        {
            _broadcasterScreens = new Dictionary<string, ICommsScreen>();

            List<IMyTextSurfaceProvider> providers = new List<IMyTextSurfaceProvider>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(providers);

            if (providers.Count < 1) return;

            foreach (IMyTextSurfaceProvider provider in providers)
            {
                if ((provider as IMyTerminalBlock).CustomName.Contains(DF_BC_SCREEN_TAG))
                    AddCommsScreensFromBlock(provider, _broadcasterScreens);
            }
        }

        public void AddCommsScreensFromBlock(IMyTextSurfaceProvider provider, Dictionary<string, ICommsScreen> screens, bool receiver = true)
        {
            // TODO
        }


        void BuildComms()
        {
            AddReceiverScreens();
            AddBroadcasterScreens();

            _commsEnabled = _broadcasterScreens.Count() + _receiverScreens.Count() > 0;
        }
    }
}
