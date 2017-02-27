/*
Project Orleans Cloud Service SDK ver. 1.0
 
Copyright (c) Microsoft Corporation
 
All rights reserved.
 
MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the ""Software""), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Globalization;
using System.Net;

using Orleans.Runtime.Host;
using System.Diagnostics;
using Orleans.Runtime.Configuration;
using SBTech.Orleans.Providers.Elastic;

namespace TestSilo
{
    internal class OrleansHostWrapper : IDisposable
    {
        public bool Debug
        {
            get { return siloHost != null && siloHost.Debug; }
            set { siloHost.Debug = value; }
        }

        private SiloHost siloHost;

        public OrleansHostWrapper(string[] args)
        {
            ParseArguments(args);
            Init();
        }

        public bool Run()
        {
            bool ok = false;

            try
            {
                siloHost.InitializeOrleansSilo();


                siloHost.Config.AddMemoryStorageProvider();
                siloHost.Config.Globals.RegisterStorageProvider<ElasticStatisticsProvider>("");




                ok = siloHost.StartOrleansSilo();

                if (ok)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Successfully started Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type));
                }
                else
                {
                    throw new SystemException(string.Format(CultureInfo.InvariantCulture, "Failed to start Orleans silo '{0}' as a {1} node.", siloHost.Name, siloHost.Type));
                }
            }
            catch (Exception exc)
            {	//log and mute
                siloHost.ReportStartupError(exc);
                var msg = string.Format(CultureInfo.InvariantCulture, "{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        public bool Stop()
        {
            bool ok = false;

            try
            {
                siloHost.ShutdownOrleansSilo();

                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Orleans silo '{0}' shutdown.", siloHost.Name));
            }
            catch (Exception exc)
            {	//log and mute
                siloHost.ReportStartupError(exc);
                var msg = string.Format(CultureInfo.InvariantCulture, "{0}:\n{1}\n{2}", exc.GetType().FullName, exc.Message, exc.StackTrace);
                Console.WriteLine(msg);
            }

            return ok;
        }

        private void Init()
        {
            TimeSpan _timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

            siloHost.LoadOrleansConfig();

            //do not make this silo host do the facility bootstrapper

            //////////////enable the Dependency Injection
            //////////////makes
            ////////////siloHost.NodeConfig.StartupTypeName = typeof(FacilityStartup).AssemblyQualifiedName;

            //////////////note the FacilityConfigurationClient used is specified with the startup type
            //////////////thus if you want to Moq the interface, rather than using the FacilityStartup class
            //////////////do so with the NodeConfig.StartupTypeName
            ////////////siloHost.Config.Globals.RegisterBootstrapProvider<FacilityBootstrapper>("R5Facility", new Dictionary<string, string>() { { "DO-NOT-BOOTSTRAP", "true" } } );


            //set up timeing for in debugger vs a straight run
            siloHost.Config.Globals.ClientDropTimeout= _timeout;
        }

        private bool ParseArguments(string[] args)
        {
            string deploymentId = null;

            string configFileName = "TestingSiloConfiguration.xml";
            string siloName = Dns.GetHostName(); // Default to machine name

            int argPos = 1;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a.StartsWith("-", StringComparison.OrdinalIgnoreCase) || a.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    switch (a.ToUpperInvariant())	//CA1308 - always use Upper, not Lower
                    {
                        case "/?":
                        case "/HELP":
                        case "-?":
                        case "-HELP":
                            // Query usage help
                            return false;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + a);
                            return false;
                    }
                }
                else if (a.Contains("="))
                {
                    string[] split = a.Split('=');
                    if (String.IsNullOrEmpty(split[1]))
                    {
                        Console.WriteLine("Bad command line arguments supplied: " + a);
                        return false;
                    }
                    switch (split[0].ToUpperInvariant())	//CA1308 - always use Upper, not Lower
                    {
                        case "DEPLOYMENTID":
                            deploymentId = split[1];
                            break;
                        case "DEPLOYMENTGROUP":
                            // TODO: Remove this at some point in future
                            Console.WriteLine("Ignoring deprecated command line argument: " + a);
                            break;
                        default:
                            Console.WriteLine("Bad command line arguments supplied: " + a);
                            return false;
                    }
                }
                // unqualified arguments below
                else if (argPos == 1)
                {
                    siloName = a;
                    argPos++;
                }
                else if (argPos == 2)
                {
                    configFileName = a;
                    argPos++;
                }
                else
                {
                    // Too many command line arguments
                    Console.WriteLine("Too many command line arguments supplied: " + a);
                    return false;
                }
            }

            siloHost = new SiloHost(siloName);
            siloHost.ConfigFileName = configFileName;
            if (deploymentId != null)
                siloHost.DeploymentId = deploymentId;

            return true;
        }

        public static void PrintUsage()
        {
            Console.WriteLine(
@"USAGE: 
    OrleansHost.exe [<siloName> [<configFile>]] [DeploymentId=<idString>] [/debug]
Where:
    <siloName>      - Name of this silo in the Config file list (optional)
    <configFile>    - Path to the Config file to use (optional)
    DeploymentId=<idString> 
                    - Which deployment group this host instance should run in (optional)
    /debug          - Turn on extra debug output during host startup (optional)");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool dispose)
        {
            siloHost.Dispose();
            siloHost = null;
        }
    }
}
