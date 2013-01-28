/*  New BSD License
-------------------------------------------------------------------------------
Copyright (c) 2006-2012, EntitySpaces, LLC
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the EntitySpaces, LLC nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL EntitySpaces, LLC BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
-------------------------------------------------------------------------------
*/

using System;
using System.Data;
using System.Threading;

using TheCodeKing.Net.Messaging;

using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Runtime.Serialization;

using Tiraggo.Interfaces;

namespace Tiraggo.Profiler
{
    public class ProfilerListener
    {
        private ProfilerListener() { }

        public delegate void TraceEventHandler(ITraceArguments packet);

        /// <summary>
        /// The instance used to broadcast messages on a particular channel.
        /// </summary>
        private static IXDBroadcast broadcast;
        private static ProfilerListener profileListener = new ProfilerListener();
        private static string delimiter = "±";

        public static bool BeginProfiling(string dataProvider, string channel)
        {
            IDataProvider provider = esProviderFactory.Factory.GetDataProvider(dataProvider, "DataProvider");

            if (provider != null)
            {
                if (!provider.IsTracing)
                {
                    // create an instance of IXDBroadcast using the given mode, 
                    // note IXDBroadcast does not implement IDisposable
                    broadcast = XDBroadcast.CreateBroadcast(XDTransportMode.IOStream);

                    provider.TraceChannel = channel;
                    provider.TraceHandler += profileListener.MyTraceEventHandler;
                }
            }

            return true;
        }

        public static bool EndProfiling(string dataProvider)
        {
            // create an instance of IXDBroadcast using the given mode, 
            // note IXDBroadcast does not implement IDisposable
            broadcast = null;

            IDataProvider provider = esProviderFactory.Factory.GetDataProvider(dataProvider, "DataProvider");

            if (provider != null)
            {
                provider.TraceHandler -= profileListener.MyTraceEventHandler;
            }

            return true;
        }

        private void MyTraceEventHandler(ITraceArguments packet)
        {
            string data = "";

            // TransactionId
            data += (packet.SqlCommand.Transaction != null) ? packet.SqlCommand.Transaction.GetHashCode().ToString() : "";
            data += delimiter;

            // ObjectType
            object caller = packet.Request.DynamicQuery ?? packet.Request.Caller;

            if (caller != null)
            {
                data += caller.GetType().Name;
            }
            data += delimiter;

            // StackTrace - ApplicationName - TraceChannel - ThreadId - SQL - Duration
            data += packet.CallStack + delimiter + packet.ApplicationName + delimiter + packet.TraceChannel + delimiter +
                packet.ThreadId.ToString() + delimiter + packet.SqlCommand.CommandText + delimiter + packet.Duration.ToString() + delimiter +
                packet.Ticks.ToString() + delimiter + packet.PacketOrder.ToString() + delimiter + packet.Action + delimiter + packet.Syntax + delimiter;

            data += packet.Exception == null ? string.Empty : packet.Exception;
            data += delimiter;

            if (packet.Parameters != null && packet.Parameters.Count > 0)
            {
                bool first = true;

                for (int i = 0; i < packet.Parameters.Count; i++)
                {
                    ITraceParameter param = packet.Parameters[i] as ITraceParameter;

                    if (!first) data += "«";

                    data += param.Name + "«" + param.Direction + "«" + param.ParamType + "«";
                    data += param.BeforeValue != null ? Convert.ToString(param.BeforeValue) : "null";
                    data += "«";
                    data += param.AfterValue != null ? Convert.ToString(param.AfterValue) : "null";

                    first = false;
                }
            }

            broadcast.SendToChannel(packet.TraceChannel, data);
        }

        public class Channels
        {
            public const string Channel_1 = "Channel_1";
            public const string Channel_2 = "Channel_2";
            public const string Channel_3 = "Channel_3";
            public const string Channel_4 = "Channel_4";
            public const string Channel_5 = "Channel_5";
            public const string Channel_6 = "Channel_6";
            public const string Channel_7 = "Channel_7";
            public const string Channel_8 = "Channel_8";
            public const string Channel_9 = "Channel_9";
            public const string Channel_10 = "Channel_10";
        }
    }
}
