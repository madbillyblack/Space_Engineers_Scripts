using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        public class PrintHinge
        {
            public IMyMotorStator Hinge { get; set; }
            public MyIniHandler Ini { get; set; }

            public float Max { get; set; }
            public float Min { get; set; }
            public float Speed { get; set; }

            public PrintHinge(IMyMotorStator hinge)
            {
                Hinge = hinge;
                Ini = new MyIniHandler(hinge);

                SetMaxLimit();
                SetMinLimit();
                SetSpeed();
            }

            private void SetMaxLimit()
            {
                if (Ini.HasKey(MAIN_HEADER, MAX_KEY))
                {
                    Max = ParseFloat(Ini.GetKey(MAIN_HEADER, MAX_KEY, "90"), 90);
                }
                else
                {
                    Max = Hinge.UpperLimitDeg;
                    Ini.SetKey(MAIN_HEADER, MAX_KEY, Max.ToString());
                }
            }


            private void SetMinLimit()
            {
                if (Ini.HasKey(MAIN_HEADER, MIN_KEY))
                {
                    Min = ParseFloat(Ini.GetKey(MAIN_HEADER, MIN_KEY, "0"), 0);
                }
                else
                {
                    Min = Hinge.LowerLimitDeg;
                    Ini.SetKey(MAIN_HEADER, MIN_KEY, Min.ToString());
                }
            }


            private void SetSpeed()
            {
                if (Ini.HasKey(MAIN_HEADER, SPD_KEY))
                {
                    Speed = ParseFloat(Ini.GetKey(MAIN_HEADER, SPD_KEY, "0.5"), 0.5f);
                }
                else
                {
                    Speed = Math.Abs(Hinge.TargetVelocityRPM);
                    if (Speed == 0)
                        Speed = 0.5f;
                    Ini.SetKey(MAIN_HEADER, SPD_KEY, Speed.ToString());
                }
            }
        }
    }
}
