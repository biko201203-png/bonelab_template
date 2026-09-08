using System;
using System.Collections.Generic;
using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.Elements;
using LabFusion.Network;
using LabFusion.Representation;
using TMPro;
using UnityEngine;

[assembly: MelonInfo(typeof(FusionStatsNametags.MainClass), "Fusion Stats Nametags", "1.0.0", "YourName")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace FusionStatsNametags
{
    public class MainClass : MelonMod
    {
        public static bool IsEnabled = true;
        private static MenuCategory _menuCat;
        private static readonly Dictionary<byte, TextMeshPro> ActiveNametags = new Dictionary<byte, TextMeshPro>();

        public override void OnInitializeMelon()
        {
            SetupBoneMenu();
            PlayerIdManager.OnPlayerIdSubscribed += OnPlayerJoined;
            PlayerIdManager.OnPlayerIdUnsubscribed += OnPlayerLeft;
        }

        private void SetupBoneMenu()
        {
            _menuCat = MenuManager.CreateCategory("Stats Nametags", Color.cyan);
            _menuCat.CreateBoolElement("Enable Overhead Stats", Color.white, IsEnabled, (val) =>
            {
                IsEnabled = val;
                if (!IsEnabled) ClearAllNametags();
            });
        }

        private void OnPlayerJoined(PlayerId playerId)
        {
            if (playerId.IsMe) return;
            MelonCoroutines.Start(CreateNametagCoroutine(playerId));
        }

        private System.Collections.IEnumerator CreateNametagCoroutine(PlayerId playerId)
        {
            yield return new WaitForSeconds(1.5f);

            if (!PlayerRepManager.TryGetPlayerRep(playerId, out var playerRep) || playerRep.RigManager == null)
                yield break;

            Transform headTransform = playerRep.RigManager.physicsRig.m_head;
            if (headTransform == null) yield break;

            GameObject textObj = new GameObject($"StatsUI_{playerId.SmallId}");
            textObj.transform.SetParent(headTransform, false);
            textObj.transform.localPosition = new Vector3(0, 0.4f, 0);

            TextMeshPro tmPro = textObj.AddComponent<TextMeshPro>();
            tmPro.alignment = TextAlignmentOptions.Center;
            tmPro.fontSize = 2.5f;
            tmPro.color = Color.green;
            tmPro.text = "Loading stats...";

            ActiveNametags[playerId.SmallId] = tmPro;
        }

        private void OnPlayerLeft(PlayerId playerId)
        {
            if (ActiveNametags.ContainsKey(playerId.SmallId))
            {
                if (ActiveNametags[playerId.SmallId] != null)
                {
                    GameObject.Destroy(ActiveNametags[playerId.SmallId].gameObject);
                }
                ActiveNametags.Remove(playerId.SmallId);
            }
        }

        public override void OnUpdate()
        {
            if (!IsEnabled || ActiveNametags.Count == 0) return;

            Transform activeCam = BoneLib.Player.playerHead;
            if (activeCam == null) return;

            foreach (var kvp in ActiveNametags)
            {
                byte smallId = kvp.Key;
                TextMeshPro tmPro = kvp.Value;

                if (tmPro == null) continue;

                if (PlayerIdManager.TryGetPlayerId(smallId, out var playerId) && 
                    PlayerRepManager.TryGetPlayerRep(playerId, out var playerRep) && playerRep.RigManager != null)
                {
                    tmPro.transform.LookAt(activeCam.position);
                    tmPro.transform.Rotate(0, 180, 0);

                    string username = playerRep.IsValidated ? playerRep.PlayerId.Username : "Connecting...";
                    float health = playerRep.RigManager.health._health;

                    tmPro.text = $"{username}\n<color=red>HP: {Mathf.CeilToInt(health)}</color>";
                }
            }
        }

        private void ClearAllNametags()
        {
            foreach (var tmPro in ActiveNametags.Values)
            {
                if (tmPro != null) GameObject.Destroy(tmPro.gameObject);
            }
            ActiveNametags.Clear();
        }
    }
}
