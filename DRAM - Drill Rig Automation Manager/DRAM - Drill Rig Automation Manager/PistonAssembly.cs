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
        const string HORZ_TAG = "[HORZ]";
        const string VERT_TAG = "[VERT]";
        const string BASE_TAG = "[BASE]";
        const string MIN = "Min Limit";
        const string MAX = "Max Limit";

        const float H_STEP = 1;
        const float V_STEP = 1;
        const float B_START = 9;
        const float PISTON_SPEED = 1;

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

                if(Pistons.Count > 0)
                {
                    foreach (Piston piston in Pistons)
                    {
                        if (piston.Base.CurrentPosition > max)
                            max = piston.Base.CurrentPosition;
                    }
                }


                return max;
            }

            public double MinPos()
            {
                double min = 10;

                if(Pistons.Count > 0)
                {
                    foreach (Piston piston in Pistons)
                    {
                        if (piston.Base.CurrentPosition < min)
                            min = piston.Base.CurrentPosition;

                    }
                }

                return min;
            }

            public void AdjustMinimum(float min)
            {
                if(Pistons.Count < 1) return;

                foreach (Piston piston in Pistons)
                {
                    piston.Base.MinLimit += min;
                    piston.SetKey(MIN, piston.Base.MinLimit);
                }

            }

            public void SetMinimum(float min)
            {
                if (Pistons.Count < 1) return;

                foreach (Piston piston in Pistons)
                {
                    piston.SetKey(MIN, min);
                    piston.Base.MinLimit = min;
                }

            }

            public void AdjustMaximum(float max)
            {
                if (Pistons.Count < 1) return;

                foreach (Piston piston in Pistons)
                {
                    piston.Base.MaxLimit += max;
                    piston.SetKey(MAX, piston.Base.MaxLimit);
                }
                    
            }

            public void SetMaximum(float max)
            {
                if (Pistons.Count < 1) return;

                foreach (Piston piston in Pistons)
                {
                    piston.SetKey(MAX, max);
                    piston.Base.MaxLimit = max;
                }
                    
            }

            public void SetVelocity(float value)
            {
                if (Pistons.Count < 1) return;

                foreach (Piston piston in Pistons)
                    piston.Base.Velocity = value;
            }
        }

        // PISTON // - Wrapper Class for IMyPistonBase
        public class Piston
        {
            public IMyPistonBase Base;
            MyIni Ini;

            public Piston(IMyPistonBase piston, float min, float max)
            {
                Base = piston;
                Ini = GetIni(Base);

                Base.MinLimit = (float) GetKey(MIN, min);
                Base.MaxLimit = (float) GetKey(MAX, max);
            }

            public void SetKey(string key, double value)
            {
                Ini.Set(MAIN_HEADER, key, value);
                Base.CustomData = Ini.ToString();
            }

            public double GetKey(string key, double defaultValue)
            {
                if (!Ini.ContainsKey(MAIN_HEADER, key))
                {
                    SetKey(key, defaultValue);
                    return defaultValue;
                }
                 
                return Ini.Get(MAIN_HEADER, key).ToDouble();
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
                        _HorzPistons.Pistons.Add(new Piston(piston, 0, 0));
                    }
                    else if (name.Contains(VERT_TAG))
                    {
                        _VertPistons.Pistons.Add(new Piston(piston, 0, 0));
                    }
                    else if (name.Contains(BASE_TAG))
                    {
                        _BasePistons.Pistons.Add(new Piston(piston, _baseStart, _baseStart + 0.5f));
                    }
                }
            }

            _vertCount = _VertPistons.Pistons.Count();
            _horzCount = _HorzPistons.Pistons.Count();
            _baseCount = _BasePistons.Pistons.Count();
        }
    }
}
