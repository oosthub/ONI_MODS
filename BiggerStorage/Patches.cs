using HarmonyLib;
using KMod;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;


namespace BiggerStorage
{
    public class BiggerStorage : UserMod2
    {
        private const int CONFIG_VERSION = 4;
        public static Config config;
        public static string pathToMod;
        public static string pathToConfig;
        public static bool alreadyOpenedFileLog;

        public static void OnLoad(string path)
        {
            try
            {
                AnnouncePatches();
                pathToMod = path;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                OpenUnityFileLog();
                throw ex;
            }
        }

        public static void PrePatch(Harmony harmony)
        {
            string str = Path.Combine(Manager.GetDirectory(), Path.Combine("settings", nameof(BiggerStorage)));
            if (!Directory.Exists(str))
                Directory.CreateDirectory(str);
            pathToConfig = Path.Combine(str, "config.json");
            LoadConfig();
        }

        private static void LoadConfig()
        {
            if (File.Exists(pathToConfig))
            {
                Debug.Log((object)"[BiggerStorage] Loading config.json");
                config = (Config)JsonConvert.DeserializeObject<Config>(File.ReadAllText(pathToConfig));
                if (config.version == 4)
                    return;
            }
            else
            {
                Debug.Log((object)"[BiggerStorage] Creating config.json");
                config = new Config();
            }
            config.version = 4;
            File.WriteAllText(pathToConfig, JsonConvert.SerializeObject((object)config, (Formatting)1));
            Application.OpenURL(Path.GetDirectoryName(pathToConfig));
        }

        public static void OpenUnityFileLog()
        {
            if (alreadyOpenedFileLog)
                return;
            alreadyOpenedFileLog = true;
            string[] strArray = new string[2]
            {
        Application.persistentDataPath,
        Application.dataPath
            };
            foreach (string path1 in strArray)
            {
                string path = Path.Combine(path1, "output_log.txt");
                if (File.Exists(path))
                {
                    Thread.Sleep(500);
                    Application.OpenURL(path);
                    break;
                }
            }
        }

        public static void AnnouncePatches()
        {
            Debug.Log((object)"[BiggerStorage] Announce harmony patches");
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (HarmonyMethod harmonyMethod in HarmonyMethodExtensions.GetFromType(type))//.GetHarmonyMethods(type))
                    Debug.Log((object)("[BiggerStorage] -> " + ((Type)harmonyMethod.declaringType).FullName + "." + (string)harmonyMethod.methodName));
            }
        }

        public class Config
        {
            public float StorageMultiplier = 4f;
            public float BatteryMultiplier = 2f;
            public float RocketFuelMultiplier = 1f;
            public float RocketCargoMultiplier = 1f;
            public float PipeMultiplier = 1f;
            public float WireMultiplier = 1f;
            public float GeneratorMultiplier = 1f;
            public float ConveyorMultiplier = 1f;
            public float OtherMultiplier = 1f;
            public Config.Capacities Capacity = new Config.Capacities();
            public int version;

            public class Capacities
            {
                public int Locker = 20000;
                public int LiquidReservoir = 5000;
                public int GasReservoir = 150;
                public int RationBox = 150;
                public int Refrigerator = 100;
                public int FuelTank = 900;
                public int OxidizerTank = 2700;
                public int CargoBay = 1000;
                public int ConveyorInbox = 1000;
                public int ConveyorOutbox = 1000;
                public int ConveyorRails = 20;
                public float LiquidPipe = 10f;
                public float GasPipe = 1f;
            }
        }

