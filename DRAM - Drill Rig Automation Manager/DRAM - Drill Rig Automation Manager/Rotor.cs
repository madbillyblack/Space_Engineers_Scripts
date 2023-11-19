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
        public const string VELOCITY = "Velocity";
        public const string ON = "OnOff_On";
        public const string OFF = "OnOff_Off";

        public RotorAssembly _rotors;

        public class RotorAssembly
        {
            public List<Rotor> Rotors;

            public RotorAssembly()
            {
                Rotors = new List<Rotor>();
            }

            public void Reverse()
            {
                if (Rotors.Count < 1) return;

                foreach(Rotor rotor in Rotors)
                    rotor.Reverse();
            }

            public void StartRotors()
            {
                if (Rotors.Count < 1) return;

                foreach (Rotor rotor in Rotors)
                    rotor.StartRotor();
            }

            public void StopRotors()
            {
                if (Rotors.Count < 1) return;

                foreach (Rotor rotor in Rotors)
                    rotor.StopRotor();
            }
        }
        

        public class Rotor
        {
            public IMyMotorAdvancedStator Base;
            MyIni Ini;
            float velocity;

            public Rotor(IMyMotorAdvancedStator rotorBase)
            {
                Base = rotorBase;
                Ini = GetIni(Base);
                velocity = (float) GetKey(VELOCITY, ROTOR_SPEED);
            }

            public void SetKey(string key, double value)
            {
                Ini.Set(MAIN_TAG, key, value);
                Base.CustomData = Ini.ToString();
            }

            public double GetKey(string key, double defaultValue)
            {
                if (!Ini.ContainsKey(MAIN_TAG, key))
                {
                    SetKey(key, defaultValue);
                    return defaultValue;
                }

                return Ini.Get(MAIN_TAG, key).ToDouble();
            }

            public void Reverse()
            {
                velocity *= -1;
                Base.TargetVelocityRPM = velocity;
                SetKey(VELOCITY, velocity);
            }

            public void StopRotor()
            {
                Base.TargetVelocityRPM = 0;
                Base.GetActionWithName(OFF).Apply(Base);
                Base.RotorLock = true;
            }

            public void StartRotor()
            {
                Base.TargetVelocityRPM = velocity;
                Base.RotorLock = false;
                Base.GetActionWithName(ON).Apply(Base);
            }
        }


        public void AddRotors()
        {
            _rotors = new RotorAssembly();

            List<IMyMotorAdvancedStator> rotors = new List<IMyMotorAdvancedStator>();
            GridTerminalSystem.GetBlocksOfType<IMyMotorAdvancedStator>(rotors);

            if (rotors.Count < 1 )
            {
                _statusMessage += "No Rotors Found!\n";
                return;
            }

            foreach (IMyMotorAdvancedStator rotor in rotors )
            {
                if(SameGridID(rotor))
                    _rotors.Rotors.Add(new Rotor(rotor));
            }
        }
    }
}
