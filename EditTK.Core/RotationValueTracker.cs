using EditTK.Core.Timing;
using EditTK.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace EditTK.Core
{
    /// <summary>
    /// A helper class that keeps track of a user changed rotation/angle (in degrees) handling snapping and smoothing (when snapping)
    /// </summary>
    public class RotationValueTracker : ValueTracker
    {
        public RotationValueTracker(double startValue, double snapInterval, TimeTracker timeTracker, SnapAnimType snapSmoothType = SnapAnimType.NONE, Action? onSnapped = null)
            : base(startValue, snapInterval, timeTracker, snapSmoothType, onSnapped)
        {

        }

        public override void Update(double rotationAngleDegrees, bool snap)
        {
            double currentAngle = StartValue + DeltaValue;
            double newAngle = currentAngle + MathUtils.GetShortestDegRotationBetween(currentAngle, rotationAngleDegrees);

            base.Update(newAngle, snap);
        }
    }
}
