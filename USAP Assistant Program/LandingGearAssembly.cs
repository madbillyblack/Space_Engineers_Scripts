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
            public List<IMyPistonBase> Pistons;
            public List<IMyMotorStator> Stators;
            public List<IMyLandingGear> LandingPlates;
            public List<IMyShipConnector> Connectors;
            public List<IMyLightingBlock> Lights;

            public IMyTimerBlock Timer;
            public bool IsExtended;

            public LandingGearAssembly(IMyTimerBlock timer)
            {
                Pistons = new List<IMyPistonBase>();
                Stators = new List<IMyMotorStator>();
                LandingPlates = new List<IMyLandingGear>();
                Connectors = new List<IMyShipConnector>();
                Lights = new List<IMyLightingBlock>();

                Timer = timer;
                IsExtended = ParseBool(GetKey(timer, INI_HEAD, "Extended", "True"));
                EnsureKey(timer, INI_HEAD, "Extension Delay", timer.TriggerDelay.ToString());
                EnsureKey(timer, INI_HEAD, "Retraction Delay", timer.TriggerDelay.ToString());
            }


            // EXTEND //
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


            // ACTIVATE //
            public void Activate(bool extending)
            {
                IsExtended = extending;

                if (LandingPlates.Count > 0)
                    foreach (IMyLandingGear landingPlate in LandingPlates)
                        DisengageLandingPlate(landingPlate);

                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        EngagePiston(piston, extending);

                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        EngageStator(stator, extending);

                if (Lights.Count > 0)
                    foreach (IMyLightingBlock light in Lights)
                        ActivateLandingLight(light, extending);

                float delay;
                if (extending)
                    delay = ParseFloat(GetKey(Timer, INI_HEAD, "Extension Delay", Timer.TriggerDelay.ToString()), Timer.TriggerDelay);
                else
                    delay = ParseFloat(GetKey(Timer, INI_HEAD, "Retraction Delay", Timer.TriggerDelay.ToString()), Timer.TriggerDelay);

                Timer.TriggerDelay = delay;
                Timer.StartCountdown();
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
                // Deactivate pistons
                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        DisengagePiston(piston);

                // Deactivate rotors and hinges
                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        DisengageStator(stator);

                // Set Mag Plates to AutoLock or Off
                if (LandingPlates.Count > 0)
                    foreach (IMyLandingGear landingPlate in LandingPlates)
                        EngageLandingPlate(landingPlate);
            }

            public void SwapDirections()
            {
                IsExtended = !IsExtended;

                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        SwapVelocities(piston);

                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        SwapVelocities(stator);
            }

            // CLEAR DATA // - Temp
            public void ClearData()
            {
                Timer.CustomData = "";

                if (Pistons.Count > 0)
                    foreach (IMyPistonBase piston in Pistons)
                        piston.CustomData = "";

                if (Stators.Count > 0)
                    foreach (IMyMotorStator stator in Stators)
                        stator.CustomData = "";
            }
        }
        /*
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
                {
                    public IMyLandingGear LandingGear;
                    public int RetractCase;

                    public LandingPlate(IMyLandingGear landingGear)
                    {
                        LandingGear = landingGear;
                        string onRetract = GetKey(landingGear, INI_HEAD, "On Retract", "AutoLock");

                        switch(onRetract.ToUpper())
                        {
                            case "OFF":
                            case "TURNOFF":
                            case "TURN OFF":
                                RetractCase = 2;
                                break;
                            case "AUTOLOCK":
                            case "AUTO LOCK":
                            case "AUTO_LOCK":
                            case "AUTO-LOCK":
                                RetractCase = 1;
                                break;
                            default:
                                RetractCase = 0;
                                break;
                        }
                    }
                }

                public class LandingLight
                { }
        */
        // INIT FUNCTIONS ---------------------------------------------------------------------------------------------------------------------------------------

        // ASSEMBLE LANDING GEAR //
        void AssembleLandingGear()
        {
            _landingGear = null;

            List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(timers);

            if (timers.Count < 1)
                return;

            foreach (IMyTimerBlock timer in timers)
            {
                if (timer.CustomName.Contains(GEAR_TAG) && GetKey(timer, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString()) == _gridID)
                {
                    _landingGear = new LandingGearAssembly(timer);

                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.SearchBlocksOfName(GEAR_TAG, blocks);

                    foreach (IMyTerminalBlock block in blocks)
                    {
                        if (GetKey(block, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString()) == _gridID)
                        {
                            switch (block.DefinitionDisplayNameText)
                            {
                                case "Piston":
                                    AssignLandingPiston(block as IMyPistonBase);
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


        // ASSIGN LANDING PLATE //
        void AssignLandingPlate(IMyLandingGear landingPlate)
        {
            EnsureKey(landingPlate, INI_HEAD, "On Retract", "AutoLock");
            EnsureKey(landingPlate, INI_HEAD, "On Extend", "AutoLock");
            _landingGear.LandingPlates.Add(landingPlate);
        }


        // ASSIGN LANDING STATOR //
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

            EnsureKey(stator, INI_HEAD, "Extend Velocity", defaultExtend.ToString("0.00"));
            EnsureKey(stator, INI_HEAD, "Retract Velocity", defaultRetract.ToString("0.00"));
            EnsureKey(stator, INI_HEAD, "Off When Stationary", "false");

            _landingGear.Stators.Add(stator);
        }


        // ASSIGN LANDING PISTON //
        void AssignLandingPiston(IMyPistonBase piston)
        {
            string defaultBool;
            float velocity = piston.Velocity;

            if ((_landingGear.IsExtended && velocity >= 0) || (!_landingGear.IsExtended && velocity < 0))
                defaultBool = "True";
            else
                defaultBool = "False";

            // Determine if extension corresponds to positive velocity
            bool extendToPositive = ParseBool(GetKey(piston, INI_HEAD, "Extend To Positive", defaultBool));

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

            EnsureKey(piston, INI_HEAD, "Extend Velocity", defaultExtend.ToString("0.00"));
            EnsureKey(piston, INI_HEAD, "Retract Velocity", defaultRetract.ToString("0.00"));
            EnsureKey(piston, INI_HEAD, "Off When Stationary", "false");

            _landingGear.Pistons.Add(piston);
        }


        // ASSIGN LANDING LIGHT //
        void AssignLandingLight(IMyLightingBlock light)
        {
            string defaultExtended, defaultRetracted;
            string currentColor = light.Color.R + "," + light.Color.G + "," + light.Color.B;

            if ((_landingGear.IsExtended && light.IsWorking) || (!_landingGear.IsExtended && !light.IsWorking))
            {
                defaultExtended = currentColor;
                defaultRetracted = "0,0,0";
            }
            else
            {
                defaultExtended = "0,0,0";
                defaultRetracted = currentColor;
            }

            EnsureKey(light, INI_HEAD, "Color on Extend", defaultExtended);
            EnsureKey(light, INI_HEAD, "Color on Retract", defaultRetracted);

            _landingGear.Lights.Add(light);
        }


        // ACTIVATION FUNCTIONS ---------------------------------------------------------------------------------------------------------------------------------

        // ENGAGE STATOR // - For use when assembly is in motion
        static void EngageStator(IMyMotorStator stator, bool extending)
        {
            stator.GetActionWithName("OnOff_On").Apply(stator);
            stator.RotorLock = false;

            if (_landingGear.IsExtended)
                stator.TargetVelocityRPM = ParseFloat(GetKey(stator, INI_HEAD, "Extend Velocity", "0"), 0);
            else
                stator.TargetVelocityRPM = ParseFloat(GetKey(stator, INI_HEAD, "Retract Velocity", "0"), 0);
        }


        // DISENGAGE STATOR // - For use when assembly is at rest
        static void DisengageStator(IMyMotorStator stator)
        {
            stator.TargetVelocityRPM = 0;

            if (ParseBool(GetKey(stator, INI_HEAD, "Off When Stationary", "false")))
                stator.GetActionWithName("OnOff_Off").Apply(stator);
            else
                stator.RotorLock = true;
        }


        // ENGAGE PISTON //
        static void EngagePiston(IMyPistonBase piston, bool extending)
        {
            piston.GetActionWithName("OnOff_On").Apply(piston);

            if (extending)
                piston.Velocity = ParseFloat(GetKey(piston, INI_HEAD, "Extend Velocity", "0"), 0);
            else
                piston.Velocity = ParseFloat(GetKey(piston, INI_HEAD, "Retract Velocity", "0"), 0);
        }


        // DISENGAGE PISTON //
        static void DisengagePiston(IMyPistonBase piston)
        {
            piston.Velocity = 0;

            if (ParseBool(GetKey(piston, INI_HEAD, "Off When Stationary", "false")))
                piston.GetActionWithName("OnOff_Off").Apply(piston);
        }


        // ACTIVATE LANDING LIGHT //
        static void ActivateLandingLight(IMyLightingBlock light, bool extending)
        {
            if (extending)
                light.Color = ParseColor(GetKey(light, INI_HEAD, "Color on Extend", "255,0,127"));
            else
                light.Color = ParseColor(GetKey(light, INI_HEAD, "Color on Retract", "0,0,0"));
        }


        // DISENGAGE LANDING PLATE // - For use when gear assembly is in motion (extending or retracting)
        static void DisengageLandingPlate(IMyLandingGear landingGear)
        {
            landingGear.GetActionWithName("OnOff_On").Apply(landingGear);
            landingGear.Unlock();
            landingGear.AutoLock = false;
        }


        // ENGAGE LANDING PLATE // - For use when gear assembly is at rest (extended or retracted)
        static void EngageLandingPlate(IMyLandingGear landingGear)
        {
            if (_landingGear.IsExtended)
            {
                string onExtend = GetKey(landingGear, INI_HEAD, "On Extend", "AutoLock").ToUpper();

                // Check if string is any reasonable permutation of "AutoLock"
                switch (onExtend)
                {
                    case "AUTOLOCK":
                    case "AUTO LOCK":
                    case "AUTO-LOCK":
                    case "AUTO_LOCK":
                        landingGear.AutoLock = true;
                        break;
                }
            }
            else if (!_landingGear.IsExtended)
            {
                string onRetract = GetKey(landingGear, INI_HEAD, "On Retract", "AutoLock").ToUpper();

                switch (onRetract)
                {
                    case "AUTOLOCK":
                    case "AUTO LOCK":
                    case "AUTO-LOCK":
                    case "AUTO_LOCK":
                        landingGear.AutoLock = true;
                        break;
                    case "OFF":
                    case "TURNOFF":
                    case "TURN OFF":
                        landingGear.GetActionWithName("OnOff_Off").Apply(landingGear);
                        break;
                }
            }
        }


        // SET RETRACT BEHAVIOR //
        void SetRetractBehavior(string behavior)
        {
            if (_landingGear == null || _landingGear.LandingPlates.Count < 1)
                return;

            foreach (IMyLandingGear landingPlate in _landingGear.LandingPlates)
                SetKey(landingPlate, INI_HEAD, "On Retract", behavior);
        }


        // SET VELOCITY //
        static void SetVelocity(IMyPistonBase piston)
        {
            if (_landingGear == null)
                return;

            float velocity = piston.Velocity;

            if (_landingGear.IsExtended)
                SetKey(piston, INI_HEAD, "Extend Velocity", velocity.ToString("0.00"));
            else
                SetKey(piston, INI_HEAD, "Retract Velocity", velocity.ToString("0.00"));
        }
            
        static void SetVelocity(IMyMotorStator stator)
        {
            if (_landingGear == null)
                return;

            float velocity = stator.TargetVelocityRPM;

            if (_landingGear.IsExtended)
                SetKey(stator, INI_HEAD, "Extend Velocity", velocity.ToString("0.00"));
            else
                SetKey(stator, INI_HEAD, "Retract Velocity", velocity.ToString("0.00"));
        }

        // SWAP VELOCITIES //
        static void SwapVelocities(IMyTerminalBlock block)
        {
            bool extendToPositive = !(ParseBool(GetKey(block, INI_HEAD, "Extend To Positive", "")));
            SetKey(block, INI_HEAD, "Extend To Positive", extendToPositive.ToString());

            string extendVelocity = GetKey(block, INI_HEAD, "Extend Velocity", "");
            string retractVelocity = GetKey(block, INI_HEAD, "Retract Velocity", "");

            if (extendVelocity != "")
                SetKey(block, INI_HEAD, "Retract Velocity", extendVelocity);

            if (retractVelocity != "")
                SetKey(block, INI_HEAD, "Extend Velocity", retractVelocity);
        }


       /* // SET CURRENT POSITION // - Sets velocities for all stators and pistons to their current configurations in the specified position (extended/retracted)
        static void SetCurrentPosition(bool toExtend)
        {
            if (_landingGear == null)
                return;

            if (toExtend)
                _landingGear.IsExtended = true;
            else
                _landingGear.IsExtended = false;

            if (_landingGear.Pistons.Count > 0)
                foreach (IMyPistonBase piston in _landingGear.Pistons)
                    SetVelocity(piston);

            if (_landingGear.Stators.Count > 0)
                foreach (IMyMotorStator stator in _landingGear.Stators)
                    SetVelocity(stator);
        }*/
    }
}
