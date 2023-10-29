using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Bradley50mm;
using GHPC.Camera;
using GHPC.Equipment.Optics;
using GHPC.Player;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
        AmmoType ammo_xm1024;

        AmmoClipCodexScriptable clip_codex_xm1023;
        AmmoType.AmmoClip clip_xm1023;
        AmmoCodexScriptable ammo_codex_xm1023;
        AmmoType ammo_xm1023;

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
                gun_xm913.FriendlyName = "50mm Cannon XM913";
                gun_xm913.Type = WeaponSystemCodexScriptable.WeaponType.Autocannon;

                // xm1023 
                ammo_xm1023 = new AmmoType();
                ShallowCopy(ammo_xm1023, ammo_m791);
                ammo_xm1023.Name = "XM1023 APFSDS-T";
                ammo_xm1023.Caliber = 50;
                ammo_xm1023.RhaPenetration = 150f;
                ammo_xm1023.MuzzleVelocity = 1522f;
                ammo_xm1023.VisualType = LiveRoundMarshaller.LiveRoundVisualType.Shell;
                ammo_xm1023.Mass = 0.550f;

                ammo_codex_xm1023 = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
                ammo_codex_xm1023.AmmoType = ammo_xm1023;
                ammo_codex_xm1023.name = "ammo_xm1023";

                clip_xm1023 = new AmmoType.AmmoClip();
                clip_xm1023.Capacity = 35;
                clip_xm1023.Name = "XM1023 APFSDS-T";
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
                ammo_xm1024.Name = "XM1024 PDHE-T";
                ammo_xm1024.Caliber = 50;
                ammo_xm1024.RhaPenetration = 20f;
                ammo_xm1024.MuzzleVelocity = 990f;
                ammo_xm1024.TntEquivalentKg = 0.220f;
                ammo_xm1024.MaxSpallRha = 15f;
                ammo_xm1024.MinSpallRha = 5f;
                ammo_xm1024.VisualType = LiveRoundMarshaller.LiveRoundVisualType.Shell;
                ammo_xm1024.Mass = 0.850f;

                ammo_codex_xm1024 = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
                ammo_codex_xm1024.AmmoType = ammo_xm1024;
                ammo_codex_xm1024.name = "ammo_xm1024";

                clip_xm1024 = new AmmoType.AmmoClip();
                clip_xm1024.Capacity = 60;
                clip_xm1024.Name = "XM1024 PDHE-T";
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

                WeaponsManager weaponsManager = vic.GetComponent<WeaponsManager>();
                WeaponSystemInfo mainGunInfo = weaponsManager.Weapons[0];
                WeaponSystem mainGun = mainGunInfo.Weapon;

                mainGunInfo.Name = "50mm gun XM913";
                mainGun.Impulse = 1450;
                mainGun.RecoilBlurMultiplier = 1.7f;
                mainGun.BaseDeviationAngle = 0.045f;
                FieldInfo codex = typeof(WeaponSystem).GetField("CodexEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                codex.SetValue(mainGun, gun_xm913);

                GameObject gunTube = vic_go.transform.Find("M2BRADLEY_rig/HULL/Turret/Mantlet/Main gun").gameObject;
                gunTube.transform.localScale = new Vector3(1.4f, 1.7f, 1.2f);

                Transform muzzleFlashes = mainGun.MuzzleEffects[0].transform;
                muzzleFlashes.GetChild(1).transform.localScale = new Vector3(5f, 5f, 5f);

                FieldInfo fixParallaxField = typeof(FireControlSystem).GetField("_fixParallaxForVectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
                fixParallaxField.SetValue(mainGun.FCS, true);
                mainGun.FCS.MaxLaserRange = 3000; 
                mainGun.WeaponSound.SingleShotEventPaths[0] = "event:/Weapons/canon_73mm-2A28Grom";

                mainGun.SetCycleTime(0.45f);
 
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
    }
}