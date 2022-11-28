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
        const string DISPLAY_HEAD = "USAP Display Screens";

        public class Display
        {


            IMyTextSurfaceProvider SurfaceProvider;
            IMyTextSurface Surface;
            bool ShowLoadCount;
            bool ShowTargetSpeed;
            bool ShowCruiseThrust;
            bool ShowActiveProfile;
            bool ShowLandingGear;
            bool ShowParked;

            public Display(IMyTextSurfaceProvider surfaceProvider, int surfaceIndex, string iniHeader)
            {
                SurfaceProvider = surfaceProvider;
                Surface = SurfaceProvider.GetSurface(surfaceIndex);
                Surface.ContentType = ContentType.TEXT_AND_IMAGE;

                IMyTerminalBlock block = SurfaceProvider as IMyTerminalBlock;

                // Set boolean strings to true depending on populated lists.
                if (_miningCargos.Count > 0)
                {
                    ShowLoadCount = ParseBool(GetKey(block, iniHeader, "Show Load Count", "true"));
                }
                    
                if (_cruiseThrusters.Count > 0)
                {
                    ShowTargetSpeed = ParseBool(GetKey(block, iniHeader, "Show Target Speed", "True"));
                    ShowCruiseThrust = ParseBool(GetKey(block, iniHeader, "Show Throttle Level", "true"));
                }

                if (_constructionCargos.Count > 0)
                {
                    ShowActiveProfile = ParseBool(GetKey(block, iniHeader, "Show Active Profile", "True"));
                }
                    
                if (_landingGear != null)
                {
                    ShowLandingGear = ParseBool(GetKey(block, iniHeader, "Show Landing Gear", "True"));

                    if (_landingGear.Connectors.Count > 0 || _landingGear.LandingPlates.Count > 0)
                        ShowParked = ParseBool(GetKey(block, iniHeader, "Show Parking Status", "true"));
                }
            }


            // PRINT //
            public void Print()
            {
                string output = "";

                if (ShowLoadCount)
                    output += "Load Count: " + _loadCount.ToString() + "\n";
                if (ShowTargetSpeed)
                    output += "Target Speed: " + _targetThrottle.ToString("0.0") + " m/s\n";
                if (ShowCruiseThrust)
                    output += "Auto-Throttle: " + _currentPower + "\n";
                if (ShowActiveProfile)
                    output += "Active Profile: " + _activeProfile + "\n";
                if (ShowLandingGear && _landingGear != null)
                    output += "Gear: " + _landingGear.Status + "\n";
                if (ShowParked && _landingGear != null)
                {
                    if (_landingGear.IsParked())
                        output += "Parking: Locked\n";
                    else
                        output += "Parking: Unlocked\n";
                }

                if(ShowLoadCount || ShowCruiseThrust || ShowActiveProfile || ShowLandingGear || ShowParked)
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
                    if (ParseBool(GetKey(block, DISPLAY_HEAD, "Show on screen " + i, defaultBool))) 
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
                if(GetKey(block, SHARED, "Grid_ID", _gridID) == _gridID)
                    DisplaysFromBlock(block);
        }


        static void PrintDisplays()
        {
            if (_displays.Count < 1)
                return;

            foreach (Display display in _displays)
                display.Print();
        }
    }
}
