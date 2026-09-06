using Modding;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using OneShotPvP;

namespace OneShot_PvP
{
    public class OneShot_PvP : Mod
    {
        internal static OneShot_PvP Instance;

        public override void Initialize(
            Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;

            Assembly hkmpAssembly =
                typeof(Hkmp.Api.Client.ClientAddon).Assembly;

            Log(
                "HKMP assembly: " +
                hkmpAssembly.FullName
            );

            Log(
                "HKMP location: " +
                hkmpAssembly.Location
            );

            Log(
                "ClientAddon type assembly: " +
                typeof(Hkmp.Api.Client.ClientAddon).Assembly.FullName
            );

            Log(
                "Registering OneShot client addon..."
            );

            OneShotPvPMod.InitializeClient();

            Log(
                "Registering OneShot server addon..."
            );

            OneShotPvPMod.InitializeServer();

            Log("Initialized");
        }
    }
}