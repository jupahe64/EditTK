namespace EditTK.Core.Timing
{
    public class Timeline : TimeTracker
    {
        /// <summary>
        /// The factor by which time advances
        /// <para>default is 1.0</para>
        /// </summary>
        public double TimeScale { get; set; } = 1.0;

        internal new void Update(double deltaTime)
        {
            base.Update(deltaTime);
        }

        /// <summary>
        /// Sets the time in seconds for this Timeline to a specific value
        /// <para>Should not be called every frame</para>
        /// </summary>
        public new void SetTime(double time)
        {
            base.SetTime(time);
        }
    }
}
