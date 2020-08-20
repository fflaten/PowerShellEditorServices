﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerShell.EditorServices.Server;
using Microsoft.PowerShell.EditorServices.Services;
using Microsoft.PowerShell.EditorServices.Services.PowerShell;
using Microsoft.PowerShell.EditorServices.Services.PowerShell.Execution;
using Microsoft.PowerShell.EditorServices.Services.PowerShell.Runspace;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace Microsoft.PowerShell.EditorServices.Handlers
{
    internal class DisconnectHandler : IDisconnectHandler
    {
        private readonly ILogger<DisconnectHandler> _logger;
        private readonly PowerShellExecutionService _executionService;
        private readonly DebugService _debugService;
        private readonly DebugStateService _debugStateService;
        private readonly DebugEventHandlerService _debugEventHandlerService;
        private readonly PsesDebugServer _psesDebugServer;

        public DisconnectHandler(
            ILoggerFactory factory,
            PsesDebugServer psesDebugServer,
            PowerShellExecutionService executionService,
            DebugService debugService,
            DebugStateService debugStateService,
            DebugEventHandlerService debugEventHandlerService)
        {
            _logger = factory.CreateLogger<DisconnectHandler>();
            _psesDebugServer = psesDebugServer;
            _executionService = executionService;
            _debugService = debugService;
            _debugStateService = debugStateService;
            _debugEventHandlerService = debugEventHandlerService;
        }

        public async Task<DisconnectResponse> Handle(DisconnectArguments request, CancellationToken cancellationToken)
        {
            _debugEventHandlerService.UnregisterEventHandlers();
            if (_debugStateService.ExecutionCompleted == false)
            {
                _debugStateService.ExecutionCompleted = true;
                _executionService.CancelCurrentTask();

                if (_debugStateService.IsInteractiveDebugSession && _debugStateService.IsAttachSession)
                {
                    // Pop the sessions
                    if (_executionService.CurrentRunspace.RunspaceOrigin == RunspaceOrigin.EnteredProcess)
                    {
                        try
                        {
                            await _executionService.ExecutePSCommandAsync(
                                new PSCommand().AddCommand("Exit-PSHostProcess"),
                                new PowerShellExecutionOptions(),
                                CancellationToken.None).ConfigureAwait(false);

                            if (_debugStateService.IsRemoteAttach &&
                                _executionService.CurrentRunspace.IsRemote())
                            {
                                await _executionService.ExecutePSCommandAsync(
                                    new PSCommand().AddCommand("Exit-PSSession"),
                                    new PowerShellExecutionOptions(),
                                    CancellationToken.None).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogException("Caught exception while popping attached process after debugging", e);
                        }
                    }
                }

                _debugService.IsClientAttached = false;
            }

            _logger.LogInformation("Debug adapter is shutting down...");

#pragma warning disable CS4014
            // Trigger the clean up of the debugger. No need to wait for it.
            Task.Run(_psesDebugServer.OnSessionEnded);
#pragma warning restore CS4014

            return new DisconnectResponse();
        }
    }
}
