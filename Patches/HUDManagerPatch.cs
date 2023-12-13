using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TMPro;
using Unity.Netcode;
using Unity.Networking.Transport.Error;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace Spectators.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        // Spectator Box UI Text Element
        private static TextMeshProUGUI specBox;
        // Last Spectated Player ID
        public static int lastPlayerID = -100;
        // Spectators
        public static List<string> specList = new List<string> { };
        // Started Watching Key 
        public const string key1 = "iswatching69";
        // Stopped Watching Key
        public const string key2 = "stoppedwatching69";
        // Spectators Header Color
        public static string headerColor = "#e36d19";
        // Spectators Name Color
        public static string specColor = "#3d52a1";
        // HUDManager Instance
        public static HUDManager hud = HUDManager.Instance;
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(" SpectatorDebug");

        // Sends spectating messages to spectated player
        [HarmonyPostfix]
        [HarmonyPatch("SetSpectatingTextToPlayer")]
        static void patchSetSpectatingTextToPlayer(ref PlayerControllerB playerScript)
        {
            string localPlayer = hud.playersManager.localPlayerController.playerUsername;
            int playerId = -10; ;
            // Get playerUsername from playerId
            for (int i = 0; i < hud.playersManager.allPlayerScripts.Length; i++)
            {
                if (hud.playersManager.allPlayerScripts[i].playerUsername == playerScript.playerUsername)
                {
                    playerId = i;
                }
            }
            if (playerId == lastPlayerID)
            {
                return;
            }
            else
            {
                // Send Start and Stop spectating commands
                if (lastPlayerID != -100)
                {
                    mls.LogInfo("Sending Spec Stop to :" + playerScript.playerUsername);
                    hud.AddTextToChatOnServer(localPlayer + key2, lastPlayerID);
                }
                lastPlayerID = playerId;
                mls.LogInfo("Sending Spec Start to :" + playerScript.playerUsername);
                hud.AddTextToChatOnServer(localPlayer + key1, playerId);
            }
        }

        // Filter Spectator chat messages
        [HarmonyPrefix]
        [HarmonyPatch("AddChatMessage")]
        static bool patchSpectatorsClientChat(ref string chatMessage, ref string nameOfUserWhoTyped)
        {
            int chatLength = chatMessage.Length;
            // Spectator Commands Filter
            if (chatMessage.Contains(key1) || chatMessage.Contains(key2))
            {
                // User Filter
                if (nameOfUserWhoTyped != hud.playersManager.localPlayerController.playerUsername)
                {
                    hud.lastChatMessage = chatMessage;
                    return false;
                }
                // Local User - Add Spectator
                else if (chatMessage.Contains(key1))
                {
                    specList.Add(chatMessage.Substring(0, chatLength - (key1.Length)));
                    mls.LogInfo("Adding Spectator: " + chatMessage.Substring(0, chatLength - (key1.Length)));
                    hud.lastChatMessage = chatMessage;
                    return false;
                }
                // Local User - Remove Spectator
                else 
                {
                    specList.Remove(chatMessage.Substring(0, chatLength - (key2.Length)));
                    mls.LogInfo("Removing Spectator: " + chatMessage.Substring(0, chatLength - (key2.Length)));
                    hud.lastChatMessage = chatMessage;
                    return false;
                }
            }
            // Change Spectators and Header Color
            else if (chatMessage.Contains("/specColor") || chatMessage.Contains("/headerColor"))
            {
                
                if (chatMessage.Contains('#'))
                {
                    if (chatMessage.Contains("/specColor"))
                    {
                        string tempColor = chatMessage.Substring(11, chatLength-11);
                        specColor = tempColor;
                    }
                    else 
                    {
                        string tempColor = chatMessage.Substring(13, chatLength-13);
                        headerColor = tempColor;
                    }
                    
                }
                hud.lastChatMessage = chatMessage;
                return false;
            }
            // Spontaneous Death
            else if (chatMessage == "/kill512")
            {
                hud.localPlayer.KillPlayer(default(Vector3), true, (CauseOfDeath)0, 0);
                hud.lastChatMessage = chatMessage;
                return false;
            }
            // Message Passthrough
            else
            {
                return true;
            }
        }

        // Update Spectator List
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        static void modifyWeightText() 
        {
            // Spectator Display Toggle
            if (specBox == null) 
            { 
                return; 
            }
            else
            {
                ColorUtility.TryParseHtmlString(specColor, out Color textColor);
                specBox.color = textColor;
                specBox.text = "<color=" + headerColor + ">" + "Spectators:</color>\n";
                if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                {

                    specList.RemoveRange(0, specList.Count);
                    specBox.text = "";
                    return;
                }
                // Populate Spectator List
                for (int i = 0; i < specList.Count; i++)
                {
                    string specText = specBox.text;
                    specBox.text = string.Format(specText + "<size=6f><color=" + specColor + "> {0}</color></size>\n", nameSpacer(specList[i]));
                }
            }
        }

        // Clear Spectator List after round
        [HarmonyPostfix]
        [HarmonyPatch("HideHUD")]
        static void clearSpecEndRound() {
            mls.LogInfo("Look out sweepers!");
            specList.RemoveRange(0, specList.Count);
        }

        // Adds spectator list to UI elements 
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        static void createSpecElem(ref HUDManager __instance) 
        {
            GameObject spec = new GameObject("SpectatorList");
            spec.AddComponent<RectTransform>();
            TextMeshProUGUI specBoxTemp = spec.AddComponent<TextMeshProUGUI>();
            RectTransform rectTransform = specBoxTemp.rectTransform;
            rectTransform.SetParent(__instance.PTTIcon.transform, false);
            rectTransform.anchoredPosition = new Vector2(460f, -90f);
            specBoxTemp.font = __instance.controlTipLines[0].font;
            specBoxTemp.fontSize = 7f;
            specBoxTemp.enabled = true;
            specBoxTemp.color = Color.white;
            specBoxTemp.text = "Spectators: \n";
            specBoxTemp.overflowMode = (TextOverflowModes)0;
            specBox = specBoxTemp;
            mls.LogInfo("Spectator UI Element Loaded...");
        }

        // Give the man some space to breathe
        static string nameSpacer(string name)
        {
            string tempName = name;
            int nameWidth = 18;
            if(name.Length<nameWidth)
            {
                for (int i = 0; i < (nameWidth - name.Length); i++) 
                {
                    tempName += " ";
                }
            }
            return tempName;
        }

    }
}
