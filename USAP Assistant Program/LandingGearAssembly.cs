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
        public class LandingGearAssembly
        {
            public List<IMyLandingGear> LandingPlates;
            public List<IMyPistonBase> Pistons;
            public List<IMyMotorStator> Stators;
            public List<IMyLightingBlock> Lights;
            public IMyTimerBlock Timer;
            public bool IsExtended;

            public LandingGearAssembly(IMyTimerBlock timer)
            {
                Timer = timer;
                IsExtended = ParseBool(GetKey(timer, INI_HEAD, "Extended", "False"));
                timer.TriggerDelay = ParseFloat(GetKey(timer, INI_HEAD, "Delay", timer.TriggerDelay.ToString()), timer.TriggerDelay);

                LandingPlates = new List<IMyLandingGear>();
                Pistons = new List<IMyPistonBase>();
                Stators = new List<IMyMotorStator>();
                Lights = new List<IMyLightingBlock>();
            }

           
        }


        void AssembleLandingGear()
        {
            _landingGear = null;

            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timers);

            if (timers.Count < 1)
                return;

            foreach(IMyTimerBlock timer in timers)
            {
                if(timer.CustomName.Contains(GEAR_TAG) && GetKey(timer, SHARED, "Grid_ID", timer.CubeGrid.EntityId.ToString()) == _gridID)
                {
                    _landingGear = new LandingGearAssembly(timer);

                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.SearchBlocksOfName(GEAR_TAG, blocks);

                    foreach(IMyTerminalBlock block in blocks)
                    {
                        if(GetKey(block, SHARED, "Grid_ID", block.CubeGrid.EntityId.ToString()) == _gridID)
                        {
                            switch(block.DefinitionDisplayNameText)
                            {
                                case "Piston":
                                    AssignLandingStrut(block as IMyPistonBase);
                                    break;
                                case "Rotor":
                                case "Advanced Rotor":
                                case "Hinge":
                                case "Hinge 3x3":
                                    AssignLandingStator(block as IMyMotorStator);
                                    break;
                                case "Landing Gear":
                                case "Magnetic Plate":
                                case "Large Magnetic Plate":
                                    AssignLandingPlate(block as IMyLandingGear);
                                    break;
                                case "Spotlight":
                                case "Searchlight":
                                case "Light Panel":
                                case "Offset Spotlight":
                                case "Offset Light":
                                case "Rotating Light":
                                case "Corner Light - Double":
                                case "Corner Light":
                                case "Interior Light":
                                    AssignLandingLight(block as IMyLightingBlock);
                                    break;
                            }
                        }
                    }
           
                    return;
                }
            }
        }

        void AssignLandingPlate(IMyLandingGear landingPlate)
        {
            EnsureKey(landingPlate, INI_HEAD, "AutoLock on Retract", "True");
            EnsureKey(landingPlate, INI_HEAD, "AutoLock on Extend", "True");
            _landingGear.LandingPlates.Add(landingPlate);
        }

        void AssignLandingStator(IMyMotorStator stator)
        {
            string defaultBool;
            float velocity = stator.TargetVelocityRPM;

            if ((_landingGear.IsExtended && velocity >= 0) || (!_landingGear.IsExtended && velocity < 0))
                defaultBool = "True";
            else
                defaultBool = "False";

            EnsureKey(stator, INI_HEAD, "Extend To Positive", defaultBool);

            _landingGear.Stators.Add(stator);
        }

        void AssignLandingStrut(IMyPistonBase strut)
        {
            string defaultBool;
            float velocity = strut.Velocity;

            if ((_landingGear.IsExtended && velocity >= 0) || (!_landingGear.IsExtended && velocity < 0))
                defaultBool = "True";
            else
                defaultBool = "False";

            EnsureKey(strut, INI_HEAD, "Extend To Positive", defaultBool);

            _landingGear.Pistons.Add(strut);
        }

        void AssignLandingLight(IMyLightingBlock light)
        {
            string defaultBool;

            if ((_landingGear.IsExtended && light.IsWorking) || (!_landingGear.IsExtended && !light.IsWorking))
                defaultBool = "True";
            else
                defaultBool = "False";

            EnsureKey(light, INI_HEAD, "On for Extension", defaultBool);

            _landingGear.Lights.Add(light);
        }
    }


}
