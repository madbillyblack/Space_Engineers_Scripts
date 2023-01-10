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
    partial class Program : MyGridProgram
    {
        const string COMPASS_TAG = "[COMPASS]"; // Tag for projector tables
        const string DV_REF = "[Reference]"; // Default Reference Tag Value

        // Breather Variables
        const int BREATH_SIZE = 2;
        int _cycle;
        bool _breatheOut;
        string[] _needle = {"\\  ",
                            " | ",
                            "  /"};

        static string _statusMessage;
        string _refTag;
        static IMyTerminalBlock _me;
        List<Compass> _compasses;
        static IMyTerminalBlock _refBlock;


        public Program()
        {
            _me = Me;

            Build();

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save(){}


        // MAIN //
        public void Main(string argument, UpdateType updateSource)
        {
            PrintBreather();
            Echo(_statusMessage);

            Echo(_refBlock.CustomName + "\n Block: " + _refBlock.WorldMatrix.Forward.ToString());
            AlignCompasses();
        }


        // BUILD //
        void Build()
        {
            _statusMessage = "";
            _programIni = GetIni(Me);
            _gridID = GetKey(SHARED, GRID_KEY, Me.CubeGrid.EntityId.ToString());

            StartBreather();
            AssignReference();
            AssignProjectors();
        }


        // ASSIGN REFERENCE //
        void AssignReference()
        {
            _refTag = GetKey(PROGRAM_HEAD, REF_TAG, DV_REF); // Get Current Reference Tag Value

            List<IMyTerminalBlock> tempList = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(_refTag, tempList);

            if(tempList.Count > 0)
            {
                _refBlock = tempList[0];
            }
            else
            {
                _refBlock = Me;
                _statusMessage += "WARNING: No block with tag \"" + _refTag + "\" found.\n This Program Block set as reference.\n";
            }
        }


        // ASSIGN PROJECTORS //
        void AssignProjectors()
        {
            _compasses = new List<Compass>();
            List<IMyProjector> tempList = new List<IMyProjector>();

            GridTerminalSystem.GetBlocksOfType<IMyProjector>(tempList);
            
            if(tempList.Count > 0)
            {
                foreach(IMyProjector projector in tempList)
                {
                    if(projector.CustomName.Contains(COMPASS_TAG) && projector.BlockDefinition.SubtypeName == "LargeBlockConsole")
                    {
                        _compasses.Add(new Compass(projector));
                    }
                }
            }

            if(_compasses.Count < 1)
            {
                _statusMessage += "No Projectors with tag \"" + COMPASS_TAG + "\" found!\n";
            }
        }



        // START BREATHER //
        void StartBreather()
        {
            _cycle = 0;
            _breatheOut = true;
        }

        // PRINT BREATHER //
        void PrintBreather()
        {
            Echo("HOLO COMPASS                           y  z");
            Echo("//////////////////////////////   x " + _needle[_cycle]);

            // Change direction if at breather start or end of array
            if(_cycle >= BREATH_SIZE)
            {
                _cycle = BREATH_SIZE;
                _breatheOut = false;
            }
            else if(_cycle <= 0)
            {
                _cycle = 0;
                _breatheOut = true;
            }

            if(_breatheOut)
                _cycle++;
            else
                _cycle--;
        }

    }

    
}
