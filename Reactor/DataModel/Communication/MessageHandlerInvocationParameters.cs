﻿using System.Reflection;

namespace Reactor.DataModel.Communication
{
    internal class MessageHandlerInvocationParameters
    {
        public string ModID { get; set; }
        public string MessageName { get; set; }

        public MethodInfo Method { get; set; }
    }
}
