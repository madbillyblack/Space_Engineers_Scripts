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
        List<Printer> _printers;

        // PRINTER
        // Container Class for an individual Ship Printer
        public class Printer
        {
            public string Tag { get; set; } // Format PRINTER 1 or PRINTER C or PRINTER Foo

            public IMyTimerBlock Timer {  get; set; }
            public IMyProjector Projector { get; set; }

            public List<PrintPiston> Pistons { get; set; }
            public List<IMyMotorStator> Rotors { get; set; }
            public List<PrintHinge> Hinges { get; set; }
            public List<IMyLightingBlock> Lights { get; set; }
            public List<IMyShipWelder> Welders { get; set; }
            public List<IMySensorBlock> Sensors { get; set; }

            private MyIni Ini { get; set; }

            public Printer(IMyTimerBlock timer, IMyProjector projector, List<PrintPiston> pistons, List<IMyShipWelder> welders)
            {
                Timer = timer;
                Projector = projector;

                Ini = GetIni(Timer);

                Pistons = pistons;
                Welders = welders;

                // Optional Lists
                Rotors = new List<IMyMotorStator>();
                Hinges = new List<PrintHinge>();
                Lights = new List<IMyLightingBlock>();
                Sensors = new List<IMySensorBlock>();
            }

            public void AddRotorsAndHinges(IMyBlockGroup group)
            {
                List<IMyMotorStator> stators = new List<IMyMotorStator>();
                group.GetBlocksOfType<IMyMotorStator>(stators);

                if(stators.Count < 1) { return; }

                foreach(IMyMotorStator stator in stators)
                {
                    string blockDef = stator.BlockDefinition.ToString().ToLower();
                    if (blockDef.Contains("rotor"))
                        Rotors.Add(stator);
                    

                }
            }

            public void Run() { }

            public void Pause() { }

            public void Reset() { }

            public void Retract() { }

            public void Detach() { }

            public string CurrentMode()
            {
                return "";
                //TODO
            }

            private enum mode { RUNNING, RETRACTING, RESETTING, READY, DETACHING, DETACHED, PAUSED}
        }


        public void AddPrinters()
        {
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            if (groups.Count < 1) { return; }

            foreach (IMyBlockGroup group in groups)
            {
                if (group.Name.Contains(GROUP_TAG))
                {
                    Printer printer = PrinterFromGroup(group);
                    if(printer != null)
                    {
                        _printers.Add(printer);
                    }
                }
            }
        }


        public Printer PrinterFromGroup(IMyBlockGroup group)
        {
            // Assign Timer
            IMyTimerBlock timer = TimerFromGroup(group);
            if (timer == null) { return null; }

            // Assign Projector
            IMyProjector projector = ProjectorFromGroup(group);
            if(projector == null) { return null; }

            // Assign Pistons & initialize ini data
            List<PrintPiston> pistons = PistonsFromGroup(group);
            if(pistons.Count < 1) { return null; }

            // Assign Welders
            List<IMyShipWelder> welders = WeldersFromGroup(group);
            if(welders.Count < 1) { return null; }

            // Create Printer from required components
            Printer printer = new Printer(timer, projector, pistons, welders);

            #region Add Optional Components
            // Add Rotors & Hinges
            printer.AddRotorsAndHinges(group);

            // Add Sensors
            group.GetBlocksOfType(printer.Lights);

            // Add Lights
            group.GetBlocksOfType<IMyLightingBlock>(printer.Lights);
            #endregion

            return printer;
        }

        IMyTimerBlock TimerFromGroup(IMyBlockGroup group)
        {
            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            group.GetBlocksOfType<IMyTimerBlock>(timers);
            if (timers.Count < 1)
            {
                _logger.LogError("Group \"" + group.Name + "\" contains no Timer. Printer cannot be assembled.");
                return null;
            }

            IMyTimerBlock timer = timers[0];

            if (timers.Count > 1)
            {
                _logger.LogWarning("Group \"" + group.Name + "\" has more than one timer. Only timer \"" + timer.Name + "\" will be used.");
            }

            return timer;
        }


        IMyProjector ProjectorFromGroup(IMyBlockGroup group)
        {
            List<IMyProjector> projectors = new List<IMyProjector>();
            group.GetBlocksOfType<IMyProjector>(projectors);

            if(projectors.Count < 1)
            {
                _logger.LogError("Group \"" + group.Name + "\" contains no Projector. Printer cannot be assembled.");
                return null;
            }

            IMyProjector projector = projectors[0];

            if(projectors.Count > 1)
            {
                _logger.LogWarning("Group \"" + group.Name + "\" has more than one projector. Only projector \"" + projector.Name + "\" will be used.");
            }

            return projector;
        }

        public List<PrintPiston> PistonsFromGroup(IMyBlockGroup group)
        {
            List <PrintPiston> pistons = new List<PrintPiston>();
            List<IMyPistonBase> tempList = new List<IMyPistonBase>();
            group.GetBlocksOfType(pistons);

            if(pistons.Count < 1)
            {
                _logger.LogError("Group \"" + group.Name + "\" contains no pistons. Printer cannot be assembled.");
                return pistons;
            }

            foreach(IMyPistonBase tempPiston in tempList)
            {
                pistons.Add(new PrintPiston(tempPiston));
            }

            return pistons;
        }


        public List<IMyShipWelder> WeldersFromGroup(IMyBlockGroup group)
        {
            List<IMyShipWelder> welders = new List<IMyShipWelder>();
            group.GetBlocksOfType(welders);

            if(welders.Count < 1)
                _logger.LogError("Group \"" + group.Name + "\" contains no welders. Printer cannot be assembled.");

            return welders;
        }



    }
}
