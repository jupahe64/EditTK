using EditTK.Core.Timing;
using EditTK.Core.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EditTK.Core
{
    public enum SnapAnimType
    {
        NONE,
        ELASTIC,
        EASING
    }

    /// <summary>
    /// A helper class that keeps track of a user changed value handling snapping and smoothing (when snapping)
    /// </summary>
    public class ValueTracker
    {
        public double StartValue { get; private set; }
        public double DeltaValue { get; private set; }
        public double AnimDeltaValue { get; private set; }

        public double Value => StartValue + DeltaValue;
        public double SmoothValue => StartValue + AnimDeltaValue;

        public double SnapInterval { get; set; }

        private readonly float _easingFactor = 1f;
        private readonly RegisteredIntervallHandler _registeredHandler;
        private bool _previousSnap;

        private readonly Action? _onSnapped;

        public ValueTracker(double startValue, double snapInterval, TimeTracker timeTracker, SnapAnimType snapSmoothType = SnapAnimType.NONE, Action? onSnapped = null)
        {
            StartValue = startValue;
            SnapInterval = snapInterval;

            _easingFactor = snapSmoothType switch
            {
                SnapAnimType.NONE => 1f,
                SnapAnimType.ELASTIC => 1.1f,
                SnapAnimType.EASING => 0.9f,
                _ => 1f
            };

            _registeredHandler = timeTracker.AddIntervallHandler(Animate, 1 / 60.0);

            _previousSnap = false;
            _onSnapped = onSnapped;
        }

        ~ValueTracker()
        {
            _registeredHandler.Unregister();
        }

        private void Animate(int framesPassed)
        {
            AnimDeltaValue = MathUtils.Mix(AnimDeltaValue, DeltaValue, 1 - Math.Pow(1 - _easingFactor, framesPassed));
        }

        public virtual void Update(double newValue, bool snap)
        {
            double newDelta = newValue - StartValue;

            if (snap)
                newDelta = MathUtils.Round(DeltaValue, SnapInterval);

            if ((snap || _previousSnap) && DeltaValue != newDelta)
                _onSnapped?.Invoke();

            DeltaValue = newDelta;

            _previousSnap = snap;
        }
    }
}
