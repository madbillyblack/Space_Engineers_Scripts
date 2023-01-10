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
        // HAS SURFACES //
        static int GetSurfaceCount(IMyTerminalBlock block)
        {
            try
            {
                return (block as IMyTextSurfaceProvider).SurfaceCount;
            }
            catch
            {
                return 0;
            }
            
        }

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


        // TO RADIANS //  Converts Degree Value to Radians
        public static double ToRadians(int angle)
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


        // DEGREE ADD //	Adds two degree angles.	 Sets Rollover at +/- 180°
        public static int DegreeAdd(int angle_A, int angle_B)
        {
            int angleOut = angle_A + angle_B;

            if (angleOut > 180)
            {
                angleOut -= 360;
            }
            else if (angleOut < -179)
            {
                angleOut += 360;
            }

            return angleOut;
        }


        // REDUCE VECTOR // Reduces all parameters of Vector3I to be within -/+ 180°.
        static Vector3I ReduceVector(Vector3I vector)
        {
            int x = ReduceAngle(vector.X);
            int y = ReduceAngle(vector.Y);
            int z = ReduceAngle(vector.Z);

            return new Vector3I(x, y, z);
        }



        // REDUCE ANGLE // - Reduces angles to within be within -/+ 180°.
        static int ReduceAngle(int angle)
        {
            int output = angle;

            while (output < -180)
                output += 360;

            while (output > 180)
                output -= 360;

            return output;
        }


        static Vector3I VectorToDegrees(Vector3 vector)
        {
            float x = ToDegrees(vector.X);
            float y = ToDegrees(vector.Y);
            float z = ToDegrees(vector.Z);

            return ReduceVector(new Vector3I((int) x, (int) y, (int) z));
        }
    }
}
