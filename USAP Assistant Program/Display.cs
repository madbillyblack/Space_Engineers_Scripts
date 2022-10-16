using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
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
        public class Display
        {
            IMyTextSurfaceProvider SurfaceProvider;
            IMyTextSurface Surface;
            bool ShowLoadCount;
            bool ShowEscapeThrust;
            bool ShowActiveProfile;

            public Display(IMyTextSurfaceProvider surfaceProvider, int surfaceIndex, string iniHeader)
            {
                SurfaceProvider = surfaceProvider;
                Surface = SurfaceProvider.GetSurface(surfaceIndex);
                Surface.ContentType = ContentType.TEXT_AND_IMAGE;

                IMyTerminalBlock block = SurfaceProvider as IMyTerminalBlock;

                // Set boolean strings to true depending on populated lists.
                if (_miningCargos.Count > 0)
                    ShowLoadCount = ParseBool(GetKey(block, iniHeader, "Show_Load_Count", "true"));
                if (_escapeThrusters.Count > 0)
                    ShowEscapeThrust = ParseBool(GetKey(block, iniHeader, "Show_Auto_Throttle", "true"));
                if (_constructionCargos.Count > 0)
                    ShowActiveProfile = ParseBool(GetKey(block, iniHeader, "Show_Active_Profile", "true"));
            }

            public void Print()
            {
                string output = "";

                if (ShowLoadCount)
                    output += "Load Count: " + _loadCount.ToString() + "\n";
                if (ShowEscapeThrust)
                    output += "Auto-Throttle: " + _currentPower + "\n";
                if (ShowActiveProfile)
                    output += "Active Profile: " + _activeProfile + "\n";

                if(ShowLoadCount || ShowEscapeThrust || ShowActiveProfile)
                    Surface.WriteText(output.Trim());
            }

        }


        void DisplaysFromBlock(IMyTerminalBlock block)
        {
            int surfaceCount = (block as IMyTextSurfaceProvider).SurfaceCount;
            if (surfaceCount < 1)
            {
                _statusMessage += "BLOCK HAS NO TEXT SURFACES:\n" + block.CustomName + "\n";
                return;
            }
            else if (surfaceCount == 1)
            {
                Display screen = new Display(block as IMyTextSurfaceProvider, 0, "USAP Screen 0");
                _displays.Add(screen);
            }
            else
            {
                string defaultBool = "true";

                for(int i = 0; i < surfaceCount; i++)
                {
                    if (ParseBool(GetKey(block, INI_HEAD, "Show on screen " + i, defaultBool))) 
                    {
                        Display display = new Display(block as IMyTextSurfaceProvider, i, "USAP Screen " + i);
                        _displays.Add(display);
                    }

                    defaultBool = "false";
                }
            }
        }


        void AssignDisplays()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(DISPLAY_TAG, blocks);

            if (blocks.Count < 1)
                return;

            foreach (IMyTerminalBlock block in blocks)
                DisplaysFromBlock(block);
        }


        void PrintDisplays()
        {
            if (_displays.Count < 1)
                return;

            foreach (Display display in _displays)
                display.Print();
        }
    }
}
