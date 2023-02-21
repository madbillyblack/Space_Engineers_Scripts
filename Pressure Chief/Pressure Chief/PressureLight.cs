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
		// PRESSURE LIGHT // Wrapper class for lights
		public class PressureLight
		{
			public IMyLightingBlock LightBlock { get; set; }
			public Color NormalColor { get; set; }
			public Color EmergencyColor { get; set; }
			public float NormalRadius { get; set; }
			public float EmergencyRadius { get; set; }
			public float NormalIntensity { get; set; }
			public float EmergencyIntensity { get; set; }

			BlockIni Ini;

			public PressureLight(IMyLightingBlock light)
			{
				LightBlock = light;
				Ini = new BlockIni(light, INI_HEAD);

				NormalColor = ColorFromString(GetKey("Normal_Color", light.Color.R.ToString() + "," + light.Color.G.ToString() + "," + light.Color.B.ToString()));
				EmergencyColor = ColorFromString(GetKey("Emergency_Color", "255,0,0"));
				NormalRadius = float.Parse(GetKey("Normal_Radius", light.Radius.ToString()));
				EmergencyRadius = float.Parse(GetKey("Emergency_Radius", light.Radius.ToString()));
				NormalIntensity = float.Parse(GetKey("Normal_Intensity", light.Intensity.ToString()));
				EmergencyIntensity = float.Parse(GetKey("Emergency_Intensity", "10"));
			}

			public void SetState(bool pressurized)
			{
				if (pressurized)
				{
					LightBlock.Color = NormalColor;
					LightBlock.Radius = NormalRadius;
					LightBlock.Intensity = NormalIntensity;
				}
				else
				{
					LightBlock.Color = EmergencyColor;
					LightBlock.Radius = EmergencyRadius;
					LightBlock.Intensity = EmergencyIntensity;
				}
			}

			// GET KEY
			public string GetKey(string key, string defaultValue)
            {
				return Ini.GetKey(key, defaultValue);
			}

			// SET KEY
			public void SetKey(string key, string value)
            {
				Ini.SetKey(key, value);
            }
		}
	}
}
