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


        // GET BRACED INFO //
        static string GetBracedInfo(string arg)
        {
            string info = "";

            if (arg.Contains("{") && arg.Contains("}"))
            {
                int open = arg.IndexOf('{');
                int close = arg.IndexOf('}') - 1;


                if (open < close)
                {
                    info = arg.Substring(open + 1, close - open).Trim();
                }
            }

            return info;
        }


        // TO RADIANS //  Converts Degree Value to Radians
        static double ToRadians(int angle)
        {
            double radianValue = (double)angle * Math.PI / 180;
            return radianValue;
        }


        // TO DEGREES //
        static float ToDegrees(float angle)
        {
            float degreeValue = angle * 180 / (float)Math.PI;
            return degreeValue;
        }


        static float ToHalfCircle(float degrees)
        {
            if (degrees >= -180 && degrees <= 180)
                return degrees;
            else if (degrees > 180)
                return degrees - 360;
            else
                return degrees + 360;
        }
    }
}
