// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Payloads;
using Newtonsoft.Json;

namespace MixedRealityExtension.RPC
{
    /// <summary>
    /// Class that represents the remote procedure call interface for the MRE interop library.
    /// </summary>
    public sealed class RPCInterface
    {
        private readonly MixedRealityExtensionApp _app;

        private Dictionary<string, RPCHandlerBase> _handlers = new Dictionary<string, RPCHandlerBase>();

        internal RPCInterface(MixedRealityExtensionApp app) => _app = app;

        /// <summary>
        /// Registers and RPC handler for the specific procedure name
        /// </summary>
        /// <param name="procName">The name of the remote procedure.</param>
        /// <param name="handler">The handler to be called when an RPC call is received for the given procedure name.</param>
        public void OnReceive(string procName, RPCHandlerBase handler)
        {
            _handlers[procName] = handler;
        }

        internal void ReceiveRPC(AppToEngineRPC payload)
        {
            if (_handlers.ContainsKey(payload.ProcName))
            {
                _handlers[payload.ProcName].Execute(payload.Args.Children().ToArray());
            }
        }

        /// <summary>
        /// Sends an RPC message to the app with the given name and arguments.
        /// </summary>
        /// <param name="procName">The name of the remote procedure call.</param>
        /// <param name="args">The arguments for the remote procedure call.</param>
        public void SendRPC(string procName, params object[] args)
        {
            _app.Protocol.Send(new EngineToAppRPC()
            {
                ProcName = procName,
                Args = args.ToList()
            });
        }
    }
}
