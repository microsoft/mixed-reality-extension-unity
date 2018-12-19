// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension.Messaging.Commands
{
    internal interface ICommandManager
    {
        void ExecuteCommandPayload(ICommandHandlerContext handlerContext, ICommandPayload commandPayload);
    }
}
