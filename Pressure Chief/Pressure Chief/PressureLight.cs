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
		const string BLINK_INTERVAL = "Blink Interval";
		const string BLINK_PERCENT = "Blink Length";

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
			public float BlinkInterval { get; set; }
			public float BlinkLength { get; set; }

			bool LockLight;

			BlockIni Ini;

			public PressureLight(IMyLightingBlock light, bool isAirLock)
			{
				LightBlock = light;
				LockLight = isAirLock;
				Ini = new BlockIni(light, INI_HEAD);

				NormalColor = ColorFromString(GetKey("Normal_Color", light.Color.R.ToString() + "," + light.Color.G.ToString() + "," + light.Color.B.ToString()));
				EmergencyColor = ColorFromString(GetKey("Emergency_Color", "255,0,0"));
				NormalRadius = ParseFloat(GetKey("Normal_Radius", light.Radius.ToString()));
				EmergencyRadius = ParseFloat(GetKey("Emergency_Radius", light.Radius.ToString()));
				NormalIntensity = ParseFloat(GetKey("Normal_Intensity", light.Intensity.ToString()));
				EmergencyIntensity = ParseFloat(GetKey("Emergency_Intensity", "10"));

				if(isAirLock)
                {
					BlinkInterval = ParseFloat(GetKey(BLINK_INTERVAL, "1"));
					BlinkLength = ParseFloat(GetKey(BLINK_PERCENT, "0.5")) * 100;
                }
				else
                {
					// Dummy values for non-airlocks
					BlinkInterval = 0;
					BlinkLength = 0.5f;
                }
			}

			// SET STATE
			public void SetState(bool pressurized)
			{
				if (pressurized)
					SetToNormal();
				else
					SetToEmergency();
			}

			// SET TO NORMAL
			void SetToNormal()
            {
				LightBlock.Color = NormalColor;
				LightBlock.Radius = NormalRadius;
				LightBlock.Intensity = NormalIntensity;
			}

			// SET TO EMERGENCY
			void SetToEmergency()
            {
				LightBlock.Color = EmergencyColor;
				LightBlock.Radius = EmergencyRadius;
				LightBlock.Intensity = EmergencyIntensity;
			}

			// SET BLINK
			public void SetBlink(bool blinkOn)
            {
				if(blinkOn)
                {
					SetToEmergency();
					LightBlock.BlinkIntervalSeconds = BlinkInterval;
					LightBlock.BlinkLength = BlinkLength;
				}
				else
                {
					LightBlock.BlinkIntervalSeconds = 0;
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
