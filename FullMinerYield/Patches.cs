using HarmonyLib;
using System;
using KMod;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;


namespace FullMinerYield
{
    public class Patches : UserMod2
    {
        public static class Mod_OnLoad
        {
            public static void OnLoad()
            {
                Debug.Log("FullMinerYield MOD Loaded!");
            }
        }

        [HarmonyPatch(typeof(WorldDamage), "OnDigComplete")]
        internal class FullMinerYield_WorldDamage_OnDigComplete
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
            {
                IEnumerable<CodeInstruction> source = instr;
                foreach (CodeInstruction codeInstruction in source.ToList<CodeInstruction>())
                {
                    if ((OpCode)codeInstruction.opcode == OpCodes.Ldc_R4 && (float)codeInstruction.operand == 0.5)
                    {
                        Debug.Log((object)" === Transpiler applied === ");
                        //codeInstruction.operand = (OperandType)(ValueType) 1f;
                        codeInstruction.operand = 1f;
                    }
                    yield return codeInstruction;
                }
            }
        }
    }
}
