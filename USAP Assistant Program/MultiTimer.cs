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
        const string MULTI_TAG = "[MULTI:";
        const string MULTI_HEADER = DASHES + " USAP: Multi-Timer " + DASHES;
        const string PHASE_HEADER = "Timer Phase ";
        const string PHASE_KEY = "Phase Count";
        Dictionary<string, MultiTimer> _multiTimers;

        // MULTI TIMER //
        public class MultiTimer
        {
            public IMyTimerBlock Block;
            public string Tag;
            public int PhaseCount;
            public Dictionary<string, Phase> Phases;

            MyIni Ini;

            public MultiTimer(IMyTimerBlock timer)
            {
                Block = timer;
                Ini = GetIni(Block as IMyTerminalBlock);

                PhaseCount = ParseInt(GetKey(MULTI_HEADER, PHASE_KEY, "2"), 2);
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
            public int Number;
            public int ActionCount;
            public float Duration;
            public List<ActionBlock> Actions;
            public string Header;

            public Phase(MultiTimer timer, int number)
            {
                Actions = new List<ActionBlock>();

                Number = number;
                Header = PHASE_HEADER + Number.ToString();

                Duration = ParseFloat(timer.GetKey(Header, "Duration", timer.Block.TriggerDelay.ToString("0.##")), timer.Block.TriggerDelay);
                ActionCount = ParseInt(timer.GetKey(Header, "Action Count", "1"), 1);
            }

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

            // ACTIVATE
            public void Activate()
            {
                if(ProgramBlock != null)
                {
                    if (ProgramBlock == _Me)
                        RunNext(Action);
                    else
                        ProgramBlock.TryRun(Action);
                }
                else if(Blocks.Count > 0)
                {
                    foreach (IMyTerminalBlock block in Blocks)
                    {
                        try
                        {
                            block.GetActionWithName(Action).Apply(block);
                        }
                        catch
                        {
                            _statusMessage += block.CustomName + " cannot perform action \"" + Action + "\"!\n";
                        }
                    }
                }
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


        // ASSIGN MULTI TIMERS //
        void AssignMultiTimers()
        {
            _multiTimers = new Dictionary<string, MultiTimer>();

            List<IMyTerminalBlock> timers = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timers);

            if (timers.Count < 1)
                return;

            foreach(IMyTimerBlock timer in timers)
            {
                if(timer.CustomName.ToUpper().Contains(MULTI_TAG) && SameGridID(timer))
                {
                    MultiTimer multiTimer = new MultiTimer(timer);
                    AssignPhases(multiTimer);

                    _multiTimers.Add(multiTimer.Tag, multiTimer); // Use Tag parameter as dictionary key.
                }
            }
        }


        // ASSIGN PHASES //
        void AssignPhases(MultiTimer timer)
        {
            int count = timer.PhaseCount;

            for(int i = 0; i < count; i++)
            {
                Phase phase = new Phase(timer, i);
                AssignActions(timer, phase);

                timer.Phases.Add(i.ToString(), phase);
            }
        }


        // ASSIGN ACTIONS //
        void AssignActions(MultiTimer timer, Phase phase)
        {
            if (phase.ActionCount < 1)
                return;

            for(int i = 0; i < phase.ActionCount; i++)
            {
                string blockString = timer.GetKey(phase.Header, "Block " + i, "");
                string actionString = timer.GetKey(phase.Header, "Action " + i, "");

                if(blockString != "" && actionString != "")
                {
                    ActionBlock actionBlock;

                    if (blockString.ToUpper().StartsWith("G:"))
                        actionBlock = ActionBlockFromGroupName(blockString, actionString);
                    else if (blockString.ToUpper().StartsWith("P:"))
                        actionBlock = ActionBlockFromProgramName(blockString, actionString);
                    else
                        actionBlock = ActionBlockFromBlockName(blockString, actionString);

                    if (actionBlock != null)
                        phase.Actions.Add(actionBlock);
                }
            }
        }


        // ACTION BLOCK FROM GROUP NAME //
        ActionBlock ActionBlockFromGroupName(string groupName, string action)
        {
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(groupName.Substring(2).Trim());
            
            if (group == null)
                return null;

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            group.GetBlocks(blocks);

            return new ActionBlock(blocks, action);
        }


        // ACTION BLOCK FROM PROGRAM NAME //
        ActionBlock ActionBlockFromProgramName(string programName, string action)
        {
            string name = programName.Substring(2).Trim();

            IMyTerminalBlock programBlock = GridTerminalSystem.GetBlockWithName(name);

            if (programBlock == null)
                return null;

            // Verify Block Type
            string[] data = programBlock.GetType().ToString().Split('.');
            string type = data[data.Length - 1].Trim();

            if (type == "MyProgrammableBlock" && SameGridID(programBlock))
                return new ActionBlock(programBlock as IMyProgrammableBlock, action);

           return null;
        }


        // ACTION BLOCK FROM BLOCK NAME //
        ActionBlock ActionBlockFromBlockName(string blockName, string action)
        {
            IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(blockName);

            if (block == null)
                return null;

            return new ActionBlock(block, action);
        }
    }
}
