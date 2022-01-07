using System;
using System.Collections.Generic;
using System.Text;

namespace EditTK.Core.Timing
{
    public class GlobalTimeTracker : TimeTracker
    {
        bool _alreadyInControl = false;
        readonly List<Timeline> _timelines = new List<Timeline>();


        public void AddTimeline(Timeline timeline)
        {
            _timelines.Add(timeline);
        }

        public void RemoveTimeline(Timeline timeline)
        {
            _timelines.Remove(timeline);
        }

        /// <summary>
        /// Updates the time of this <see cref="GlobalTimeTracker"/> (should be called on a per frame basis)
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds</param>
        public new void Update(double deltaTime)
        {
            base.Update(deltaTime);

            for (int i = 0; i < _timelines.Count; i++)
            {
                _timelines[i].Update(deltaTime);
            }
        }
    }
}
