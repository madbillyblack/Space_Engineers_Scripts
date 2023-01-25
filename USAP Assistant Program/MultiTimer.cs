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
        // MULTI TIMER //
        public class MultiTimer
        {
            public IMyTimerBlock Block;
            MyIni Ini;
            Dictionary<string, Phase> Phases;


            public MultiTimer(IMyTimerBlock timer)
            {
                Block = timer;
                Ini = GetIni(Block as IMyTerminalBlock);
            }

            //ENSURE KEY
            void EnsureKey(string header, string key, string defaultVal)
            {
                if (!Ini.ContainsKey(header, key))
                    SetKey(header, key, defaultVal);
            }

            // GET KEY
            public string GetKey(string header, string key, string defaultVal)
            {
                EnsureKey(header, key, defaultVal);
                return Ini.Get(header, key).ToString();
            }

            // SET KEY
            public void SetKey(string header, string key, string arg)
            {
                Ini.Set(header, key, arg);
                UpdateCustomData();
            }

            // UPDATE CUSTOM DATA
            public void UpdateCustomData()
            {
                Block.CustomData = Ini.ToString();
            }
        }


        // PHASE //
        public class Phase
        {
            public float Duration;

        }


        // ACTION BLOCK //
        public class ActionBlock
        {
            public List<IMyTerminalBlock> Blocks;
            public IMyProgrammableBlock ProgramBlock;
            public string Action;
            public bool IsProgramBlock;

            // Constructor(s)
            public ActionBlock(IMyTerminalBlock block, string action)
            {
                Init(action);
                Blocks.Add(block);
            }

            public ActionBlock(List<IMyTerminalBlock> blocks, string action)
            {
                Init(action);
                Blocks = blocks;
            }

            public ActionBlock(IMyProgrammableBlock programBlock, string action)
            {
                Init(action, programBlock);
            }

            // INIT
            void Init(string action, IMyProgrammableBlock programBlock = null)
            {
                List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
                Action = action;

                ProgramBlock = programBlock;
                IsProgramBlock = ProgramBlock != null;
            }
        }
    }
}
