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
        public class PID
        {
            public double Kp { get; set; } = 0;
            public double Ki { get; set; } = 0;
            public double Kd { get; set; } = 0;
            public double Value { get; private set; }

            double _timeStep = 0;
            double _inverseTimeStep = 0;
            double _errorSum = 0;
            double _lastError = 0;
            bool _firstRun = true;

            public PID(double kp, double ki, double kd, double timeStep)
            {
                Kp = kp;
                Ki = ki;
                Kd = kd;
                _timeStep = timeStep;
                _inverseTimeStep = 1 / _timeStep;
            }

            protected virtual double GetIntegral(double currentError, double errorSum, double timeStep)
            {
                return errorSum + currentError * timeStep;
            }

            public double Control(double error)
            {
                //Compute derivative term
                double errorDerivative = (error - _lastError) * _inverseTimeStep;

                if(_firstRun)
                {
                    errorDerivative = 0;
                    _firstRun = false;
                }

                //Get error sum
                _errorSum = GetIntegral(error, _errorSum, _timeStep);

                //Store this error as last error
                _lastError = error;

                //Constuct output
                Value = Kp * error + Ki * _errorSum + Kd * errorDerivative;
                return Value;
            }

            public Double Control(double error, double timeStep)
            {
                if(timeStep != _timeStep)
                {
                    _timeStep = timeStep;
                    _inverseTimeStep = 1 / _timeStep;
                }

                return Control(error);
            }

            public virtual void Reset()
            {
                _errorSum = 0;
                _lastError = 0;
                _firstRun = true;
            }
        }

        public class DecayingIntegralPID : PID
        {
            public double IntegralDecayRatio { get; set; }

            public DecayingIntegralPID(double kp, double ki, double kd, double timeStep, double decayRatio) : base(kp,ki,kd, timeStep)
            {
                IntegralDecayRatio = decayRatio;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep)
            {
                return errorSum * (1.0 - IntegralDecayRatio) + currentError * timeStep;
            }
        }

        public class ClampedIntegralPID: PID
        {
            public double IntegralUpperBound { get; set; }
            public double IntegralLowerBound { get; set; }

            public ClampedIntegralPID(double kp, double ki, double kd, double timeStep, double lowerBound, double upperBound): base(kp,ki,kd,timeStep)
            {
                IntegralUpperBound = upperBound;
                IntegralLowerBound = lowerBound;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep)
            {
                errorSum = errorSum + currentError * timeStep;
                return Math.Min(IntegralUpperBound, Math.Max(errorSum, IntegralLowerBound));
            }
        }

        public class BufferedIntegralPID : PID
        {
            readonly Queue<double> _integralBuffer = new Queue<double>();
            public int IntegralBufferSize { get; set; } = 0;

            public BufferedIntegralPID(double kp, double ki, double kd, double timeStep, int bufferSize) : base(kp, ki, kd, timeStep)
            {
                IntegralBufferSize = bufferSize;
            }

            protected override double GetIntegral(double currentError, double errorSum, double timeStep)
            {
                if (_integralBuffer.Count == IntegralBufferSize)
                    _integralBuffer.Dequeue();
                _integralBuffer.Enqueue(currentError * timeStep);
                return _integralBuffer.Sum();
            }

            public override void Reset()
            {
                base.Reset();
                _integralBuffer.Clear();
            }
        }


        // SET GAINS FROM STRING //
        public void SetGainsFromString(string gains)
        {
            string[] gainArgs = gains.Split(',');
            if (gainArgs.Length != 3)
            {
                _statusMessage += "Invalid GAIN argument!\n Follow format kP,kI,kD";
                return;
            }

            _kP = ParseFloat(gainArgs[0].Trim(), (float) _kP);
            _kI = ParseFloat(gainArgs[1].Trim(), (float) _kI);
            _kD = ParseFloat(gainArgs[2].Trim(), (float) _kD);

            SetMainKey(HEADER, P_KEY, _kP.ToString("0.####"));
            SetMainKey(HEADER, I_KEY, _kI.ToString("0.####"));
            SetMainKey(HEADER, D_KEY, _kD.ToString("0.####"));

            _pid = new PID(_kP, _kI, _kD, TIME_STEP);
        }


        // ADJUST GAIN //
        public double AdjustGain(double gain, string adjustment)
        {
            double mod;
            if (adjustment == "")
                mod = CALIBRATION;
            else mod = ParseFloat(adjustment, 0);

            double newGain = gain + mod;

            if (newGain < 0)
                return 0;

            return newGain;
        }


        // ADJUST P //
        public void AdjustP(string adjustment)
        {
            _kP = AdjustGain(_kP, adjustment);
            _pid = new PID(_kP,_kI, _kD, TIME_STEP);
        }


        // ADJUST I //
        public void AdjustI(string adjustment)
        {
            _kI = AdjustGain(_kI, adjustment);
            _pid = new PID(_kP, _kI, _kD, TIME_STEP);
        }


        // ADJUST D //
        public void AdjustD(string adjustment)
        {
            _kD = AdjustGain(_kD, adjustment);
            _pid = new PID(_kP, _kI, _kD, TIME_STEP);
        }
    }
}
