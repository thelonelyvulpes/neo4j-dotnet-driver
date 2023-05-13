// Copyright (c) "Neo4j"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace Neo4j.Driver.Tests.TestBackend;

internal sealed class Controller
{
    public Controller(Stream stream)
    {
        Trace.WriteLine("Controller initialising");
        
        var connectionReader = new StreamReader(stream, new UTF8Encoding(false));
        var connectionWriter = new StreamWriter(stream, new UTF8Encoding(false));
        connectionWriter.NewLine = "\n";
        
        RequestReader = new RequestReader(stream);
        ResponseWriter = new ResponseWriter(connectionWriter);
        
        ProtocolObjectFactory.ObjManager = new ProtocolObjectManager();
    }
    
    private bool BreakProcessLoop { get; set; }
    private RequestReader RequestReader { get; }
    private ResponseWriter ResponseWriter { get; }
    public TransactionManager TransactionManager { get; } = new();

    private async Task ProcessStreamObjects()
    {
        BreakProcessLoop = false;

        while (!BreakProcessLoop)
        {
            var protocolObject = await RequestReader.ParseNextRequest().ConfigureAwait(false);
            if (protocolObject == null)
            {
                Trace.WriteLine("Protocol object was null, breaking loop");
                break;
            }
            protocolObject.ProtocolEvent += BreakLoopEvent;

            await protocolObject.Process(this).ConfigureAwait(false);
            await SendResponse(protocolObject).ConfigureAwait(false);
            Trace.Flush();
        }

        BreakProcessLoop = false; //Ensure that any process loops that this one is running within still continue.
    }

    private async Task<IProtocolObject> TryConsumeStreamObjectOfType(Type type)
    {
        //Read the next incoming request message
        var protocolObject = await RequestReader.ParseNextRequest().ConfigureAwait(false);
        if (protocolObject.GetType().Name != type.Name)
        {
            throw new Exception("not good");
        }
        return protocolObject;
    }

    public async Task<T> TryConsumeStreamObjectOfType<T>() where T : IProtocolObject
    {
        var result = await TryConsumeStreamObjectOfType(typeof(T)).ConfigureAwait(false);
        return (T)result;
    }

    public async Task Process(Func<Exception, bool> loopConditional, CancellationToken cancellationToken = default)
    {
        Trace.WriteLine("Starting Controller.Process");

        while (true)
        {
            try
            {
                await ProcessStreamObjects().ConfigureAwait(false);
            }
            catch (FinishedException)
            {
                throw;
            }
            catch (Exception ex) when (ex is Neo4jException
                                           or TestKitClientException
                                           or ArgumentException
                                           or NotSupportedException
                                           or JsonSerializationException
                                           or TestKitProtocolException
                                           or DriverExceptionWrapper)
            {
                await ResponseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                if (!loopConditional(ex))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"General exception detected, restarting connection.\n{ex}");
                break;
            }
            Trace.Flush();
        }
    }

    private void BreakLoopEvent(object sender, EventArgs e)
    {
        BreakProcessLoop = true;
    }

    private Task SendResponse(IProtocolObject protocolObject)
    {
        return ResponseWriter.WriteResponseAsync(protocolObject);
    }

    public Task SendResponse(string response)
    {
        return ResponseWriter.WriteResponseAsync(response);
    }
}
