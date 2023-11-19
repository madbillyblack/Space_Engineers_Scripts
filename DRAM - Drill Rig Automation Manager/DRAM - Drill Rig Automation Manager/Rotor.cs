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
        List<Rotor> _rotors;

        public class Rotor
        {
            public IMyMotorAdvancedStator Base;
            MyIni Ini;

            public Rotor(IMyMotorAdvancedStator rotorBase)
            {
                Base = rotorBase;
                Ini = GetIni(Base as IMyTerminalBlock);
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
        }


        public void AddRotors()
        {
            _rotors = new List<Rotor>();

            List<IMyMotorAdvancedStator> rotors = new List<IMyMotorAdvancedStator>();
            GridTerminalSystem.GetBlocksOfType< IMyMotorAdvancedStator>(rotors);

            if (rotors.Count < 1 )
            {
                _statusMessage += "No Rotors Found!\n";
                return;
            }

            foreach (IMyMotorAdvancedStator rotor in rotors )
            {
                if(SameGridID(rotor as IMyTerminalBlock))
                    _rotors.Add(new Rotor(rotor));
            }
        }
    }
}
