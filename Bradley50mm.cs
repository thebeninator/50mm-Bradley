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

[assembly: MelonInfo(typeof(Bradley50mmMod), "50mm Bradley", "2.0.3", "ATLAS")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace Bradley50mm
{
    public struct LockOnData
    {
        public LockOnData(Vehicle target, MissileGuidanceUnit unit)
        {
            Target = target;
            Unit = unit;
        }

        public Vehicle Target { get; set; }
        public MissileGuidanceUnit Unit { get; set; }
    }

    public class Bradley50mmMod : MelonMod
    {
        GameObject[] vic_gos;
        GameObject gameManager;
        CameraManager cameraManager;
        static PlayerInput playerManager;

        WeaponSystemCodexScriptable gun_xm913;

        AmmoClipCodexScriptable clip_codex_xm1024;
        AmmoType.AmmoClip clip_xm1024;
        AmmoCodexScriptable ammo_codex_xm1024;
        static AmmoType ammo_xm1024;

        AmmoClipCodexScriptable clip_codex_xm1023;
        AmmoType.AmmoClip clip_xm1023;
        AmmoCodexScriptable ammo_codex_xm1023;
        static AmmoType ammo_xm1023;

        AmmoClipCodexScriptable clip_codex_TOW_FF;
        AmmoType.AmmoClip clip_TOW_FF;
        AmmoCodexScriptable ammo_codex_TOW_FF;
        static AmmoType ammo_TOW_FF;

        AmmoType ammo_m791;
        AmmoType ammo_m792;
        AmmoType ammo_I_TOW;

        static Dictionary<int, LockOnData> locked_on_targets = new Dictionary<int, LockOnData>();

        public static void UpdateLockText(FireControlSystem fcs, string text) {
            GameObject lock_text_optic;
            GameObject lock_text_optic_night;


            if (fcs.MainOptic.slot.IsLinkedNightSight)
            {
                lock_text_optic = fcs.MainOptic.slot.LinkedDaySight.transform.GetChild(3).GetChild(2).GetChild(1).GetChild(1).gameObject;
                lock_text_optic_night = fcs.MainOptic.transform.GetChild(0).GetChild(2).GetChild(1).GetChild(1).gameObject;
            }
            else
            {
                lock_text_optic = fcs.MainOptic.transform.GetChild(3).GetChild(2).GetChild(1).GetChild(1).gameObject;
                lock_text_optic_night = fcs.MainOptic.slot.LinkedNightSight.transform.GetChild(0).GetChild(2).GetChild(1).GetChild(1).gameObject;
            }

            lock_text_optic.GetComponent<TMPro.TextMeshProUGUI>().text = text;
            lock_text_optic_night.GetComponent<TMPro.TextMeshProUGUI>().text = text;
        }

        public static void ResetGuidance(MissileGuidanceUnit unit, FireControlSystem fcs)
        {
            unit.transform.localPosition = new Vector3(-1.1509f, 0.5546f, 0.0471f);
            unit.transform.localEulerAngles = new Vector3(0.1569f, 359.86f, 0f);
            LockOnData data = locked_on_targets[fcs.GetInstanceID()];
            data.Target = null;
            locked_on_targets[fcs.GetInstanceID()] = data;

            UpdateLockText(fcs, "NO LOCK");
        }

        public static void SetTarget(FireControlSystem fcs, Vehicle target) {
            if (fcs == null) return;
            if (target == null) return;

            LockOnData data = locked_on_targets[fcs.GetInstanceID()];
            data.Target = target;
            locked_on_targets[fcs.GetInstanceID()] = data;

            UpdateLockText(fcs, "LOCK");
        }

        // if you're wondering, yes: this is literally just teleporting the guidance computer over the target 
        // and telling it to look down
        public override void OnLateUpdate() {
            foreach (KeyValuePair<int, LockOnData> t in locked_on_targets)
            {
                Vehicle vic = t.Value.Target;
                MissileGuidanceUnit unit = t.Value.Unit;

                if (vic == null) continue;

                Vector3 loc = vic.transform.position;
                loc.y = vic.transform.position.y + 120f;
                unit.transform.position = loc;
                unit.transform.LookAt(vic.transform);
            }
        }

        public override async void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu2_Scene" || sceneName == "LOADER_MENU" || sceneName == "LOADER_INITIAL" || sceneName == "t64_menu") return;

            vic_gos = GameObject.FindGameObjectsWithTag("Vehicle");

            while (vic_gos.Length == 0) 
            {
                vic_gos = GameObject.FindGameObjectsWithTag("Vehicle");
                await Task.Delay(1);
            }

            await Task.Delay(3000);

            gameManager = GameObject.Find("_APP_GHPC_");

            cameraManager = gameManager.GetComponent<CameraManager>();
            playerManager = gameManager.GetComponent<PlayerInput>();

            if (gun_xm913 == null)
            {
                foreach (AmmoCodexScriptable s in Resources.FindObjectsOfTypeAll(typeof(AmmoCodexScriptable)))
                {
                    if (s.AmmoType.Name == "25mm APDS-T M791") ammo_m791 = s.AmmoType;
                    if (s.AmmoType.Name == "25mm HEI-T M792") ammo_m792 = s.AmmoType;
                    if (s.AmmoType.Name == "BGM-71C I-TOW") ammo_I_TOW = s.AmmoType;
                }

                // xm913
                gun_xm913 = ScriptableObject.CreateInstance<WeaponSystemCodexScriptable>();
                gun_xm913.name = "gun_xm913";
                gun_xm913.CaliberMm = 50;
                gun_xm913.FriendlyName = "50mm cannon M913";
                gun_xm913.Type = WeaponSystemCodexScriptable.WeaponType.Autocannon;

                // xm1023 
                ammo_xm1023 = new AmmoType();
                Util.ShallowCopy(ammo_xm1023, ammo_m791);
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
                Util.ShallowCopy(ammo_xm1024, ammo_m792);
                ammo_xm1024.Name = "M1024 HEAB-T";
                ammo_xm1024.Caliber = 50;
                ammo_xm1024.RhaPenetration = 5f;
                ammo_xm1024.MuzzleVelocity = 990f;
                ammo_xm1024.TntEquivalentKg = 0.520f;
                ammo_xm1024.MaxSpallRha = 30f;
                ammo_xm1024.MinSpallRha = 10f;
                ammo_xm1024.ImpactFuseTime = 0.005f;
                ammo_xm1024.ImpactTypeFuzed = ParticleEffectsManager.EffectVisualType.AutocannonImpactExplosive;
                ammo_xm1024.ImpactTypeFuzedTerrain = ParticleEffectsManager.EffectVisualType.AutocannonImpactExplosiveTerrain;
                ammo_xm1024.ImpactTypeUnfuzed = ParticleEffectsManager.EffectVisualType.AutocannonImpactExplosive;
                ammo_xm1024.ImpactTypeUnfuzedTerrain = ParticleEffectsManager.EffectVisualType.AutocannonImpactExplosiveTerrain;
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

                // TOW-FF
                ammo_TOW_FF = new AmmoType();
                Util.ShallowCopy(ammo_TOW_FF, ammo_I_TOW);
                ammo_TOW_FF.Name = "BGM-71C I-TOW-FF";
                ammo_TOW_FF.TntEquivalentKg = 1.5f;
                ammo_TOW_FF.SpallMultiplier = 1.5f;
                ammo_TOW_FF.Tandem = true;
                ammo_TOW_FF.ClimbAngle = 20f;
                ammo_TOW_FF.TurnSpeed = 2.5f;
                ammo_TOW_FF.DiveAngle = 45F;
                ammo_TOW_FF.LoiterAltitude = 5000f;
                ammo_TOW_FF.AimPointMarch = 0.05f;
                ammo_TOW_FF.MaxSpallRha = 55f;
                ammo_TOW_FF.MinSpallRha = 20f;
                //ammo_TOW_FF.MuzzleVelocity = 140f;
                //ammo_TOW_FF.SphericalSpall = true;
                ammo_TOW_FF.RangedFuseTime = 20f;
                ammo_TOW_FF.UseTracer = false;
                ammo_TOW_FF.EdgeSetback = 0.5f;
                ammo_TOW_FF.Guidance = AmmoType.GuidanceType.Laser;

                ammo_codex_TOW_FF = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
                ammo_codex_TOW_FF.AmmoType = ammo_TOW_FF;
                ammo_codex_TOW_FF.name = "ammo_TOW_FF";

                clip_TOW_FF = new AmmoType.AmmoClip();
                clip_TOW_FF.Capacity = 2;
                clip_TOW_FF.Name = "BGM-71C I-TOW-FF";
                clip_TOW_FF.MinimalPattern = new AmmoCodexScriptable[1];
                clip_TOW_FF.MinimalPattern[0] = ammo_codex_TOW_FF;

                clip_codex_TOW_FF = ScriptableObject.CreateInstance<AmmoClipCodexScriptable>();
                clip_codex_TOW_FF.name = "clip_TOW_FF";
                clip_codex_TOW_FF.ClipType = clip_TOW_FF;
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

                WeaponSystemInfo towGunInfo = weaponsManager.Weapons[1];
                WeaponSystem towGun = towGunInfo.Weapon;

                mainGunInfo.Name = "50mm gun M913";
                mainGun.Impulse = 1450;
                mainGun.RecoilBlurMultiplier = 1.55f;
                mainGun.BaseDeviationAngle = 0.030f;
                mainGun.FCS.MaxLaserRange = 4000;
                mainGun.WeaponSound.SingleShotEventPaths[0] = "event:/Weapons/canon_73mm-2A28Grom";

                FieldInfo codex = typeof(WeaponSystem).GetField("CodexEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                codex.SetValue(mainGun, gun_xm913);

                GameObject gunTube = vic_go.transform.Find("M2BRADLEY_rig/HULL/Turret/Mantlet/Main gun").gameObject;
                gunTube.transform.localPosition = new Vector3(0.0825f, 0.0085f, 3f);
                gunTube.transform.localScale = new Vector3(1.8f, 1.8f, 1.6f);

                GameObject gunTubeStart = vic_go.transform.Find("M2BRADLEY_rig/HULL/Turret/Mantlet/bushmaster start").gameObject;
                gunTubeStart.transform.localPosition = new Vector3(0.0825f, 0.0085f, 3f);

                GameObject gunTubeEnd = vic_go.transform.Find("M2BRADLEY_rig/HULL/Turret/Mantlet/bushmaster end").gameObject;
                gunTubeEnd.transform.localPosition = new Vector3(0.0825f, 0.0085f, 2.8f);

                // more powah
                Transform muzzleFlashes = mainGun.MuzzleEffects[0].transform;
                muzzleFlashes.localPosition = new Vector3(0.0f, 0.0f, 1.0005f);

                // front
                muzzleFlashes.GetChild(1).transform.localScale = new Vector3(10f, 10f, 10f);
                //brake left 
                muzzleFlashes.GetChild(8).transform.localScale = new Vector3(4f, 1f, 2.5f);
                //brake right
                muzzleFlashes.GetChild(9).transform.localScale = new Vector3(4f, 1f, 2.5f);

                FieldInfo fixParallaxField = typeof(FireControlSystem).GetField("_fixParallaxForVectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
                fixParallaxField.SetValue(mainGun.FCS, true);

                UsableOptic optic = Util.GetDayOptic(mainGun.FCS);
                UsableOptic night_optic = optic.slot.LinkedNightSight.PairedOptic;
                optic.RotateAzimuth = true;
                optic.slot.LinkedNightSight.PairedOptic.RotateAzimuth = true;
                optic.transform.GetChild(2).transform.localPosition = new Vector3(2.8227f, 2.7418f, 0f);
                night_optic.transform.GetChild(2).transform.localPosition = new Vector3(2.8227f, 2.7418f, 0f);
                night_optic.transform.GetChild(1).GetChild(1).transform.localPosition = new Vector3(2.8227f, 2.7418f, 0f);

                var lock_text = GameObject.Instantiate(optic.transform.GetChild(3).GetChild(2).GetChild(1).gameObject);
                lock_text.AddComponent<Reparent>();
                lock_text.GetComponent<Reparent>().NewParent = optic.transform.GetChild(3).GetChild(2).GetChild(1).transform;
                typeof(Reparent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(lock_text.GetComponent<Reparent>(), new object[] {});

                lock_text.transform.localPosition = new Vector3(91.1828f, 0f, 0f);
                lock_text.transform.localScale = new Vector3(1f, 1f, 1f);
                lock_text.GetComponent<TMPro.TextMeshProUGUI>().text = "NO LOCK";
                lock_text.SetActive(true);

                var lock_text_flir = GameObject.Instantiate(night_optic.transform.GetChild(0).GetChild(2).GetChild(1).gameObject);
                lock_text_flir.AddComponent<Reparent>();
                lock_text_flir.GetComponent<Reparent>().NewParent = night_optic.transform.GetChild(0).GetChild(2).GetChild(1).transform;
                typeof(Reparent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(lock_text_flir.GetComponent<Reparent>(), new object[] { });

                lock_text_flir.transform.localPosition = new Vector3(91.1828f, 0f, 0f);
                lock_text_flir.transform.localScale = new Vector3(1f, 1f, 1f);
                lock_text_flir.GetComponent<TMPro.TextMeshProUGUI>().text = "NO LOCK";
                lock_text_flir.SetActive(true);

                locked_on_targets.Add(mainGun.FCS.GetInstanceID(), new LockOnData(null, towGun.GuidanceUnit));
                towGun.TriggerHoldTime = 4f;
                towGun.MaxSpeedToFire = 999f;
                towGun.MaxSpeedToDeploy = 999f;
                vic.AimablePlatforms[2].ForcedStowSpeed = 999f;

                mainGun.SetCycleTime(0.35f);
                //mainGun.SetCycleTime(0.10f);

                LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();

                GHPC.Weapons.AmmoRack towRack = towGun.Feed.ReadyRack;
                towRack.ClipTypes[0] = clip_TOW_FF;

                for (int i = 0; i <= 3; i++)
                {
                    towRack.StoredClips[i] = clip_TOW_FF;
                }

                loadoutManager.LoadedAmmoTypes = new AmmoClipCodexScriptable[] {clip_codex_xm1023, clip_codex_xm1024};

                for (int i = 0; i <= 1; i++)
                {
                    GHPC.Weapons.AmmoRack rack = loadoutManager.RackLoadouts[i].Rack;
                    loadoutManager.RackLoadouts[i].OverrideInitialClips = new AmmoClipCodexScriptable[] {clip_codex_xm1023, clip_codex_xm1024};
                    rack.ClipTypes = new AmmoType.AmmoClip[] {clip_xm1023, clip_xm1024};
                    Util.EmptyRack(rack);
                }

                loadoutManager.SpawnCurrentLoadout();

                PropertyInfo roundInBreech = typeof(AmmoFeed).GetProperty("AmmoTypeInBreech"); 
                roundInBreech.SetValue(mainGun.Feed, null);
                roundInBreech.SetValue(towGun.Feed, null);

                MethodInfo refreshBreech = typeof(AmmoFeed).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
                refreshBreech.Invoke(mainGun.Feed, new object[] {});
                refreshBreech.Invoke(towGun.Feed, new object[] { });

                towRack.AddInvisibleClip(clip_TOW_FF);

                // update ballistics computer
                MethodInfo registerAllBallistics = typeof(LoadoutManager).GetMethod("RegisterAllBallistics", BindingFlags.Instance | BindingFlags.NonPublic);
                registerAllBallistics.Invoke(loadoutManager, new object[] {});
            }
        }

        [HarmonyPatch(typeof(GHPC.Weapons.LiveRound), "Start")]
        public static class Airburst 
        {
            private static void Postfix(GHPC.Weapons.LiveRound __instance) 
            {
                if (__instance.Info.Name != "M1024 HEAB-T") return;

                FieldInfo rangedFuseTimeField = typeof(GHPC.Weapons.LiveRound).GetField("_rangedFuseCountdown", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo rangedFuseTimeActiveField = typeof(GHPC.Weapons.LiveRound).GetField("_rangedFuseActive", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo ballisticsComputerField = typeof(FireControlSystem).GetField("_bc", BindingFlags.Instance | BindingFlags.NonPublic);

                FireControlSystem FCS = __instance.Shooter.WeaponsManager.Weapons[0].FCS;
                BallisticComputerRepository bc = ballisticsComputerField.GetValue(FCS) as BallisticComputerRepository;
                float range = FCS.CurrentRange;
                float fallOff = bc.GetFallOfShot(ammo_xm1024, range);
                float extra_distance = range > 2000 ? 19f + 3.5f : 17f;

                //funky math 
                rangedFuseTimeField.SetValue(__instance, bc.GetFlightTime(ammo_xm1024, range + range / ammo_xm1024.MuzzleVelocity * 2 + (range + fallOff) / 2000f + extra_distance));
                rangedFuseTimeActiveField.SetValue(__instance, true);
            }
        }

        [HarmonyPatch(typeof(GHPC.Weapons.FireControlSystem), "DoLase")]
        public static class LockTarget
        {
            private static void Postfix(GHPC.Weapons.FireControlSystem __instance)
            {
                if (__instance.gameObject.GetComponentInParent<Vehicle>().FriendlyName != "M2(50) Bradley") return;
                if (__instance.CurrentAmmoType.Name != "BGM-71C I-TOW-FF") return; 

                float num = -1f;
                int layerMask = 1 << CodeUtils.LAYER_MASK_VISIBILITYONLY;
                RaycastHit raycastHit;
                if (Physics.Raycast(__instance.LaserOrigin.position, __instance.LaserOrigin.forward, out raycastHit, __instance.MaxLaserRange, layerMask) && raycastHit.collider.tag == "Smoke")
                {
                    return;
                }
                if (Physics.Raycast(__instance.LaserOrigin.position, __instance.LaserOrigin.forward, out raycastHit, __instance.MaxLaserRange, ConstantsAndInfoManager.Instance.LaserRangefinderLayerMask.value) && (raycastHit.distance < num || num == -1f))
                {
                    num = raycastHit.distance;
                }

                GameObject raycast_hit = raycastHit.transform.gameObject;

                if (raycast_hit.TryGetComponentInChildren<GHPC.VariableArmor>(out GHPC.VariableArmor var_armor))
                {
                    SetTarget(__instance, (GHPC.Vehicle.Vehicle)var_armor.Unit);
                }
                else if (raycast_hit.TryGetComponentInChildren<GHPC.UniformArmor>(out GHPC.UniformArmor uni_armor))
                {
                    SetTarget(__instance, (GHPC.Vehicle.Vehicle)uni_armor.Unit);
                }
                else {
                    ResetGuidance(__instance.CurrentWeaponSystem.GuidanceUnit, __instance);
                }
            }
        }

        [HarmonyPatch(typeof(GHPC.AI.UnitAI), "SetTarget")]
        public static class AILockTarget
        {
            private static void Postfix(GHPC.AI.UnitAI __instance, object[] __args)
            {
                bool player_controlled = playerManager.CurrentPlayerUnit.InstanceId == __instance.Unit.InstanceId;

                if (player_controlled) return;
                if (__args[0] == null) return;
                if ((__args[0] as GHPC.AI.ITarget).Owner == null) return;
                if (__instance.Unit.FriendlyName != "M2(50) Bradley") return;

                WeaponSystemInfo weapon_system_info = __instance.UCI.GunnerBrain.ActiveWeapon;

                if (weapon_system_info == null) return; 

                if (weapon_system_info.FCS.CurrentAmmoType.Name != "BGM-71C I-TOW-FF") return;

                SetTarget(weapon_system_info.FCS, (__args[0] as GHPC.AI.ITarget).Owner as GHPC.Vehicle.Vehicle);
            }
        }

        [HarmonyPatch(typeof(GHPC.Weapons.MissileGuidanceUnit), "OnGuidanceStopped")]
        public static class ResetTargetGuidanceStopped
        {
            private static void Postfix(GHPC.Weapons.MissileGuidanceUnit __instance)
            {
                Vehicle vic = __instance.gameObject.GetComponentInParent<Vehicle>();

                if (vic.FriendlyName != "M2(50) Bradley") return;

                ResetGuidance(__instance, vic.GetComponentInChildren<FireControlSystem>());
            }
        }

        [HarmonyPatch(typeof(GHPC.Weapons.MissileGuidanceUnit), "StopGuidance")]
        public static class KeepTracking
        {
            private static bool Prefix(GHPC.Weapons.MissileGuidanceUnit __instance)
            {
                if (__instance.CurrentMissiles.Count > 0 && __instance.CurrentMissiles[0].ShotInfo.TypeInfo.Name == "BGM-71C I-TOW-FF") {
                    return false; 
                }

                return true; 
            }
        }

        [HarmonyPatch(typeof(GHPC.Weapons.WeaponSystem), "Fire")]
        public static class CannotFireWithoutLock
        {
            private static bool Prefix(GHPC.Weapons.WeaponSystem __instance)
            {  
                FireControlSystem fcs = __instance.FCS;

                if (fcs.CurrentAmmoType.Name == "BGM-71C I-TOW-FF" && locked_on_targets[fcs.GetInstanceID()].Target == null)
                {
                    return false; 
                }

                return true;
            }
        }
    }
}
