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
using HarmonyLib;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityAnimator;

[assembly: MelonInfo(typeof(Bradley50mmMod), "50mm Bradley", "1.0.0", "ATLAS")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace Bradley50mm
{
    public class Bradley50mmMod : MelonMod
    {
        GameObject[] vic_gos;
        GameObject gameManager;
        CameraManager cameraManager;
        PlayerInput playerManager;

        WeaponSystemCodexScriptable gun_xm913;

        AmmoClipCodexScriptable clip_codex_xm1024;
        AmmoType.AmmoClip clip_xm1024;
        AmmoCodexScriptable ammo_codex_xm1024;
        static AmmoType ammo_xm1024;

        AmmoClipCodexScriptable clip_codex_xm1023;
        AmmoType.AmmoClip clip_xm1023;
        AmmoCodexScriptable ammo_codex_xm1023;
        static AmmoType ammo_xm1023;

        AmmoType ammo_m791;
        AmmoType ammo_m792;

        // https://snipplr.com/view/75285/clone-from-one-object-to-another-using-reflection
        public static void ShallowCopy(System.Object dest, System.Object src)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] destFields = dest.GetType().GetFields(flags);
            FieldInfo[] srcFields = src.GetType().GetFields(flags);

            foreach (FieldInfo srcField in srcFields)
            {
                FieldInfo destField = destFields.FirstOrDefault(field => field.Name == srcField.Name);

                if (destField != null && !destField.IsLiteral)
                {
                    if (srcField.FieldType == destField.FieldType)
                        destField.SetValue(dest, srcField.GetValue(src));
                }
            }
        }

        public static void EmptyRack(GHPC.Weapons.AmmoRack rack)
        {
            MethodInfo removeVis = typeof(GHPC.Weapons.AmmoRack).GetMethod("RemoveAmmoVisualFromSlot", BindingFlags.Instance | BindingFlags.NonPublic);

            PropertyInfo stored_clips = typeof(GHPC.Weapons.AmmoRack).GetProperty("StoredClips");
            stored_clips.SetValue(rack, new List<AmmoType.AmmoClip>());

            rack.SlotIndicesByAmmoType = new Dictionary<AmmoType, List<byte>>();

            foreach (Transform transform in rack.VisualSlots)
            {
                AmmoStoredVisual vis = transform.GetComponentInChildren<AmmoStoredVisual>();

                if (vis != null && vis.AmmoType != null)
                {
                    removeVis.Invoke(rack, new object[] { transform });
                }
            }
        }

        public override void OnInitializeMelon()
        {
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu2_Scene" || sceneName == "LOADER_MENU" || sceneName == "LOADER_INITIAL") return;

            vic_gos = GameObject.FindGameObjectsWithTag("Vehicle");
            gameManager = GameObject.Find("_APP_GHPC_");
            cameraManager = gameManager.GetComponent<CameraManager>();
            playerManager = gameManager.GetComponent<PlayerInput>();

            if (gun_xm913 == null)
            {
                foreach (AmmoCodexScriptable s in Resources.FindObjectsOfTypeAll(typeof(AmmoCodexScriptable)))
                {
                    if (s.AmmoType.Name == "25mm APDS-T M791") ammo_m791 = s.AmmoType;
                    if (s.AmmoType.Name == "25mm HEI-T M792") ammo_m792 = s.AmmoType;
                }

                // xm913
                gun_xm913 = ScriptableObject.CreateInstance<WeaponSystemCodexScriptable>();
                gun_xm913.name = "gun_xm913";
                gun_xm913.CaliberMm = 50;
                gun_xm913.FriendlyName = "50mm cannon M913";
                gun_xm913.Type = WeaponSystemCodexScriptable.WeaponType.Autocannon;

                // xm1023 
                ammo_xm1023 = new AmmoType();
                ShallowCopy(ammo_xm1023, ammo_m791);
                ammo_xm1023.Name = "M1023 APFSDS-T";
                ammo_xm1023.Caliber = 50;
                ammo_xm1023.RhaPenetration = 150f;
                ammo_xm1023.MuzzleVelocity = 1522f;
                ammo_xm1023.VisualType = LiveRoundMarshaller.LiveRoundVisualType.Shell;
                ammo_xm1023.Mass = 0.550f;

                ammo_codex_xm1023 = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
                ammo_codex_xm1023.AmmoType = ammo_xm1023;
                ammo_codex_xm1023.name = "ammo_xm1023";

                clip_xm1023 = new AmmoType.AmmoClip();
                clip_xm1023.Capacity = 40;
                clip_xm1023.Name = "M1023 APFSDS-T";
                clip_xm1023.MinimalPattern = new AmmoCodexScriptable[1];
                clip_xm1023.MinimalPattern[0] = ammo_codex_xm1023;

                clip_codex_xm1023 = ScriptableObject.CreateInstance<AmmoClipCodexScriptable>();
                clip_codex_xm1023.name = "clip_xm1023";
                clip_codex_xm1023.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
                clip_codex_xm1023.CompatibleWeaponSystems[0] = gun_xm913;
                clip_codex_xm1023.ClipType = clip_xm1023;

                // xm1024
                ammo_xm1024 = new AmmoType();
                ShallowCopy(ammo_xm1024, ammo_m792);
                ammo_xm1024.Name = "M1024 HEAB-T";
                ammo_xm1024.Caliber = 50;
                ammo_xm1024.RhaPenetration = 5f;
                ammo_xm1024.MuzzleVelocity = 990f;
                ammo_xm1024.TntEquivalentKg = 0.520f;
                ammo_xm1024.MaxSpallRha = 55f;
                ammo_xm1024.MinSpallRha = 5f;
                ammo_xm1024.ImpactFuseTime = 0f;
                ammo_xm1024.VisualType = LiveRoundMarshaller.LiveRoundVisualType.Shell;
                ammo_xm1024.Mass = 0.750f;

                ammo_codex_xm1024 = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
                ammo_codex_xm1024.AmmoType = ammo_xm1024;
                ammo_codex_xm1024.name = "ammo_xm1024";

                clip_xm1024 = new AmmoType.AmmoClip();
                clip_xm1024.Capacity = 120;
                clip_xm1024.Name = "M1024 HEAB-T";
                clip_xm1024.MinimalPattern = new AmmoCodexScriptable[1];
                clip_xm1024.MinimalPattern[0] = ammo_codex_xm1024;

                clip_codex_xm1024 = ScriptableObject.CreateInstance<AmmoClipCodexScriptable>();
                clip_codex_xm1024.name = "clip_xm1024";
                clip_codex_xm1024.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
                clip_codex_xm1024.CompatibleWeaponSystems[0] = gun_xm913;
                clip_codex_xm1024.ClipType = clip_xm1024;
            }

            foreach (GameObject vic_go in vic_gos)
            {
                Vehicle vic = vic_go.GetComponent<Vehicle>();

                if (vic == null) continue;
                if (vic.FriendlyName != "M2 Bradley") continue;

                string name = "M2(50) Bradley"; 

                FieldInfo friendlyName = typeof(GHPC.Unit).GetField("_friendlyName", BindingFlags.NonPublic | BindingFlags.Instance);
                friendlyName.SetValue(vic, name);

                WeaponsManager weaponsManager = vic.GetComponent<WeaponsManager>();
                WeaponSystemInfo mainGunInfo = weaponsManager.Weapons[0];
                WeaponSystem mainGun = mainGunInfo.Weapon;

                mainGunInfo.Name = "50mm gun M913";
                mainGun.Impulse = 1450;
                mainGun.RecoilBlurMultiplier = 1.7f;
                mainGun.BaseDeviationAngle = 0.035f;

                FieldInfo codex = typeof(WeaponSystem).GetField("CodexEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                codex.SetValue(mainGun, gun_xm913);

                GameObject gunTube = vic_go.transform.Find("M2BRADLEY_rig/HULL/Turret/Mantlet/Main gun").gameObject;
                gunTube.transform.localPosition = new Vector3(0.0825f, 0.0085f, 3.2858f);
                gunTube.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

                GameObject gunTubeStart = vic_go.transform.Find("M2BRADLEY_rig/HULL/Turret/Mantlet/bushmaster start").gameObject;
                gunTubeStart.transform.localPosition = new Vector3(0.0825f, 0.0085f, 3.2858f);

                GameObject gunTubeEnd = vic_go.transform.Find("M2BRADLEY_rig/HULL/Turret/Mantlet/bushmaster end").gameObject;
                gunTubeEnd.transform.localPosition = new Vector3(0.0825f, 0.0085f, 3.0306f);

                // more powah
                Transform muzzleFlashes = mainGun.MuzzleEffects[0].transform;
                muzzleFlashes.localPosition = new Vector3(0.0f, 0.0f, 1.0009f);

                // front
                muzzleFlashes.GetChild(1).transform.localScale = new Vector3(5f, 5f, 5f);
                //brake left 
                muzzleFlashes.GetChild(8).transform.localScale = new Vector3(4f, 1f, 2.5f);
                //brake right
                muzzleFlashes.GetChild(9).transform.localScale = new Vector3(4f, 1f, 2.5f);


                FieldInfo fixParallaxField = typeof(FireControlSystem).GetField("_fixParallaxForVectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
                fixParallaxField.SetValue(mainGun.FCS, true);
                mainGun.FCS.MaxLaserRange = 4000;
                mainGun.WeaponSound.SingleShotEventPaths[0] = "event:/Weapons/canon_73mm-2A28Grom";

                mainGun.SetCycleTime(0.55f);
                //mainGun.SetCycleTime(0.10f);


                LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();

                loadoutManager.LoadedAmmoTypes = new AmmoClipCodexScriptable[] {clip_codex_xm1023, clip_codex_xm1024};

                for (int i = 0; i <= 1; i++)
                {
                    GHPC.Weapons.AmmoRack rack = loadoutManager.RackLoadouts[i].Rack;
                    loadoutManager.RackLoadouts[i].OverrideInitialClips = new AmmoClipCodexScriptable[] {clip_codex_xm1023, clip_codex_xm1024};
                    rack.ClipTypes = new AmmoType.AmmoClip[] {clip_xm1023, clip_xm1024};
                    EmptyRack(rack);
                }

                loadoutManager.SpawnCurrentLoadout();

                PropertyInfo roundInBreech = typeof(AmmoFeed).GetProperty("AmmoTypeInBreech"); 
                roundInBreech.SetValue(mainGun.Feed, null);

                MethodInfo refreshBreech = typeof(AmmoFeed).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
                refreshBreech.Invoke(mainGun.Feed, new object[] {});

                // update ballistics computer
                MethodInfo registerAllBallistics = typeof(LoadoutManager).GetMethod("RegisterAllBallistics", BindingFlags.Instance | BindingFlags.NonPublic);
                registerAllBallistics.Invoke(loadoutManager, new object[] {});
            }
        }

        [HarmonyPatch(typeof(GHPC.Weapons.LiveRound), "Start")]
        public static class Airburst {
            private static void Postfix(GHPC.Weapons.LiveRound __instance) 
            {
                if (__instance.Info.Name != "M1024 HEAB-T") return;
              
                FieldInfo rangedFuseTimeField = typeof(GHPC.Weapons.LiveRound).GetField("_rangedFuseCountdown", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo rangedFuseTimeActiveField = typeof(GHPC.Weapons.LiveRound).GetField("_rangedFuseActive", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo ballisticsComputerField = typeof(FireControlSystem).GetField("_bc", BindingFlags.Instance | BindingFlags.NonPublic);

                FireControlSystem FCS = __instance.Shooter.WeaponsManager.Weapons[0].FCS;
                BallisticComputerRepository bc = ballisticsComputerField.GetValue(FCS) as BallisticComputerRepository;

                //funky math 
                rangedFuseTimeField.SetValue(__instance, bc.GetFlightTime(ammo_xm1024, FCS.CurrentRange + FCS.CurrentRange / 990f * 2 + 19f + FCS.CurrentRange/2000f));
                rangedFuseTimeActiveField.SetValue(__instance, true);
            }
        }
    }
}