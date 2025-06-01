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
        const string SHARED = "Shared Data";
        const string GRID_KEY = "Grid_ID";
        static string _gridID;

        static MyIniHandler _programIni;

        public class MyIniHandler
        {
            public IMyTerminalBlock Block { get; private set; }

            private MyIni Ini { get; set; }

            public MyIniHandler(IMyTerminalBlock block)
            {
                Block = block;
                Ini = GetIni(Block);
            }

            public void SetKey(string header, string key, string arg)
            {
                Ini.Set(header, key, arg);
                Block.CustomData = Ini.ToString();
            }

            public string GetKey(string header, string key, string defaultVal)
            {
                EnsureKey(header, key, defaultVal);
                return Ini.Get(header, key).ToString(); ;
            }

            public void EnsureKey(string header, string key, string defaultVal)
            {
                if (!Ini.ContainsKey(header, key))
                    SetKey(header, key, defaultVal);
            }

            public bool HasSameGridId()
            {
                return GetKey(SHARED, GRID_KEY, _gridID) == _gridID;
            }

            public bool HasKey(string header, string key)
            {
                return Ini.ContainsKey(header, key);
            }
        }


        // ENSURE KEY // Check to see if INI key exists, and if it doesn't write with default value.
        static void EnsureKey(IMyTerminalBlock block, string header, string key, string defaultVal)
        {
            //if (!block.CustomData.Contains(header) || !block.CustomData.Contains(key))
            MyIni ini = GetIni(block);
            if (!ini.ContainsKey(header, key))
                SetKey(block, header, key, defaultVal);
        }


        // GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
        static string GetKey(IMyTerminalBlock block, string header, string key, string defaultVal)
        {
            EnsureKey(block, header, key, defaultVal);
            MyIni blockIni = GetIni(block);
            return blockIni.Get(header, key).ToString();
        }


        // SET KEY // Update ini key for block, and write back to custom data.
        static void SetKey(IMyTerminalBlock block, string header, string key, string arg)
        {
            MyIni blockIni = GetIni(block);
            blockIni.Set(header, key, arg);
            block.CustomData = blockIni.ToString();
        }


        // GET INI // Get entire INI object from specified block.
        static MyIni GetIni(IMyTerminalBlock block)
        {
            MyIni iniOuti = new MyIni();

            MyIniParseResult result;
            if (!iniOuti.TryParse(block.CustomData, out result))
            {
                block.CustomData = "---\n" + block.CustomData;
                if (!iniOuti.TryParse(block.CustomData, out result))
                    throw new Exception(result.ToString());
            }

            return iniOuti;
        }


        // SET GRID ID // Updates Grid ID parameter for all designated blocks in Grid, then rebuilds the grid.
        void SetGridID(string arg)
        {
            string gridID;
            if (arg != "")
                gridID = arg;
            else
                gridID = Me.CubeGrid.EntityId.ToString();

            SetKey(Me, SHARED, "Grid_ID", gridID);
            _gridID = gridID;

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

            foreach (IMyTerminalBlock block in blocks)
            {
                if (block.CustomData.Contains(SHARED))
                    SetKey(block, SHARED, "Grid_ID", gridID);
            }

            Build();
        }


        // SAME GRID ID // - By default unassigned blocks will be given current Grid's ID.
        bool SameGridID(IMyTerminalBlock block, bool useDefaultValue=true)
        {
            if (!useDefaultValue && !block.CustomData.Contains(SHARED))
                return false;

            if (GetKey(block, SHARED, GRID_KEY, _gridID) == _gridID)
                return true;
            else
                return false;
        }
    }
}
