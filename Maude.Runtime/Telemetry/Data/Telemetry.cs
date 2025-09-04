using System;
using Ansight.Adb.Sessions.Data;
using SQLite;

namespace Maude.Runtime.Telemetry.Data
{
    public class Telemetry : Entity
    {
        /// <summary>
        /// The telemetry channel that this sample belongs to.
        /// </summary>
        [Indexed]
        public string Channel { get; set; }

        /// <summary>
        /// The group within the telemetry channel that this sample belongs to.
        /// <para/>
        /// A group represents a discrete line kind within the sample set.
        /// </summary>
        [Indexed]
        public string Group { get; set; }

        /// <summary>
        /// The telemery segment that this sample belongs to.
        /// </summary>
        [Indexed]
        public Guid Segment { get; set; }

        /// <summary>
        /// The date/time that this telemery sample was recorded on the device in UTC time.
        /// </summary>
        [Indexed]
        public DateTime CapturedAtUtc { get; set; }

        /// <summary>
        /// The value of this telemetry sample.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// An additional data payload that may be attached to this telemetry sample.
        /// </summary>
        public string Data { get; set; }
    }
}

