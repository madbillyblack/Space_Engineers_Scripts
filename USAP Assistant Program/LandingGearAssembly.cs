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
            public List<LandingPiston> Pistons;
            public List<LandingStator> Stators;
            public List<LandingPlate> LandingPlates;
            public List<LandingLight> Lights;
            public IMyTimerBlock Timer;
            public bool IsExtended;

            public LandingGearAssembly(IMyTimerBlock timer)
            {
                Timer = timer;
                IsExtended = ParseBool(GetKey(timer, INI_HEAD, "Extended", "False"));
                timer.TriggerDelay = ParseFloat(GetKey(timer, INI_HEAD, "Delay", timer.TriggerDelay.ToString()), timer.TriggerDelay);
                
                Pistons = new List<LandingPiston>();
                Stators = new List<LandingStator>();
                LandingPlates = new List<LandingPlate>();
                Lights = new List<LandingLight>();
            }

            public void Extend()
            {
                if (IsExtended)
                    return;

                Activate(true);
            }

            public void Retract()
            {
                if (!IsExtended)
                    return;

                Activate(false);
            }


            public void Activate(bool extending)
            {
                if (LandingPlates.Count > 0)
                    foreach (IMyLandingGear landingPlate in LandingPlates)
                        ActivateLandingPlate(landingPlate, extending);

                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        ActivateLandingPiston(piston, extending);

                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        ActivateLandingStator(stator, extending);

                if (Lights.Count > 0)
                    foreach (IMyLightingBlock light in Lights)
                        ActivateLandingLight(light, extending);              
            }


            public void Toggle()
            {
                if (IsExtended)
                    Retract();
                else
                    Extend();
            }

            public void TimerCall()
            {
                //TODO
            }
        }

        public class LandingStator
        {
            public IMyMotorStator Stator;
            public bool ExtendToPositive;
            public float ExtendVelocity;
            public float RetractVelocity;

            public LandingStator(IMyMotorStator stator, bool currentlyExtended)
            {
                Stator = stator;

                string defaultBool;
                float velocity = stator.TargetVelocityRPM;

                if ((currentlyExtended && velocity >= 0) || (!currentlyExtended && velocity < 0))
                    defaultBool = "True";
                else
                    defaultBool = "False";

                ExtendToPositive = ParseBool(GetKey(stator, INI_HEAD, "Extend To Positive", defaultBool));

                // Set default extension and retraction velocities based on current velocities
                float defaultExtend, defaultRetract;
                if ((ExtendToPositive && velocity > 0) || !ExtendToPositive && velocity < 0)
                {
                    defaultExtend = velocity;
                    defaultRetract = -velocity;
                }
                else
                {
                    defaultExtend = -velocity;
                    defaultRetract = velocity;
                }

                ExtendVelocity = ParseFloat(GetKey(stator, INI_HEAD, "Extend Velocity", defaultExtend.ToString()), defaultExtend);
                RetractVelocity = ParseFloat(GetKey(stator, INI_HEAD, "Retract Velocity", defaultRetract.ToString()), defaultRetract);
            }

            public void Extend()
            {
                Stator.RotorLock = false;
                Stator.TargetVelocityRPM = ExtendVelocity;
            }

            public void Retract()
            {
                Stator.RotorLock = false;
                Stator.TargetVelocityRPM = RetractVelocity;
            }

            public void Stop()
            {
                Stator.TargetVelocityRPM = 0;
                Stator.RotorLock = true;
            }
        }

        public class LandingPiston
        {
            public IMyPistonBase Piston;
            public bool ExtendToPositive;
            public float ExtendVelocity;
            public float RetractVelocity;

            public LandingPiston(IMyPistonBase piston, bool currentlyExtended)
            {
                Piston = piston;

                string defaultBool;
                float velocity = piston.Velocity;

                if ((currentlyExtended && velocity >= 0) || (!currentlyExtended && velocity < 0))
                    defaultBool = "True";
                else
                    defaultBool = "False";

                ExtendToPositive = ParseBool(GetKey(piston, INI_HEAD, "Extend To Positive", defaultBool));

                float defaultExtend, defaultRetract;
                if ((ExtendToPositive && velocity > 0) || !ExtendToPositive && velocity < 0)
                {
                    defaultExtend = velocity;
                    defaultRetract = -velocity;
                }
                else
                {
                    defaultExtend = -velocity;
                    defaultRetract = velocity;
                }

                ExtendVelocity = ParseFloat(GetKey(piston, INI_HEAD, "Extend Velocity", defaultExtend.ToString()), defaultExtend);
                RetractVelocity = ParseFloat(GetKey(piston, INI_HEAD, "Retract Velocity", defaultRetract.ToString()), defaultRetract);
            }

            public void Extend()
            {
                Piston.Velocity = ExtendVelocity;
            }

            public void Retract()
            {
                Piston.Velocity = RetractVelocity;
            }

            public void Stop()
            {
                Piston.Velocity = 0;
            }
        }

        public class LandingPlate
        { }

        public class LandingLight
        { }

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
                                    _landingGear.Pistons.Add(new LandingPiston(block as IMyPistonBase, _landingGear.IsExtended));
                                    break;
                                case "Rotor":
                                case "Advanced Rotor":
                                case "Hinge":
                                case "Hinge 3x3":
                                    _landingGear.Stators.Add(new LandingStator(block as IMyMotorStator, _landingGear.IsExtended));
                                    break;
                                case "Landing Gear":
                                case "Magnetic Plate":
                                case "Large Magnetic Plate":
                                    //AssignLandingPlate(block as IMyLandingGear);
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
                                    //AssignLandingLight(block as IMyLightingBlock);
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
            EnsureKey(landingPlate, INI_HEAD, "Off on Retract", "True");
            EnsureKey(landingPlate, INI_HEAD, "AutoLock on Retract", "False");
            EnsureKey(landingPlate, INI_HEAD, "AutoLock on Extend", "True");
            //_landingGear.LandingPlates.Add(landingPlate);
        }

        void AssignLandingStator(IMyMotorStator stator)
        {
            string defaultBool;
            float velocity = stator.TargetVelocityRPM;

            if ((_landingGear.IsExtended && velocity >= 0) || (!_landingGear.IsExtended && velocity < 0))
                defaultBool = "True";
            else
                defaultBool = "False";

            // Determine whether stator motion is positive or negative in relation to extension/retraction
            bool extendToPositive = ParseBool(GetKey(stator, INI_HEAD, "Extend To Positive", defaultBool));

            // Set default extension and retraction velocities based on current velocities
            float defaultExtend, defaultRetract;
            if ((extendToPositive && velocity > 0) || !extendToPositive && velocity < 0)
            {
                defaultExtend = velocity;
                defaultRetract = -velocity;
            }
            else
            {
                defaultExtend = -velocity;
                defaultRetract = velocity;
            }

            EnsureKey(stator, INI_HEAD, "Extend Velocity", defaultExtend.ToString());
            EnsureKey(stator, INI_HEAD, "Retract Velocity", defaultRetract.ToString());

            //_landingGear.Stators.Add(stator);
        }

        void AssignLandingPiston(IMyPistonBase piston)
        {
            string defaultBool;
            float velocity = piston.Velocity;

            if ((_landingGear.IsExtended && velocity >= 0) || (!_landingGear.IsExtended && velocity < 0))
                defaultBool = "True";
            else
                defaultBool = "False";

            EnsureKey(piston, INI_HEAD, "Extend To Positive", defaultBool);

            //_landingGear.Pistons.Add(piston);
        }

        void AssignLandingLight(IMyLightingBlock light)
        {
            string defaultBool;

            if ((_landingGear.IsExtended && light.IsWorking) || (!_landingGear.IsExtended && !light.IsWorking))
                defaultBool = "True";
            else
                defaultBool = "False";

            EnsureKey(light, INI_HEAD, "On for Extension", defaultBool);

            //_landingGear.Lights.Add(light);
        }

        static void ActivateLandingStator(IMyMotorStator stator, bool extending)
        {
            //TODO
        }

        static void ActivateLandingPiston(IMyPistonBase piston, bool extending)
        {
            //TODO
        }

        static void ActivateLandingPlate(IMyLandingGear landingPlate, bool extending)
        {
            //TODO
        }

        static void ActivateLandingLight(IMyLightingBlock light, bool extending)
        {
            //TODO
        }
    }
}