        [HarmonyPatch(typeof(Game), "OnPrefabInit")]
        private static class Game_OnPrefabInit_Patch
        {
            private static void Postfix(Game __instance)
            {
                try
                {
                    LoadConfig();
                    Traverse.Create((object)Game.Instance.gasConduitFlow).Field("MaxMass").SetValue((object)(float)((double)config.PipeMultiplier * (double)config.Capacity.GasPipe));
                    Debug.Log((object)string.Format("[BiggerStorage] GasPipe - multiplier {0}, new capacity {1}", (object)config.PipeMultiplier, (object)(float)((double)config.PipeMultiplier * (double)config.Capacity.GasPipe)));
                    Traverse.Create((object)Game.Instance.liquidConduitFlow).Field("MaxMass").SetValue((object)(float)((double)config.PipeMultiplier * (double)config.Capacity.LiquidPipe));
                    Debug.Log((object)string.Format("[BiggerStorage] LiquidPipe - multiplier {0}, new capacity {1}", (object)config.PipeMultiplier, (object)(float)((double)config.PipeMultiplier * (double)config.Capacity.LiquidPipe)));
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch(typeof(ValveBase), "OnSpawn")]
        private static class ValveBase_OnSpawn_Patch
        {
            private static void Postfix(ValveBase __instance)
            {
                try
                {
                    if (__instance.conduitType == ConduitType.Liquid)
                    {
                        float num = Mathf.Clamp01(__instance.CurrentFlow / __instance.maxFlow);
                        __instance.maxFlow = (float)(config.PipeMultiplier * config.Capacity.LiquidPipe);
                        __instance.CurrentFlow = (float)__instance.maxFlow * num;
                        Debug.Log((object)string.Format("[BiggerStorage] LiquidValve - multiplier {0}, new max flow {1}", (object)config.PipeMultiplier, (object)(float)((double)config.PipeMultiplier * (double)config.Capacity.LiquidPipe)));
                    }
                    else
                    {
                        if (__instance.conduitType != ConduitType.Gas)
                            return;
                        float num = Mathf.Clamp01(__instance.CurrentFlow / __instance.maxFlow);
                        __instance.maxFlow = (float)(config.PipeMultiplier * config.Capacity.GasPipe);
                        __instance.CurrentFlow = (float)__instance.maxFlow * num;
                        Debug.Log((object)string.Format("[BiggerStorage] GasValve - multiplier {0}, new max flow {1}", (object)config.PipeMultiplier, (object)(float)((double)config.PipeMultiplier * (double)config.Capacity.GasPipe)));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch(typeof(Battery), "OnSpawn")]
        private static class Battery_OnSpawn_Patch
        {
            private static void Postfix(Battery __instance)
            {
                Building component = (__instance).GetComponent<Building>();
                //---->BO
                //if (!Object.op_Implicit((Object)component))
                //    return;
                string prefabId = (component.Def).PrefabID;
                try
                {
                    if (prefabId == "PowerTransformer" || prefabId == "PowerTransformerSmall")
                        return;
                    Battery battery = __instance;
                    battery.capacity = (float)(battery.capacity * (double)config.BatteryMultiplier);
                    Debug.Log((object)string.Format("[BiggerStorage] {0} - multiplier {1}, new capacity {2}", (object)prefabId, (object)config.BatteryMultiplier, (object)(float)__instance.capacity));
                }
                catch (Exception ex)
                {
                    Debug.Log((object)("[BiggerStorage] " + prefabId + " - " + ex.Message));
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch(typeof(PowerTransformerConfig), "CreateBuildingDef")]
        private static class PowerTransformerConfig_CreateBuildingDef_Patch
        {
            private static void Postfix(PowerTransformerConfig __instance, BuildingDef __result)
            {
                try
                {
                    BuildingDef buildingDef1 = __result;
                    buildingDef1.GeneratorWattageRating = (float)(buildingDef1.GeneratorWattageRating * (double)config.WireMultiplier);
                    BuildingDef buildingDef2 = __result;
                    buildingDef2.GeneratorBaseCapacity = (float)(buildingDef2.GeneratorBaseCapacity * (double)config.WireMultiplier);
                    Debug.Log((object)string.Format("[BiggerStorage] PowerTransformer - multiplier {0}, new capacity {1}", (object)config.WireMultiplier, (object)(float)__result.GeneratorBaseCapacity));
                }
                catch (Exception ex)
                {
                    Debug.Log((object)("[BiggerStorage] PowerTransformer - " + ex.Message));
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch(typeof(PowerTransformerSmallConfig), "CreateBuildingDef")]
        private static class PowerTransformerSmallConfig_CreateBuildingDef_Patch
        {
            private static void Postfix(PowerTransformerSmallConfig __instance, BuildingDef __result)
            {
                try
                {
                    BuildingDef buildingDef1 = __result;
                    buildingDef1.GeneratorWattageRating = (float)(buildingDef1.GeneratorWattageRating * (double)config.WireMultiplier);
                    BuildingDef buildingDef2 = __result;
                    buildingDef2.GeneratorBaseCapacity = (float)(buildingDef2.GeneratorBaseCapacity * (double)config.WireMultiplier);
                    Debug.Log((object)string.Format("[BiggerStorage] PowerTransformerSmall - multiplier {0}, new capacity {1}", (object)config.WireMultiplier, (object)(float)__result.GeneratorBaseCapacity));
                }
                catch (Exception ex)
                {
                    Debug.Log((object)("[BiggerStorage] PowerTransformerSmall - " + ex.Message));
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch(typeof(Storage), "OnSpawn")]
        private static class Storage_OnSpawn_Patch
        {
            private static void Postfix(Storage __instance)
            {
                Building component1 = (Building)((Component)__instance).GetComponent<Building>();
                //---->BO
                //if (!Object.op_Implicit((Object)component1))
                //    return;
                string prefabId = (string)((Def)component1.Def).PrefabID;
                try
                {
                    float num1 = config.OtherMultiplier;
                    float num2 = 0.0f;
                    float num3 = 0.0f;
                    float num4 = 99999f;
                    switch (prefabId)
                    {
                        case "CargoBay":
                            num1 = config.RocketCargoMultiplier;
                            num2 = (float)config.Capacity.CargoBay * num1;
                            break;
                        case "GasCargoBay":
                        case "LiquidCargoBay":
                            num1 = config.RocketCargoMultiplier;
                            num3 = Mathf.Min((float)config.Capacity.CargoBay * num1, num4);
                            break;
                        case "GasReservoir":
                            num1 = config.StorageMultiplier;
                            num3 = Mathf.Min((float)config.Capacity.GasReservoir * num1, num4);
                            break;
                        case "LiquidFuelTank":
                        case "SteamEngine":
                            num1 = config.RocketFuelMultiplier;
                            num3 = Mathf.Min((float)config.Capacity.FuelTank * num1, num4);
                            break;
                        case "LiquidReservoir":
                            num1 = config.StorageMultiplier;
                            num3 = Mathf.Min((float)config.Capacity.LiquidReservoir * num1, num4);
                            break;
                        case "ObjectDispenser":
                        case "StorageLocker":
                        case "StorageLockerSmart":
                            num1 = config.StorageMultiplier;
                            num2 = (float)config.Capacity.Locker * num1;
                            break;
                        case "OxidizerTank":
                        case "OxidizerTankLiquid":
                            num1 = config.RocketFuelMultiplier;
                            num3 = Mathf.Min((float)config.Capacity.OxidizerTank * num1, num4);
                            break;
                        case "RationBox":
                            num1 = config.StorageMultiplier;
                            num2 = (float)config.Capacity.RationBox * num1;
                            break;
                        case "Refrigerator":
                            num1 = config.StorageMultiplier;
                            num2 = (float)config.Capacity.Refrigerator * num1;
                            break;
                        case "SolidConduitInbox":
                            num1 = config.ConveyorMultiplier;
                            num2 = (float)config.Capacity.ConveyorInbox * num1;
                            break;
                        case "SolidConduitOutbox":
                            num1 = config.ConveyorMultiplier;
                            num2 = (float)config.Capacity.ConveyorOutbox * num1;
                            break;
                        case "SpecialCargoBay":
                            num1 = config.RocketCargoMultiplier;
                            num2 = (float)__instance.capacityKg * num1;
                            break;
                        default:
                            if ((double)num1 == 1.0)
                                return;
                            num2 = (float)__instance.capacityKg * num1;
                            break;
                    }
                    if ((double)num2 > 0.0 || (double)num3 > 0.0)
                    {
                        float num5 = Mathf.Max(num2, num3);
                        IUserControlledCapacity component2 = (IUserControlledCapacity)((Component)__instance).GetComponent<IUserControlledCapacity>();
                        if (component2 != null)
                        {
                            __instance.capacityKg = (float)(double)num5;
                            component2.UserMaxCapacity = Mathf.Min(num5, component2.UserMaxCapacity);
                        }
                        else
                            __instance.capacityKg = (float)(double)num5;
                        Debug.Log((object)string.Format("[BiggerStorage] {0} - multiplier {1}, new capacity {2}", (object)prefabId, (object)num1, (object)num5));
                    }
                    if ((double)num3 <= 0.0)
                        return;
                    ConduitConsumer component3 = (ConduitConsumer)((Component)__instance).GetComponent<ConduitConsumer>();

                    //---->BO
                    //if (!Object.op_Implicit((Object)component3))
                    //    return;
                    component3.capacityKG = (float)(double)num3;
                }
                catch (Exception ex)
                {
                    Debug.Log((object)("[BiggerStorage] " + prefabId + " - " + ex.Message));
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch]
        private static class FuelTank_MaxCapacity_Patch
        {
            private static bool Prefix(FuelTank __instance, ref float __result)
            {
                try
                {
                    __result = (float)config.Capacity.FuelTank * config.RocketFuelMultiplier;
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
                return true;
            }
        }

        [HarmonyPatch]
        private static class OxidizerTank_MaxCapacity_Patch
        {
            private static bool Prefix(OxidizerTank __instance, ref float __result)
            {
                try
                {
                    __result = (float)config.Capacity.OxidizerTank * config.RocketFuelMultiplier;
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Wire), "GetMaxWattageAsFloat")]
        private static class Wire_GetMaxWattageAsFloat_Patch
        {
            private static void Postfix(Wire __instance, ref float __result)
            {
                try
                {
                    __result *= config.WireMultiplier;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch]
        private static class Generator_WattageRating_Patch
        {
            private static void Postfix(Generator __instance, ref float __result)
            {
                try
                {
                    __result *= config.GeneratorMultiplier;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch(typeof(SolidConduitDispenser), "ConduitUpdate")]
        private static class SolidConduitDispenser_ConduitUpdate_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(
              IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codeInstructionList = new List<CodeInstruction>(instructions);
                for (int index = 0; index < codeInstructionList.Count; ++index)
                {
                    if ((OpCode)codeInstructionList[index].opcode == OpCodes.Ldc_R4 && (double)(float)codeInstructionList[index].operand == 20.0)
                    {
                        codeInstructionList[index].operand = (float)((double)config.Capacity.ConveyorRails * (double)config.ConveyorMultiplier);
                        Debug.Log((object)string.Format("[BiggerStorage] Conveyor - multiplier {0}, new capacity {1}", (object)config.ConveyorMultiplier, (object)codeInstructionList[index].operand));
                    }
                }
                return ((IEnumerable<CodeInstruction>)codeInstructionList).AsEnumerable<CodeInstruction>();
            }
        }

        [HarmonyPatch(typeof(ElementSplitterComponents), "CanFirstAbsorbSecond")]
        private static class ElementSplitterComponents_CanFirstAbsorbSecond_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(
              IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codeInstructionList = new List<CodeInstruction>(instructions);
                for (int index = 0; index < codeInstructionList.Count; ++index)
                {
                    if ((OpCode)codeInstructionList[index].opcode == OpCodes.Ldc_R4 && (double)(float)codeInstructionList[index].operand == 25000.0)
                    {
                        codeInstructionList[index].operand = 99999f;
                        Debug.Log((object)string.Format("[BiggerStorage] Elements stacking changed to {0} kg", (object)codeInstructionList[index].operand));
                    }
                }
                return ((IEnumerable<CodeInstruction>)codeInstructionList).AsEnumerable<CodeInstruction>();
            }
        }

        [HarmonyPatch(typeof(WoodLogConfig), "CreatePrefab")]
        private static class WoodLogConfig_CreatePrefab_Patch
        {
            private static void Postfix(WoodLogConfig __instance, GameObject __result)
            {
                try
                {
                    ((EntitySplitter)__result.GetComponent<EntitySplitter>()).maxStackSize = 99999.0f;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch(typeof(TableSaltConfig), "CreatePrefab")]
        private static class TableSaltConfig_CreatePrefab_Patch
        {
            private static void Postfix(TableSaltConfig __instance, GameObject __result)
            {
                try
                {
                    ((EntitySplitter)__result.GetComponent<EntitySplitter>()).maxStackSize = 99999.0f;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }

        [HarmonyPatch(typeof(RotPileConfig), "CreatePrefab")]
        private static class RotPileConfig_CreatePrefab_Patch
        {
            private static void Postfix(RotPileConfig __instance, GameObject __result)
            {
                try
                {
                    ((EntitySplitter)__result.GetComponent<EntitySplitter>()).maxStackSize = 99999.0f;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }
        /*
        [HarmonyPatch(typeof(ModsScreen), "BuildDisplay")]
        private static class ModsScreen_BuildDisplay_Patch
        {
            private static void Postfix(ModsScreen __instance, KButton ___workshopButton)
            {
                try
                {
                    Type type = AccessTools.TypeByName("ModsScreen+DisplayedMod");
                    IEnumerable enumerable = (IEnumerable)Traverse.Create((object)__instance).Field("displayedMods").GetValue<IEnumerable>();
                    if (enumerable == null)
                        return;
                    foreach (object obj in enumerable)
                    {
                        Mod mod = ((List<Mod>)((Manager)Global.Instance.modManager).mods)[(int)type.GetField("mod_index").GetValue(obj)];
                        // ISSUE: cast to a reference type
                        // ISSUE: explicit reference operation
                        if (mod.enabled != null && !((string)(^(Label &) ref mod.label).id != "1720247784"))
                    {
                            RectTransform rectTransform = (RectTransform)type.GetField("rect_transform").GetValue(obj);
                            M0 m0 = Util.KInstantiateUI<KButton>(((Component)___workshopButton).gameObject, ((Component)rectTransform).gameObject, false);
                            // ISSUE: cast to a reference type
                            // ISSUE: explicit reference operation
                            ((Object)m0).name = "button_config_" + (string)(^(Label &) ref mod.label).id;
                            ((LocText)((Component)m0).GetComponentInChildren<LocText>()).text = "Config";
                            ((LayoutElement)((Component)m0).GetComponent<LayoutElement>()).preferredWidth = 70f;
                            ((KMonoBehaviour)m0).transform.SetSiblingIndex(3);
                            ((Component)m0).gameObject.SetActive(true);
                            ((KButton)m0).onClick += (Action)(() => Application.OpenURL(Path.GetDirectoryName(pathToConfig)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OpenUnityFileLog();
                }
            }
        }*/
    }
}
