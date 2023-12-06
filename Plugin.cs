using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Spectators.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Spectators
{
    [BepInPlugin(modGUID,modName,modVersion)]
    public class SpectatorsModBase : BaseUnityPlugin
    {
        private const string modGUID = "00Spectators00";
        private const string modName = "Spectators";
        private const string modVersion = "1.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);
        private static SpectatorsModBase instance;
        private static ManualLogSource mls;

        void Awake()
        {
            if (instance == null) { instance = this; }
            harmony.PatchAll();
            mls = BepInEx.Logging.Logger.CreateLogSource(" "+modGUID);
            mls.LogInfo("-- Spectators -- The eyes are watching...");


        }

    }


}
