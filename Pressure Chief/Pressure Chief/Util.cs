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
		// PARSE BOOL //
		public static bool ParseBool(string val)
		{
			string uVal = val.ToUpper();
			if (uVal == "TRUE" || uVal == "T" || uVal == "1")
			{
				return true;
			}

			return false;
		}


		// COLOR FROM STRING // Returns color based on comma separated RGB value.
		public static Color ColorFromString(string rgb)
		{
			string[] values = rgb.Split(',');
			if (values.Length < 3)
				return Color.Black;

			byte[] outputs = new byte[3];
			for (int i = 0; i < 3; i++)
			{
				bool success = byte.TryParse(values[i], out outputs[i]);
				if (!success)
					outputs[i] = 0;
			}

			return new Color(outputs[0], outputs[1], outputs[2]);
		}


		// PARSE FLOAT //
		public static float ParseFloat(string numberString)
		{
			float number;

			if (Single.TryParse(numberString, out number))
				return number;
			else
				return 0;
		}


		// PARSE INT //
		public static int ParseUInt(string value)
		{
			UInt32 number;
			try
			{
				number = UInt32.Parse(value);
			}
			catch
			{
				number = 0;
			}

			return (int)number;
		}
    }
}
