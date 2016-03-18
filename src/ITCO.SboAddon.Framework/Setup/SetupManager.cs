﻿using ITCO.SboAddon.Framework.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SAPbouiCOM;

namespace ITCO.SboAddon.Framework.Setup
{
    /// <summary>
    /// Setup Manager
    /// Uses setup version for faster startup
    /// </summary>
    public static class SetupManager
    {
        /// <summary>
        /// Find ISetup classes and Run Setup
        /// </summary>
        /// <param name="assembly">Assembly to search in</param>
        public static void FindAndRunSetups(Assembly assembly)
        {
            var setups = (from t in assembly.GetTypes()
                          where !t.IsInterface && t.GetInterfaces().Contains(typeof(ISetup))
                          select t).ToArray();

            if (!setups.Any())
                return;

            SettingService.Init();

            foreach (var setup in setups)
            {
                var setupInstance = Activator.CreateInstance(setup) as ISetup;
                RunSetup(setupInstance, true);
            }
        }

        /// <summary>
        /// Run Setup, check version
        /// </summary>
        /// <typeparam name="TSetup"></typeparam>
        /// <param name="setupInstance"></param>
        /// <param name="showApplicationMessages"></param>
        public static void RunSetup<TSetup>(TSetup setupInstance, bool showApplicationMessages = false) where TSetup : ISetup
        {
            var setup = setupInstance.GetType();
            var key = $"setup.lastversion.{setup.Name.Replace("Setup", string.Empty)}";
            var lastVersionInstalled = SettingService.GetSettingByKey(key, 0);

            if (lastVersionInstalled < setupInstance.Version)
            {
                try
                {
                    if (showApplicationMessages)
                        SboApp.Application.StatusBar.SetText($"Running setup for {setup.Name}, current version is {lastVersionInstalled}, new version is {setupInstance.Version})"
                            ,BoMessageTime.bmt_Medium, BoStatusBarMessageType.smt_Warning);

                    setupInstance.Run();
                    SettingService.SaveSetting(key, setupInstance.Version);
                }
                catch (Exception ex)
                {
                    if (showApplicationMessages)
                        SboApp.Application.MessageBox($"Setup error in {setup.Name}: {ex.Message}");
                    else
                        throw;
                }
            }

            if (showApplicationMessages)
                SboApp.Application.StatusBar.SetText($"Setup for {setup.Name} is up-to-date! (v.{lastVersionInstalled})", 
                    BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Success);
        }
    }
}
