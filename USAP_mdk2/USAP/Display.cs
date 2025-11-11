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
            bool ShowHeader;
            bool ShowStatus;
            bool ShowLoadCount;
            bool ShowTargetSpeed;
            bool ShowCruiseThrust;
            bool ShowActiveProfile;
            bool ShowLandingGear;
            bool ShowParked;
            bool ShowTurrets;
            bool ShowActionRelays;

            public Display(IMyTextSurfaceProvider surfaceProvider, int surfaceIndex, string iniHeader)
            {
                SurfaceProvider = surfaceProvider;
                Surface = SurfaceProvider.GetSurface(surfaceIndex);
                Surface.ContentType = ContentType.TEXT_AND_IMAGE;

                IMyTerminalBlock block = SurfaceProvider as IMyTerminalBlock;

                string isProgram;

                if (block == _Me)
                    isProgram = "True";
                else
                    isProgram = "False";

                ShowHeader = ParseBool(GetKey(block, iniHeader, "Show Header", isProgram));
                ShowStatus = ParseBool(GetKey(block, iniHeader, "Show Status", isProgram));

                // Set boolean strings to true depending on populated lists.
                if (_miningCargos.Count > 0)
                {
                    ShowLoadCount = ParseBool(GetKey(block, iniHeader, "Show Load Count", "True"));
                }
                    
                if (_cruiseThrusters.Count > 0)
                {
                    ShowTargetSpeed = ParseBool(GetKey(block, iniHeader, "Show Target Speed", "True"));
                    ShowCruiseThrust = ParseBool(GetKey(block, iniHeader, "Show Throttle Level", "True"));
                }

                if (_constructionCargos.Count > 0)
                {
                    ShowActiveProfile = ParseBool(GetKey(block, iniHeader, "Show Active Profile", "True"));
                }
                    
                if (_landingGear != null)
                {
                    ShowLandingGear = ParseBool(GetKey(block, iniHeader, "Show Landing Gear", "True"));

                    if (_landingGear.Connectors.Count > 0 || _landingGear.LandingPlates.Count > 0)
                        ShowParked = ParseBool(GetKey(block, iniHeader, "Show Parking Status", "True"));
                }

                if(_turrets.Count > 0)
                {
                    ShowTurrets = ParseBool(GetKey(block, iniHeader, "Show Turrets", "True"));
                }

                if(_transponders.Count > 0)
                {
                    ShowActionRelays = ParseBool(GetKey(block, iniHeader, "Show Action Relays", "True"));
                }
            }


            // PRINT //
            public void Print()
            {
                string output = "";
                int p = 0; // if p > 0, print

                if(ShowHeader)
                {
                    output += HEADER + "\n" + SLASHES + SLASHES + "////////  ";
                    if (_autoCycle)
                        output += _breather[_breath];

                    output += "\n";
                    p++;
                }

                if (ShowStatus)
                {
                    output += _statusMessage + "\n";
                    p++;
                }
                    
                if (ShowLoadCount)
                {
                    output += "Load Count: " + _loadCount.ToString() + "\n";
                    p++;
                }
                    
                if (ShowTargetSpeed)
                {
                    output += "Target Speed: " + _targetThrottle.ToString("0.0") + " m/s\n";
                    p++;
                }
                    
                if (ShowCruiseThrust)
                {
                    output += "Auto-Throttle: " + _currentPower + "\n";
                    p++;
                }
                    
                if (ShowActiveProfile)
                {
                    output += "Active Profile: " + _activeProfile + "\n";
                    p++;
                }
                    
                if (ShowLandingGear && _landingGear != null)
                {
                    output += "Gear: " + _landingGear.Status + "\n";
                    p++;
                }
                    
                if (ShowParked && _landingGear != null)
                {
                    if (_landingGear.IsParked())
                        output += "Parking: Locked\n";
                    else
                        output += "Parking: Unlocked\n";

                    p++;
                }

                if (ShowTurrets)
                {
                    output += _turretString.Trim() + "\n";
                    p++;
                }

                if (ShowActionRelays)
                {
                    output += _relayString.Trim() + "\n";
                    p++;
                }

                if (p > 0)
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

            UpdateTurretString();
            UpdateRelayString();

            foreach (Display display in _displays)
                display.Print();
        }
    }
}
