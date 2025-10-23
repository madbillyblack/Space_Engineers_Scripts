using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
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
        #region Keep these variables
        IMyTextSurface _surface;
        MyIni _ini;
        #endregion

        #region Test Variables (Can be deleted)
        IMyProjector _projector;
        #endregion

        #region
        // PRORGRAM //
        public Program()
        {
            #region Data Screen Init
            _ini = GetIni(Me);
            _surface = Me.GetSurface(0);
            _surface.ContentType = ContentType.TEXT_AND_IMAGE;
            #endregion

            List<IMyProjector> projectors = new List<IMyProjector>();
            GridTerminalSystem.GetBlocksOfType<IMyProjector>(projectors);
            _projector = projectors.FirstOrDefault();
            
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }


        // SAVE //
        public void Save(){}
        #endregion


        public void Main(string argument, UpdateType updateSource)
        {
            if (_projector == null)
            {
                PrintData("No Projector Blocks Found!");
                return;
            }

            if (argument.ToUpper().Trim() == "TOGGLE")
                ToggleBuildable();
            
            PrintProjectorData();
        }


        void PrintData(string data)
        {
            Echo(data);
            _surface.WriteText(data);
        }


        void PrintProjectorData()
        {
            string output = "Buildable Blocks: " + _projector.BuildableBlocksCount
                + "\nRemaining Armor: " + _projector.RemainingArmorBlocks
                + "\nRemaining Blocks: " + _projector.RemainingBlocks
                + "\nTotal Blocks: " + _projector.TotalBlocks
                + "\nDetails: " + _projector.DetailedInfo;

            PrintData(output);
        }

        void ToggleBuildable()
        {
            Echo("Toggling Buildable");

            if (_projector.ShowOnlyBuildable)
                _projector.ShowOnlyBuildable = false;
            else
                _projector.ShowOnlyBuildable = true;
        }


        IMyTextSurface GetDefaultSurface()
        {
            try
            {
                List<IMyTextPanel> panels = new List<IMyTextPanel>();
                GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(panels);
                return panels.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
