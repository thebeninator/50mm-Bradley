using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bradley50mm;
using GHPC.Camera;
using GHPC.Equipment.Optics;
using GHPC.Player;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using UnityEngine;
using GHPC.Effects;
using HarmonyLib;
using System.Threading.Tasks;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityAnimator;
using GHPC;
using GHPC.Utility;
using static MelonLoader.MelonLogger;
using GHPC.State;
using System.Collections;

[assembly: MelonInfo(typeof(Bradley50mmMod), "50mm Bradley", "2.0.4A", "ATLAS")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace Bradley50mm
{
    public class Bradley50mmMod : MelonMod
    {
        public static Vehicle[] vics;
        public static GameObject game_manager;
        public static CameraManager cam_manager;
        public static PlayerInput player_manager;
        public IEnumerator GetVics(GameState _)
        {
            vics = GameObject.FindObjectsByType<Vehicle>(FindObjectsSortMode.None);

            yield break;
        }

        public override void OnLateUpdate()
        {
            Bradley50mm.LateUpdate();
        }

        public override void OnSceneWasLoaded(int idx, string scene_name)
        {
            if (Util.menu_screens.Contains(scene_name)) return;

            game_manager = GameObject.Find("_APP_GHPC_");
            cam_manager = game_manager.GetComponent<CameraManager>();
            player_manager = game_manager.GetComponent<PlayerInput>();

            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(GetVics), GameStatePriority.Medium);

            Bradley50mm.Init();
        }
    }
}
