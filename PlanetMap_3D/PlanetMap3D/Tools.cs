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
        // PARSE INT //
        static int ParseInt(string arg, int defaultValue)
        {
            int number;
            if (int.TryParse(arg, out number))
                return number;
            else
                return defaultValue;
        }


        // PARSE FLOAT //
        static float ParseFloat(string arg, float defaultValue)
        {
            float number;
            if (float.TryParse(arg, out number))
                return number;
            else
                return defaultValue;
        }


        // PARSE BOOL //
        static bool ParseBool(string val)
        {
            string uVal = val.ToUpper();
            if (uVal == "TRUE" || uVal == "T" || uVal == "1")
            {
                return true;
            }

            return false;
        }

        // PARSE COLOR //
        static Color ParseColor(string colorString)
        {
            UInt16 red, green, blue;
            red = green = blue = 0;

            string[] values = colorString.Split(',');
            if (values.Length > 2)
            {
                UInt16.TryParse(values[0], out red);
                UInt16.TryParse(values[1], out green);
                UInt16.TryParse(values[2], out blue);
            }

            return new Color(red, green, blue);
        }
    }
}
