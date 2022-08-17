﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Neo4j.Driver.Tests.TestBackend;

public class Program
{
    private static IPAddress _address;
    private static uint _port;
    private static bool _reactive;

    static async Task Main(string[] args)
    {
        var consoleTraceListener = new TextWriterTraceListener(Console.Out);
        Trace.Listeners.Add(consoleTraceListener);

        try
        {
            ArgumentsValidation(args);

            await RunAsync();
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message);
            Trace.WriteLine($"Exception Details: \n {ex.StackTrace}");
        }
        finally
        {
            Trace.Flush();
            Trace.Listeners.Remove(consoleTraceListener);
            consoleTraceListener.Close();
            Trace.Close();
        }
    }

    private static void ArgumentsValidation(string[] args)
    {
        if (args.Length < 3)
            throw new IOException($"Incorrect number of arguments passed in. Expecting Address Port, but got {args.Length} arguments");

        if (!IPAddress.TryParse(args[0], out _address))
            throw new IOException($"Invalid IPAddress passed in parameter 1. {args[0]}");

        if (!uint.TryParse(args[1], out _port))
            throw new IOException($"Invalid port passed in parameter 2.  Should be unsigned integer but was: {args[1]}.");

        if (!bool.TryParse(args[2], out _reactive))
            throw new IOException($"Invalid Reactive parameter passed in parameter 3. {args[2]}");

        if (args.Length > 3)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(args[3]));
            Trace.WriteLine("Logging to file: " + args[3]);
        }

        var mode = _reactive ? "Reactive" : "Async";
        Trace.WriteLine($"Starting TestBackend on {_address}:{_port} in {mode} Mode.");
    }

    private static async Task RunAsync()
    {
        using var connection = new Connection(_address.ToString(), _port);
        var controller = new Controller(connection, _reactive);

        try
        {
            await controller.ProcessAsync(true, _ => true);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"It looks like the ExceptionExtensions system has failed in an unexpected way. \n{ex}");
        }
    }

}