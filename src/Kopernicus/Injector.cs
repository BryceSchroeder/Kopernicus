﻿/**
 * Kopernicus Planetary System Modifier
 * -------------------------------------------------------------
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 *
 * This library is intended to be used as a plugin for Kerbal Space Program
 * which is copyright of TakeTwo Interactive. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 *
 * https://kerbalspaceprogram.com
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
// ReSharper disable once RedundantUsingDirective
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
// ReSharper disable once RedundantUsingDirective
using System.Security.Cryptography;
using HarmonyLib;
using Kopernicus.ConfigParser;
using Kopernicus.Configuration;
using Kopernicus.Constants;
using UnityEngine;

namespace Kopernicus
{
    // Hook the PSystemSpawn (creation of the planetary system) event in the KSP initialization lifecycle
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, false)]
    public class Injector : MonoBehaviour
    {
        // Name of the config node group which manages Kopernicus
        private const String ROOT_NODE_NAME = "Kopernicus";

        // The checksum of the System.cfg file.
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private const String CONFIG_CHECKSUM = "cc7bda69901a41a4231c0a84696615029c4116834e0fceda70cc18d863279533";

        // Backup of the old system prefab, in case someone deletes planet templates we need at Runtime (Kittopia)
        public static PSystem StockSystemPrefab { get; private set; }

        // Whether the injector is currently patching the prefab
        public static Boolean IsInPrefab { get; private set; }

        public static MapSO moho_biomes;
        public static MapSO moho_height;

        // Awake() is the first function called in the lifecycle of a Unity3D MonoBehaviour.  In the case of KSP,
        // it happens to be called right before the game's PSystem is instantiated from PSystemManager.Instance.systemPrefab
        public void Awake()
        {
            // Abort, if KSP isn't compatible
            if (!CompatibilityChecker.IsCompatible())
            {
                String supported = CompatibilityChecker.VERSION_MAJOR + "." + CompatibilityChecker.VERSION_MINOR + "." +
                                   CompatibilityChecker.REVISION;
                String current = Versioning.version_major + "." + Versioning.version_minor + "." + Versioning.Revision;
                Debug.LogWarning("[Kopernicus] Detected incompatible install.\nCurrent version of KSP: " + current +
                                 ".\nSupported version of KSP: " + supported +
                                 ".\nPlease wait, until Kopernicus gets updated to match your version of KSP.");
                Debug.Log("[Kopernicus] Aborting...");

                // Abort
                Destroy(this);
                return;
            }

            // Log the current version to the log
            String kopernicusVersion = CompatibilityChecker.VERSION_MAJOR + "." +
                                       CompatibilityChecker.VERSION_MINOR + "." + CompatibilityChecker.REVISION +
                                       "-" + CompatibilityChecker.KOPERNICUS;
            String kspVersion = Versioning.version_major + "." + Versioning.version_minor + "." +
                                Versioning.Revision;
            Debug.Log("[Kopernicus] Running Kopernicus " + kopernicusVersion + " on KSP " + kspVersion);
            //Harmony stuff
            if (RuntimeUtility.RuntimeUtility.KopernicusConfig.UseStockMohoTemplate)
            {
                MapSO[] so = Resources.FindObjectsOfTypeAll<MapSO>();

                foreach (MapSO mapSo in so)
                {
                    if (mapSo.MapName == "moho_biomes"
                        && mapSo.Size == 6291456
                        && mapSo._data[0] == 216
                        && mapSo._data[1] == 178
                        && mapSo._data[2] == 144)
                    {
                        moho_biomes = mapSo;
                    }
                    else if (mapSo.MapName == "moho_height"
                             && mapSo.Size == 2097152
                             && mapSo._data[1509101] == 146
                             && mapSo._data[1709108] == 162
                             && mapSo._data[1909008] == 216)
                    {
                        moho_height = mapSo;
                    }
                }
            }
            Harmony harmony = new Harmony("Kopernicus");
            harmony.PatchAll();

            // Wrap this in a try - catch block so we can display a warning if Kopernicus fails to load for some reason
            try
            {
                // We're ALIVE
                IsInPrefab = true;
                Logger.Default.SetAsActive();
                Logger.Default.Log("Injector.Awake(): Begin");

                // Parser Config
                ParserOptions.Register("Kopernicus",
                    new ParserOptions.Data
                    { ErrorCallback = e => Logger.Active.LogException(e), LogCallback = s => Logger.Active.Log(s) });

                // Yo garbage collector - we have work to do man
                DontDestroyOnLoad(this);

                // If the planetary manager does not work, well, error out
                if (PSystemManager.Instance == null)
                {
                    // Log the error
                    Logger.Default.Log("Injector.Awake(): If PSystemManager.Instance is null, there is nothing to do");
                    DisplayWarning();
                    return;
                }

                // Was the system template modified?
#if !DEBUG
                String systemCfgPath = KSPUtil.ApplicationRootPath + "GameData/Kopernicus/Config/System.cfg";
                if (File.Exists(systemCfgPath))
                {
                    Byte[] data = File.ReadAllBytes(systemCfgPath);
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        String checksum = BitConverter.ToString(sha256.ComputeHash(data));
                        checksum = checksum.Replace("-", "");
                        checksum = checksum.ToLower();
                        if (checksum != CONFIG_CHECKSUM)
                        {
                            throw new Exception(
                                "The file 'Kopernicus/Config/System.cfg' was modified directly without ModuleManager");
                        }
                    }
                }
#endif

                // Backup the old prefab
                StockSystemPrefab = PSystemManager.Instance.systemPrefab;

                // Fire Pre-Load Event
                Events.OnPreLoad.Fire();

                // Get the current time
                DateTime start = DateTime.Now;

                // Get the configNode
                ConfigNode kopernicus = GameDatabase.Instance.GetConfigs(ROOT_NODE_NAME)[0].config;

                // THIS IS WHERE THE MAGIC HAPPENS - OVERWRITE THE SYSTEM PREFAB SO KSP ACCEPTS OUR CUSTOM SOLAR SYSTEM AS IF IT WERE FROM SQUAD
                PSystemManager.Instance.systemPrefab =
                    Parser.CreateObjectFromConfigNode<Loader>(kopernicus, "Kopernicus").SystemPrefab;

                // Clear space center instance so it will accept nouveau Kerbin
                SpaceCenter.Instance = null;

                // Add a handler so that we can do post spawn fixups.
                PSystemManager.Instance.OnPSystemReady.Add(PostSpawnFixups);

                // Fire Post-Load Event
                Events.OnPostLoad.Fire(PSystemManager.Instance.systemPrefab);

                // Done executing the awake function
                TimeSpan duration = DateTime.Now - start;
                Logger.Default.Log("Injector.Awake(): Completed in: " + duration.TotalMilliseconds + " ms");
                Logger.Default.Flush();
                IsInPrefab = false;
            }
            catch (Exception e)
            {
                // Log the exception
                Debug.LogException(e);

                // Open the Warning popup
                DisplayWarning();
            }
        }

        // Post spawn fixups (ewwwww........)
        public void PostSpawnFixups()
        {
            // Wrap this in a try - catch block so we can display a warning if Kopernicus fails to load for some reason
            try
            {
                // Log
                Debug.Log("[Kopernicus]: Post-Spawn");

                // Fire Event
                Events.OnPreFixing.Fire();

                // Fix the SpaceCenter
                SpaceCenter.Instance = PSystemManager.Instance.localBodies.First(cb => cb.isHomeWorld)
                    .GetComponentsInChildren<SpaceCenter>(true).FirstOrDefault();
                if (SpaceCenter.Instance != null)
                {
                    SpaceCenter.Instance.Start();
                }

                // Fix the flight globals index of each body and patch it's SOI
                Int32 counter = 0;
                CelestialBody mockBody = null;
                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    //Find ye old watchdog for slaying (if it exists)
                    if (body.name.Equals("KopernicusWatchdog"))
                    {
                        mockBody = body;
                    }
                    // Event
                    Events.OnPreBodyFixing.Fire(body);

                    // Patch the flightGlobalsIndex
                    body.flightGlobalsIndex = counter++;

                    // Finalize the Orbit
                    if (body.Get("finalizeBody", false))
                    {
                        OrbitLoader.FinalizeOrbit(body);
                    }

                    // Set Custom OrbitalPeriod
                    if (body.Has("customOrbitalPeriod"))
                    {
                        OrbitLoader.OrbitalPeriod(body);
                    }

                    // Patch the SOI
                    if (body.Has("sphereOfInfluence"))
                    {
                        body.sphereOfInfluence = body.Get<Double>("sphereOfInfluence");
                    }

                    // Patch the Hill Sphere
                    if (body.Has("hillSphere"))
                    {
                        body.hillSphere = body.Get<Double>("hillSphere");
                    }

                    // Make the Body a barycenter
                    if (body.Get("barycenter", false))
                    {
                        foreach (Collider collider in body.scaledBody.GetComponentsInChildren<Collider>(true))
                        {
                            collider.enabled = false;
                        }
                        body.scaledBody.SetActive(false);
                    }

                    // Make the bodies scaled space invisible 
                    if (body.Get("invisibleScaledSpace", false))
                    {
                        foreach (Renderer renderer in body.scaledBody.GetComponentsInChildren<Renderer>(true))
                        {
                            renderer.enabled = false;
                        }

                        foreach (Collider collider in body.scaledBody.GetComponentsInChildren<Collider>(true))
                        {
                            collider.enabled = false;
                        }

                        foreach (ScaledSpaceFader fader in body.scaledBody.GetComponentsInChildren<ScaledSpaceFader>(
                            true))
                        {
                            fader.enabled = false;
                        }
                    }
                    else
                    {
                        foreach (Renderer renderer in body.scaledBody.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer.enabled)
                            {
                                foreach (Collider collider in body.scaledBody.GetComponentsInChildren<Collider>(true))
                                {
                                    collider.enabled = true;
                                }
                            }
                        }
                    }

                    // Event
                    Events.OnPostBodyFixing.Fire(body);

                    // Log
                    Logger.Default.Log("Found Body: " + body.bodyName + ":" + body.flightGlobalsIndex + " -> SOI = " +
                                       body.sphereOfInfluence + ", Hill Sphere = " + body.hillSphere);
                }
                //Mark the watchdog for proper removal
                if (mockBody != null)
                {
                    try
                    {
                        FlightGlobals.Bodies.Remove(mockBody);
                        if (Kopernicus.Components.KopernicusStar.GetLocalStar(mockBody).orbitingBodies.Contains(mockBody))
                        {
                            Kopernicus.Components.KopernicusStar.GetLocalStar(mockBody).orbitingBodies.Remove(mockBody);
                        }
                        mockBody.gameObject.DestroyGameObject();
                    }
                    catch
                    {

                    }
                }

                // Fix the maximum viewing distance of the map view camera (get the farthest away something can be from the root object)
                PSystemBody rootBody = PSystemManager.Instance.systemPrefab.rootBody;
                Double maximumDistance = 1000d;
                if (rootBody != null)
                {
                    maximumDistance = rootBody.celestialBody.Radius * 100d;
                    if (rootBody.children != null && rootBody.children.Count > 0)
                    {
                        foreach (PSystemBody body in rootBody.children)
                        {
                            if (body.orbitDriver != null)
                            {
                                maximumDistance = Math.Max(maximumDistance,
                                    body.orbitDriver.orbit.semiMajorAxis * (1d + body.orbitDriver.orbit.eccentricity));
                            }
                            else
                            {
                                Debug.Log("[Kopernicus]: Body " + body.name + " has no Orbit driver!");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("[Kopernicus]: Root body children null or 0");
                    }
                }
                else
                {
                    Debug.Log("[Kopernicus]: Root body null!");
                }

                if (Templates.MaxViewDistance >= 0)
                {
                    maximumDistance = Templates.MaxViewDistance;
                    Debug.Log("Found max distance override " + maximumDistance);
                }
                else
                {
                    Debug.Log("Found max distance " + maximumDistance);
                }

                PlanetariumCamera.fetch.maxDistance =
                    (Single)maximumDistance * 3.0f / ScaledSpace.Instance.scaleFactor;

                // Call the event
                Events.OnPostFixing.Fire();

                // Flush the logger
                Logger.Default.Flush();

                // Fixups complete, time to surrender to fate
                Destroy(this);
            }
            catch (Exception e)
            {
                // Log the exception
                Debug.LogException(e);

                // Open the Warning popup
                DisplayWarning();
            }
        }

        // Displays a warning if Kopernicus failed to load for some reason
        public static void DisplayWarning()
        {
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "KopernicusFail", "Warning",
                "Kopernicus was not able to load the custom planetary system due to an exception in the loading process.\n" +
                "Loading your saved game is NOT recommended, because the missing planets could corrupt it and delete your progress.\n\n" +
                "Please contact the planet pack author or the Kopernicus team about the issue and send them a valid bug report, including your KSP.log, your ModuleManager.ConfigCache file and the folder Logs/Kopernicus/ from your KSP root directory.\n\n",
                "OK", true, UISkinManager.GetSkin("MainMenuSkin"));
        }

        // Log the destruction of the injector
        public void OnDestroy()
        {
            Logger.Default.Log("Injector.OnDestroy(): Complete");
            Logger.Default.Flush();
        }
    }
    [HarmonyPatch(typeof(MapSO), "ConstructBilinearCoords", new Type[] { typeof(float), typeof(float) })]
    public static class MapSOPPatch_Float
    {
        private static bool Prefix(MapSO __instance, float x, float y)
        {
            if (ReferenceEquals(__instance, Injector.moho_biomes) || ReferenceEquals(__instance, Injector.moho_height))
            {
                return true;
            }
            // X wraps around as it is longitude.
            x = Mathf.Abs(x - Mathf.Floor(x));
            __instance.centerX = x * __instance._width;
            __instance.minX = Mathf.FloorToInt(__instance.centerX);
            __instance.maxX = Mathf.CeilToInt(__instance.centerX);
            __instance.midX = __instance.centerX - __instance.minX;
            if (__instance.maxX == __instance._width)
                __instance.maxX = 0;

            // Y clamps as it is latitude and the poles don't wrap to each other.
            y = Mathf.Clamp(y, 0, 0.99999f);
            __instance.centerY = y * __instance._height;
            __instance.minY = Mathf.FloorToInt(__instance.centerY);
            __instance.maxY = Mathf.CeilToInt(__instance.centerY);
            __instance.midY = __instance.centerY - __instance.minY;
            if (__instance.maxY >= __instance._height)
                __instance.maxY = __instance._height - 1;

            return false;
        }
    }
    [HarmonyPatch(typeof(MapSO), "ConstructBilinearCoords", new Type[] { typeof(double), typeof(double) })]
    public static class MapSOPatch_Double
    {
        private static bool Prefix(MapSO __instance, double x, double y)
        {
            if (ReferenceEquals(__instance, Injector.moho_biomes) || ReferenceEquals(__instance, Injector.moho_height))
            {
                return true;
            }
            // X wraps around as it is longitude.
            x = Math.Abs(x - Math.Floor(x));
            __instance.centerXD = x * __instance._width;
            __instance.minX = (int)Math.Floor(__instance.centerXD);
            __instance.maxX = (int)Math.Ceiling(__instance.centerXD);
            __instance.midX = (float)__instance.centerXD - __instance.minX;
            if (__instance.maxX == __instance._width)
                __instance.maxX = 0;

            // Y clamps as it is latitude and the poles don't wrap to each other.
            y = Math.Min(Math.Max(y, 0), 0.99999);
            __instance.centerYD = y * __instance._height;
            __instance.minY = (int)Math.Floor(__instance.centerYD);
            __instance.maxY = (int)Math.Ceiling(__instance.centerYD);
            __instance.midY = (float)__instance.centerYD - __instance.minY;
            if (__instance.maxY >= __instance._height)
                __instance.maxY = __instance._height - 1;

            return false;
        }
    }
    [HarmonyPatch(typeof(ROCManager), "ValidateCBBiomeCombos")]
    public static class ROCManager_ValidateCBBiomeCombos
    {
        private static Func<ROCManager, string, CelestialBody> ValidCelestialBody;
        private static Func<ROCManager, CelestialBody, string, bool> ValidCBBiome;
        static bool Prefix(ROCManager __instance)
        {
            List<ROCDefinition> rocDefinitions = __instance.rocDefinitions;

            for (int num = rocDefinitions.Count - 1; num >= 0; num--)
            {
                for (int num2 = rocDefinitions[num].myCelestialBodies.Count - 1; num2 >= 0; num2--)
                {
                    CelestialBody celestialBody = ValidCelestialBody(__instance, rocDefinitions[num].myCelestialBodies[num2].name);
                    if (celestialBody.IsNullOrDestroyed())
                    {
                        Debug.LogWarningFormat("[ROCManager]: Invalid CelestialBody Name {0} on ROC Definition {1}. Removed entry.", rocDefinitions[num].myCelestialBodies[num2].name, rocDefinitions[num].type);
                        rocDefinitions[num].myCelestialBodies.RemoveAt(num2);
                        continue; // missing in stock code
                    }
                    else
                    {
                        for (int num3 = rocDefinitions[num].myCelestialBodies[num2].biomes.Count - 1; num3 >= 0; num3--)
                        {
                            if (!ValidCBBiome(__instance, celestialBody, rocDefinitions[num].myCelestialBodies[num2].biomes[num3]))
                            {
                                Debug.LogWarningFormat("[ROCManager]: Invalid Biome Name {0} for Celestial Body {1} on ROC Definition {2}. Removed entry.", rocDefinitions[num].myCelestialBodies[num2].biomes[num3], rocDefinitions[num].myCelestialBodies[num2].name, rocDefinitions[num].type);
                                rocDefinitions[num].myCelestialBodies[num2].biomes.RemoveAt(num3);
                            }
                        }
                    }
                    if (rocDefinitions[num].myCelestialBodies[num2].biomes.Count == 0) // ArgumentOutOfRangeException for myCelestialBodies[num2] when the previous if evaluate to true
                    {
                        Debug.LogWarningFormat("[ROCManager]: No Valid Biomes for Celestial Body {0} on ROC Definition {1}. Removed entry.", rocDefinitions[num].myCelestialBodies[num2].name, rocDefinitions[num].type);
                        rocDefinitions[num].myCelestialBodies.RemoveAt(num2);
                    }
                }
                if (rocDefinitions[num].myCelestialBodies.Count == 0)
                {
                    Debug.LogWarningFormat("[ROCManager]: No Valid Celestial Bodies on ROC Definition {0}. Removed entry.", rocDefinitions[num].type);
                    rocDefinitions.RemoveAt(num);
                }
            }

            return false;
        }
    }
    [HarmonyPatch(typeof(PQSLandControl), "OnVertexBuildHeight")]
    public static class PQSLandControl_OnVertexBuildHeight
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo PQSMod_sphere_field = AccessTools.Field(typeof(PQSMod), nameof(PQSMod.sphere));
            FieldInfo PQS_sx_field = AccessTools.Field(typeof(PQS), nameof(PQS.sx));
            MethodInfo GetLongitudeFromSX_method = AccessTools.Method(typeof(Harmony_Utilities), nameof(Harmony_Utilities.GetLongitudeFromSX));

            List<CodeInstruction> code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count - 1; i++)
            {
                if (code[i].opcode == OpCodes.Ldfld && ReferenceEquals(code[i].operand, PQSMod_sphere_field)
                    && code[i + 1].opcode == OpCodes.Ldfld && ReferenceEquals(code[i + 1].operand, PQS_sx_field))
                {
                    code[i + 1].opcode = OpCodes.Call;
                    code[i + 1].operand = GetLongitudeFromSX_method;
                }
            }

            return code;
        }
    }

    public static class Harmony_Utilities
    {

        /// <summary>
        /// Transform the from the sx [-0.25, 0.75] longitude range convention where [-0.25, 0] maps to [270°, 360°]
        /// and [0, 0.75] maps to [0°, 270°] into a linear [0,1] longitude range.
        /// </summary>
        public static double GetLongitudeFromSX(PQS sphere)
        {
            if (sphere.sx < 0.0)
                return sphere.sx + 1.0;
            return sphere.sx;
        }
    }
}
