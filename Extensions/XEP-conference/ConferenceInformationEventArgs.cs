using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Xmpp.Extensions
{
    /// <summary>
    /// Provides data for the ConferenceInformationEventArgs event.
    /// </summary>
    public class ConferenceInformationEventArgs : EventArgs
    {
        /// <summary>
        /// The conversation  id 
        /// </summary>
        public String Information
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the ConferenceInformationEventArgs class.
        /// </summary>
        /// <param name="information">the information</param>
        /// <summary>
        public ConferenceInformationEventArgs(String information)
        {
            Information = information;
        }
    }
}
