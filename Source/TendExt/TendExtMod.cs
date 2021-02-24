using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TendExt
{
    public class TendExtMod : Mod
    {
        public TendExtMod(ModContentPack content) : base(content)
        {
            var harm = new Harmony("legodude17.tendext");
            harm.Patch(AccessTools.Method(typeof(JobDriver_TendPatient), "MakeNewToils"),
                postfix: new HarmonyMethod(typeof(TendExtMod), "TendPatientPostfix"));
            harm.Patch(AccessTools.Method(typeof(TendUtility), "DoTend"),
                transpiler: new HarmonyMethod(typeof(TendExtMod), "DoTendHediffs"));
            Log.Message("Applied patches for: " + harm.Id);
        }

        public static IEnumerable<Toil> TendPatientPostfix(IEnumerable<Toil> __result, JobDriver_TendPatient __instance)
        {
            foreach (var toil in __result)
            {
                if (toil.defaultCompleteMode == ToilCompleteMode.Delay && __instance.job.targetB.IsValid &&
                    __instance.job.targetB.HasThing)
                {
                    var stat = __instance.job.targetB.Thing.GetStatValue(StatDefOf.MedicalTendSpeed);
                    if (Math.Abs(stat - 1f) > 0.0000001f)
                        toil.defaultDuration *= (int) (1 - stat);
                }

                yield return toil;
            }
        }

        public static IEnumerable<CodeInstruction> DoTendHediffs(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var list = instructions.ToList();
            var local = generator.DeclareLocal(typeof(Hediff));
            var method = AccessTools.Method(typeof(List<Hediff>), "get_Item");
            var idx = list.FindIndex(ins => ins.Calls(method));
            list.InsertRange(idx + 1, new[]
            {
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldloc, local),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TendExtMod), "TryApplyTendHediffs")),
                new CodeInstruction(OpCodes.Ldloc, local)
            });
            return list;
        }

        public static void TryApplyTendHediffs(Pawn patient, Medicine medicine, Hediff hediff)
        {
            if (medicine.TryGetComp<CompTendHediff>() is CompTendHediff comp) comp.ApplyHediffs(patient, hediff.Part);
        }
    }
}