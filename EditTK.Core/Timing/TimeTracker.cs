using System;
using System.Collections.Generic;
using System.Linq;

namespace EditTK.Core.Timing
{
    public delegate void TimeIntervallHandler(int passedIntervals);

    public class RegisteredIntervallHandler
    {
        private TimeTracker _timeTracker;

        internal TimeIntervallHandler Handler;
        internal double Intervall;
        internal double LastTime;

        public RegisteredIntervallHandler(TimeIntervallHandler handler, double intervall, double lastTime, TimeTracker timeTracker)
        {
            Handler = handler;
            Intervall = intervall;
            LastTime = lastTime;
            _timeTracker = timeTracker;
        }

        public void Unregister() => _timeTracker.RemoveIntervallHandler(this);
    }

    public abstract class TimeTracker
    {

        readonly List<RegisteredIntervallHandler> _intervallHandlers = 
             new List<RegisteredIntervallHandler>();

        /// <summary>
        /// The current time of this <see cref="TimeTracker"/> in seconds
        /// </summary>
        public double Time { get; private set; }

        /// <summary>
        /// The time the last frame took in seconds
        /// </summary>
        public double DeltaTime { get; private set; }


        /// <summary>
        /// Adds an event handler that's called regularly in given intervalls
        /// </summary>
        /// <param name="handler">The delegate for handling all calls</param>
        /// <param name="intervall">the intervall between two calls in seconds</param>
        /// 
        /// <remarks>
        /// Make sure to remove it with <see cref="RemoveIntervallHandler(TimeIntervallHandler)"/> as soon as you don't need it anymore!
        /// <para>You can only add the same handler once</para>
        /// </remarks>
        public RegisteredIntervallHandler AddIntervallHandler(TimeIntervallHandler handler, double intervall)
        {
            var rih = new RegisteredIntervallHandler(handler, intervall, Time, this);

            _intervallHandlers.Add(rih);

            return rih;
        }

        /// <summary>
        /// Removes the given event handler from the list of all intervall handlers of this <see cref="TimeTracker"/>
        /// </summary>
        public void RemoveIntervallHandler(RegisteredIntervallHandler handler)
        {
            _intervallHandlers.Remove(handler);
        }


        protected void Update(double deltaTime)
        {
            DeltaTime = deltaTime;
            Time += deltaTime;

            for (int i = 0; i < _intervallHandlers.Count; i++)
            {
                var handler   = _intervallHandlers[i].Handler;
                var intervall = _intervallHandlers[i].Intervall;
                var lastTime  = _intervallHandlers[i].LastTime;

                double timePassed = Time - lastTime;

                if (timePassed > intervall)
                {
                    int passedIntervals = (int)(timePassed / intervall);

                    handler(passedIntervals);

                    _intervallHandlers[i].LastTime = lastTime + intervall * passedIntervals; //make sure that no interval is left out
                }
            }
        }

        protected void SetTime(double time)
        {
            DeltaTime = 0;
            Time = time;

            //set the time for all intervall handlers
            for (int i = 0; i < _intervallHandlers.Count; i++)
            {
                _intervallHandlers[i].LastTime = Time;
            }
        }
    }
}