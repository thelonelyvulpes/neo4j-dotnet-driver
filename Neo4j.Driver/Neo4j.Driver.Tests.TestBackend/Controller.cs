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
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neo4j.Driver.Tests.TestBackend;

internal sealed class Controller : IDisposable
{
    public Controller(Stream conn)
    {
        Trace.WriteLine("Controller initialising.");
        RequestReader = new RequestReader(conn);
        ResponseWriter = new ResponseWriter(
            new StreamWriter(conn, new UTF8Encoding(false))
            {
                NewLine = "\n"
            });

        ObjManager = new ProtocolObjectManager();
        TransactionManager = new TransactionManager();
        ProtocolObjectFactory.ObjManager = ObjManager;
    }
    
    public void Dispose()
    {
        RequestReader.Dispose();
    }

    private ProtocolObjectManager ObjManager { get; }
    private RequestReader RequestReader { get; }
    private ResponseWriter ResponseWriter { get; }
    public TransactionManager TransactionManager { get; }

    private async Task ProcessStreamObjects()
    {
        var keepProcessing = true;
        while (keepProcessing)
        {
            var result = await RequestReader.ParseNextRequest();
            if (result == null)
            {
                Trace.WriteLine("No more requests to process");
                break;
            }
            result.ProtocolEvent += (_, __) =>
            {
                keepProcessing = false;
            };
            await result.Process(this);
            await SendResponse(result);
            Trace.Flush();
        }
    }
    
    public async Task Process(bool restartInitialState, Func<Exception, bool> loopConditional)
    {
        var restartConnection = restartInitialState;
        
        Trace.WriteLine("Starting Controller.Process");

        var storedException = default(Exception);

        while (loopConditional(storedException))
        {
            try
            {
                await ProcessStreamObjects();
            }
            catch (FinishedException)
            {
                throw;
            }
            catch (Exception ex) when (ExpectedExceptions(ex))
            {
                await ResponseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                storedException = ex;
                restartConnection = false;
            }
            catch (TestKitProtocolException ex)
            {
                Trace.WriteLine($"TestKit protocol exception detected: {ex.Message}");
                await ResponseWriter.WriteResponseAsync(ExceptionManager.GenerateExceptionResponse(ex));
                throw new FinishedException(false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"General exception detected, restarting connection: {ex}");
                throw new FinishedException(false);
            }
            
            if (restartConnection)
            {
                throw new FinishedException();
            }

            Trace.Flush();
        }
    }

    private async Task<IProtocolObject> TryConsumeStreamObjectOfType(Type type)
    {
        var result = await RequestReader.ParseNextRequest();
        return result.GetType().Name != type.Name ? null : result;
    }

    public async Task<T> TryConsumeStreamObjectOfType<T>() where T : IProtocolObject
    {
        return (T)await TryConsumeStreamObjectOfType(typeof(T));
    }
    
    private bool ExpectedExceptions(Exception exception)
    {
        return exception is Neo4jException
            or TestKitClientException
            or ArgumentException
            or NotSupportedException
            or JsonSerializationException
            or DriverExceptionWrapper;
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
