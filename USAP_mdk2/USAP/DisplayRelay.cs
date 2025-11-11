using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineers.Game.Utils;
using System;
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
        public static List<DisplayRelay> _transponders;
        public static string _relayString;

        public class DisplayRelay
        {
            public IMyTransponder Transponder { get; set; }
            public MyIniHandler IniHandler { get; set; }
            public String DisplayName { get; set; }

            public DisplayRelay(IMyTransponder transponder, MyIniHandler iniHandler, string displayName)
            {
                Transponder = transponder;
                IniHandler = iniHandler;
                DisplayName = displayName;
            }
        }

        public void AssignDisplayRelays()
        {
            _transponders = new List<DisplayRelay>();

            List<IMyTransponder> transponderBlocks = new List<IMyTransponder>();
            GridTerminalSystem.GetBlocksOfType<IMyTransponder>(transponderBlocks);

            if (transponderBlocks.Count > 0)
            {
                foreach (IMyTransponder transponderBlock in  transponderBlocks)
                {
                    AssignDisplayRelay(transponderBlock);
                }
            }

            if (_transponders.Count > 0)
            {
                _transponders.Sort((x,y)=> x.DisplayName.CompareTo(y.DisplayName));
            }
        }


        public void AssignDisplayRelay(IMyTransponder transponderBlock)
        {
            MyIniHandler iniHandler = new MyIniHandler(transponderBlock);

            if (!iniHandler.HasSameGridId()) return;

            string name = iniHandler.GetKey(INI_HEAD, DISPLAY_KEY, transponderBlock.CustomName);


            switch (name.Trim().ToUpper())
            {
                case null:
                case "":
                case "FALSE":
                case "OFF":
                    return;
                default:
                    _transponders.Add(new DisplayRelay(transponderBlock, iniHandler, name));
                    break;
            }
        }


        public static void UpdateRelayString()
        {
            if (_transponders.Count < 1) return;

            _relayString = "";

            foreach(DisplayRelay relay in _transponders)
            {
                _relayString += relay.DisplayName + ": Channel " + relay.Transponder.Channel + "\n";
            }
        }
    }
}
