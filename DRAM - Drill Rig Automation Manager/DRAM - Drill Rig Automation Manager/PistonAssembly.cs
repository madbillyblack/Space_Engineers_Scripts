﻿using Sandbox.Game.EntityComponents;
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
        const string HORZ_TAG = "[HORZ]";
        const string VERT_TAG = "[VERT]";
        const string BASE_TAG = "[BASE]";

        public PistonAssembly _BasePistons, _VertPistons, _HorzPistons;

        public class PistonAssembly
        {
            public List<Piston> Pistons;

            public PistonAssembly()
            {
                Pistons = new List<Piston>();
            }

            public double MaxPos()
            { 
                double max = 0;
                foreach (Piston piston in Pistons)
                {
                    if(piston.Base.CurrentPosition > max)
                        max = piston.Base.CurrentPosition;
                }

                return max;
            }

            public double MinPos()
            {
                double min = 10;

                foreach (Piston piston in Pistons)
                {
                    if (piston.Base.CurrentPosition < min)
                        min = piston.Base.CurrentPosition;
                }

                return min;
            }
        }

        // PISTON // - Wrapper Class for IMyPistonBase
        public class Piston
        {
            public IMyPistonBase Base;
            MyIni Ini;

            public Piston(IMyPistonBase piston)
            {
                Base = piston;
                Ini = GetIni(Base);

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

        public void AddPistons()
        {
            List<IMyPistonBase> pistons = new List<IMyPistonBase>();
            GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(pistons);

            if (pistons.Count < 1)
                return;

            foreach (IMyPistonBase piston in pistons)
            {
                if (SameGridID(piston))
                {
                    string name = piston.CustomName;

                    if (name.Contains(HORZ_TAG))
                    {
                        _HorzPistons.Pistons.Add(new Piston(piston));
                    }
                    else if (name.Contains(VERT_TAG))
                    {
                        _VertPistons.Pistons.Add(new Piston(piston));
                    }
                    else if (name.Contains(BASE_TAG))
                    {
                        _BasePistons.Pistons.Add(new Piston(piston));
                    }
                }
            }
        }
    }
}
