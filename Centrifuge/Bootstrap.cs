﻿using Centrifuge.UnityInterop.Bridges;
using Centrifuge.UnityInterop.Builders;
using Centrifuge.UnityInterop.DataModel;
using System;
using System.IO;
using System.Reflection;

namespace Centrifuge
{
    public static class Bootstrap
    {
        private static object ReactorManagerObject;

        public static void Initialize()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg == StartupArguments.AllocateConsole)
                {
                    if (IsMonoPlatform() && IsUnix())
                    {
                        ConsoleAllocator.CreateUnix();
                        EarlyLog.Info("Running on non-Windows platform. Skipping AllocConsole()...");
                    }
                    else
                    {
                        ConsoleAllocator.CreateWin32();
                    }
                }
            }

            var version = Assembly.GetAssembly(typeof(Bootstrap)).GetName().Version;

            Console.WriteLine($"Centrifuge Mod Loader for Unity Engine. Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}. Unity v{ApplicationBridge.UnityVersion}.");
            Console.WriteLine($"Diagnostics mode enabled. Remove '{StartupArguments.AllocateConsole}' command line argument to disable.");
            Console.WriteLine("--------------------------------------------");

            if (ApplicationBridge.GetRunningUnityGeneration() == UnityGeneration.Unity4OrOlder)
            {
                EarlyLog.Error("Centrifuge requires Unity 5 or newer. Terminating.");
                return;
            }

            EarlyLog.Info("Trying to find Centrifuge Reactor DLL...");

            var reactorPath = GetCrossPlatformCompatibleReactorPath();
            if (!File.Exists(reactorPath))
            {
                EarlyLog.Error($"Centrifuge Reactor DLL could not be found at '{reactorPath}'. Mods will not be loaded.");
                return;
            }

            Type proxyType;
            try
            {
                EarlyLog.Info("Validating and loading Centrifuge Reactor DLL...");
                Assembly.LoadFrom(reactorPath);
                EarlyLog.Info("Loaded");

                proxyType = new ManagerProxyBuilder().Build();
            }
            catch (Exception ex)
            {
                if (ex.InnerException is ReflectionTypeLoadException rtle)
                {
                    EarlyLog.Exception(rtle);
                    EarlyLog.Exception(rtle.InnerException);

                    EarlyLog.Info("------------- LOADER EXCEPTIONS FOLLOW --------------- ");
                    foreach (var lex in rtle.LoaderExceptions)
                    {
                        EarlyLog.Exception(lex);
                    }
                }
                else
                {
                    EarlyLog.Exception(ex);
                }
                return;
            }

            try
            {
                EarlyLog.Info("Creating Reactor Manager GameObject...");
                ReactorManagerObject = GameObjectBridge.CreateGameObject("com.github.ciastex/ReactorModLoaderProxy");
            }
            catch (Exception e)
            {
                EarlyLog.Exception(e);
                return;
            }

            EarlyLog.Info("About to add component to Reactor Manager GameObject...");
            object proxyComponent;
            try
            {
                proxyComponent = GameObjectBridge.AttachComponentTo(ReactorManagerObject, proxyType);
            }
            catch (Exception e)
            {
                EarlyLog.Exception(e);
                return;
            }

            Console.WriteLine("--------------------------------------------");
        }

        private static string GetCrossPlatformCompatibleReactorPath()
            => (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "?..?Centrifuge?Reactor.dll".Replace('?', Path.DirectorySeparatorChar));

        private static bool IsMonoPlatform()
        {
            var platformID = (int)Environment.OSVersion.Platform;
            return platformID == 4 || platformID == 6 || platformID == 128;
        }

        private static bool IsUnix()
        {
            var platformID = Environment.OSVersion.Platform;
            switch (platformID)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    return true;
                default:
                    return false;
            }
        }
    }
}
