using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Reloading
{
    public class ReloadingMod : Mod
    {
        private static FieldInfo thisPropertyInfo;
        private static readonly List<MethodInfo> patchedMethods = new List<MethodInfo>();
        private static Harmony harm;

        private static readonly Dictionary<Verb, IReloadable> reloadables = new Dictionary<Verb, IReloadable>();
        private static HarmonyMethod[] patches;

        public ReloadingMod(ModContentPack content) : base(content)
        {
            harm = new Harmony("legodude17.weaponreloading");
            harm.Patch(
                AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"),
                postfix: new HarmonyMethod(AccessTools.Method(GetType(), "AddWeaponReloadOrders")));
            harm.Patch(AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand"),
                new HarmonyMethod(AccessTools.Method(GetType(), "CreateReloadableVerbTargetCommand")));
            var type = typeof(JobDriver_AttackStatic).GetNestedType("<>c__DisplayClass4_0", BindingFlags.NonPublic);
            thisPropertyInfo = type.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
            harm.Patch(type.GetMethod("<MakeNewToils>b__1", BindingFlags.NonPublic | BindingFlags.Instance),
                transpiler: new HarmonyMethod(AccessTools.Method(GetType(), "EndJobIfVerbNotAvailable")));
            harm.Patch(AccessTools.Method(typeof(Stance_Busy), "Expire"),
                postfix: new HarmonyMethod(GetType(), "ReloadWeaponIfEndingCooldown"));
            harm.Patch(AccessTools.Method(typeof(PawnInventoryGenerator), "GenerateInventoryFor"),
                postfix: new HarmonyMethod(GetType(), "GenerateAdditionalAmmo"));
            if (ModLister.HasActiveModWithName("Vanilla Expanded Framework") ||
                ModLister.HasActiveModWithName("Multi Verb Combat Framework"))
            {
                Log.Message("[Reloading] Applying MVCF compat patch");
                harm.Patch(
                    AccessTools.Method(Type.GetType("MVCF.Utilities.PawnVerbGizmoUtility, MVCF"), "GetGizmosForVerb"),
                    postfix: new HarmonyMethod(GetType(), "UseReloadableCommand"));
            }

            patches = new[]
            {
                new HarmonyMethod(GetType(), nameof(CheckShots)),
                new HarmonyMethod(GetType(), nameof(TryCastShot_Postfix)),
                new HarmonyMethod(GetType(), nameof(Projectile_Prefix))
            };

            Log.Message("Applied patches for " + harm.Id);
        }

        public static bool CheckShots(Verb __instance, ref bool __result)
        {
            var reloadable = GetReloadable(__instance);
            if (reloadable == null || reloadable.ShotsRemaining > 0) return true;
            __result = false;
            return false;
        }

        public static void TryCastShot_Postfix(Verb __instance)
        {
            GetReloadable(__instance)?.Notify_ProjectileFired();
        }

        public static bool Projectile_Prefix(Verb __instance, ref ThingDef __result)
        {
            if (GetReloadable(__instance)?.CurrentProjectile is ThingDef proj)
            {
                __result = proj;
                return false;
            }

            return true;
        }


        public static void Patch(MethodInfo target, HarmonyMethod prefix = null, HarmonyMethod postfix = null)
        {
            if (patchedMethods.Contains(target)) return;
            patchedMethods.Add(target);
            harm.Patch(target, prefix, postfix);
        }

        public static MethodInfo FirstDeclaredMethod(Type type, string methodName)
        {
            var method = AccessTools.Method(type, methodName);
            while (!method.IsDeclaredMember())
            {
                type = type?.BaseType;
                method = AccessTools.Method(type, methodName);
                if (type == null || method == null) return null;
            }

            return method;
        }

        public static void RegisterVerb(Type verbType)
        {
            Patch(FirstDeclaredMethod(verbType, "TryCastShot"), patches[0], patches[1]);
            Patch(FirstDeclaredMethod(verbType, "Available"), patches[0]);
            var method = AccessTools.Method(verbType, "Projectile");
            if (method != null) Patch(method, patches[1]);
        }

        public static IReloadable GetReloadable(Verb verb)
        {
            if (reloadables.ContainsKey(verb)) return reloadables[verb];

            IReloadable rv;

            if (verb.EquipmentSource != null &&
                verb.EquipmentSource.AllComps.FirstOrFallback(comp => comp is IReloadable) is IReloadable r1)
                rv = r1;

            else if (verb.HediffCompSource?.parent != null &&
                     verb.HediffCompSource.parent.comps.FirstOrFallback(comp => comp is IReloadable) is IReloadable r2
            )
                rv = r2;
            else rv = null;
            reloadables.Add(verb, rv);
            return rv;
        }

        public static void AddWeaponReloadOrders(List<FloatMenuOption> opts, Vector3 clickPos, Pawn pawn)
        {
            var c = IntVec3.FromVector3(clickPos);

            foreach (var thing in c.GetThingList(pawn.Map))
                if (thing.TryGetComp<CompReloadable>() is CompReloadable comp)
                {
                    var text = "Reloading.Unload".Translate(comp.parent.Named("GEAR")) + " (" + comp.ShotsRemaining +
                               "/" +
                               comp.Props.MaxShots + ")";
                    if (comp.ShotsRemaining == 0)
                    {
                        text += ": " + "Reloading.NoAmmo".Translate();
                        opts.Add(new FloatMenuOption(text, null));
                    }
                    else
                    {
                        opts.Add(new FloatMenuOption(text,
                            () => pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(ReloadingDefOf.Unload, thing))));
                    }
                }

            foreach (var reloadable in pawn.AllReloadComps())
            foreach (var thing in c.GetThingList(pawn.Map))
                if (reloadable.CanReloadFrom(thing))
                {
                    var text = (reloadable.Parent is Thing ? "Reload" : "Reloading.Reload").Translate(
                                   reloadable.Parent.Named("GEAR"),
                                   thing.def.Named("AMMO")) + " (" + reloadable.ShotsRemaining + "/" +
                               reloadable.MaxShots + ")";
                    var failed = false;
                    var ammo = new List<Thing>();
                    if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        text += ": " + "NoPath".Translate().CapitalizeFirst();
                        failed = true;
                    }
                    else if (!reloadable.NeedsReload())
                    {
                        text += ": " + "ReloadFull".Translate();
                        failed = true;
                    }
                    else if ((ammo = JobGiver_Reload.FindAmmo(pawn, c, reloadable)).NullOrEmpty())
                    {
                        text += ": " + "ReloadNotEnough".Translate();
                        failed = true;
                    }

                    if (failed) opts.Add(new FloatMenuOption(text, null));
                    else
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                            new FloatMenuOption(text,
                                () => pawn.jobs.TryTakeOrderedJob(
                                    JobGiver_Reload.MakeReloadJob(reloadable, ammo))), pawn, thing));
                }
                else if (thing == pawn)
                {
                    foreach (var item in pawn.inventory.innerContainer)
                        if (reloadable.CanReloadFrom(item))
                        {
                            var text = (reloadable.Parent is Thing ? "Reload" : "Reloading.Reload").Translate(
                                           reloadable.Parent.Named("GEAR"),
                                           item.def.Named("AMMO")) + " (" + reloadable.ShotsRemaining + "/" +
                                       reloadable.MaxShots + ")";
                            if (!reloadable.NeedsReload())
                                opts.Add(new FloatMenuOption(text + ": " + "ReloadFull".Translate(), null));
                            else
                                opts.Add(
                                    new FloatMenuOption(text,
                                        () => pawn.jobs.TryTakeOrderedJob(
                                            JobGiver_ReloadFromInventory.MakeReloadJob(reloadable, item))));
                        }
                }
        }

        public static bool CreateReloadableVerbTargetCommand(Thing ownerThing, Verb verb,
            ref Command_VerbTarget __result)
        {
            if (GetReloadable(verb) is IReloadable reloadable)
            {
                var command = new Command_ReloadableVerbTarget(reloadable)
                {
                    defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst(),
                    icon = ownerThing.def.uiIcon,
                    iconAngle = ownerThing.def.uiIconAngle,
                    iconOffset = ownerThing.def.uiIconOffset,
                    tutorTag = "VerbTarget",
                    verb = verb
                };

                if (verb.caster.Faction != Faction.OfPlayer)
                    command.Disable("CannotOrderNonControlled".Translate());
                else if (verb.CasterIsPawn && verb.CasterPawn.WorkTagIsDisabled(WorkTags.Violent))
                    command.Disable(
                        "IsIncapableOfViolence".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                else if (verb.CasterIsPawn && !verb.CasterPawn.drafter.Drafted)
                    command.Disable(
                        "IsNotDrafted".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                else if (reloadable.ShotsRemaining < verb.verbProps.burstShotCount)
                    command.Disable("CommandReload_NoAmmo".Translate("ammo".Named("CHARGENOUN"),
                        reloadable.AmmoExample.Named("AMMO"),
                        ((reloadable.MaxShots - reloadable.ShotsRemaining) * reloadable.ItemsPerShot).Named("COUNT")));

                __result = command;

                return false;
            }

            return true;
        }

        public static IEnumerable<Gizmo> UseReloadableCommand(IEnumerable<Gizmo> __result)
        {
            foreach (var gizmo in __result)
                if (gizmo is Command_VerbTarget command && GetReloadable(command.verb) is IReloadable reloadable)
                {
                    var verbReloadable = command.verb;

                    var reloadableVerbTarget = new Command_ReloadableVerbTarget(reloadable)
                    {
                        defaultDesc = command.Desc,
                        defaultLabel = command.Label,
                        icon = command.icon,
                        iconAngle = command.iconAngle,
                        iconOffset = command.iconOffset,
                        tutorTag = "VerbTarget",
                        verb = verbReloadable
                    };

                    if (verbReloadable.caster.Faction != Faction.OfPlayer)
                        reloadableVerbTarget.Disable("CannotOrderNonControlled".Translate());
                    else if (verbReloadable.CasterIsPawn &&
                             verbReloadable.CasterPawn.WorkTagIsDisabled(WorkTags.Violent))
                        reloadableVerbTarget.Disable(
                            "IsIncapableOfViolence".Translate(verbReloadable.CasterPawn.LabelShort,
                                verbReloadable.CasterPawn));
                    else if (verbReloadable.CasterIsPawn && !verbReloadable.CasterPawn.drafter.Drafted)
                        reloadableVerbTarget.Disable(
                            "IsNotDrafted".Translate(verbReloadable.CasterPawn.LabelShort, verbReloadable.CasterPawn));
                    else if (reloadable.ShotsRemaining < verbReloadable.verbProps.burstShotCount)
                        reloadableVerbTarget.Disable("CommandReload_NoAmmo".Translate("ammo".Named("CHARGENOUN"),
                            reloadable.AmmoExample.Named("AMMO"),
                            ((reloadable.MaxShots - reloadable.ShotsRemaining) * reloadable.ItemsPerShot)
                            .Named("COUNT")));

                    yield return reloadableVerbTarget;
                }
                else
                {
                    yield return gizmo;
                }
        }

        public static IEnumerable<CodeInstruction> EndJobIfVerbNotAvailable(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var list = instructions.ToList();
            var idx = list.FindIndex(ins => ins.IsLdarg(0));
            var idx2 = list.FindIndex(idx + 1, ins => ins.IsLdarg(0));
            var idx3 = list.FindIndex(idx2, ins => ins.opcode == OpCodes.Ret);
            var list2 = list.Skip(idx2).Take(idx3 - idx2).ToList().ListFullCopy();
            list2.Find(ins => ins.opcode == OpCodes.Ldc_I4_2).opcode = OpCodes.Ldc_I4_3;
            var idx4 = list.FindIndex(ins => ins.opcode == OpCodes.Stloc_2);
            var label = generator.DefineLabel();
            list[idx4 + 1].labels.Add(label);
            var list3 = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, thisPropertyInfo),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(JobDriver), "pawn")),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(ReloadingMod), "PawnCanCurrentlyUseVerb")),
                new CodeInstruction(OpCodes.Brtrue_S, label)
            };
            list3.AddRange(list2);
            list.InsertRange(idx4 + 1, list3);
            return list;
        }

        public static bool PawnCanCurrentlyUseVerb(Verb verb, Pawn pawn)
        {
            return verb.IsMeleeAttack
                ? verb.CanHitTargetFrom(pawn.Position, verb.CurrentTarget)
                : verb.IsStillUsableBy(pawn);
        }

        public static void ReloadWeaponIfEndingCooldown(Stance_Busy __instance)
        {
            if (__instance.verb?.EquipmentSource == null) return;
            var pawn = __instance.verb.CasterPawn;
            if (pawn == null) return;
            var comp = __instance.verb.EquipmentSource.TryGetComp<CompReloadable>();
            if (comp == null || comp.ShotsRemaining != 0 || pawn.stances.curStance.StanceBusy) return;

            var item = pawn.inventory.innerContainer.FirstOrDefault(t => comp.CanReloadFrom(t));

            if (item == null) return;

            var job = new Job(JobDefOf.AttackStatic, pawn.CurJob.targetA)
            {
                canUseRangedWeapon = true, verbToUse = __instance.verb, endIfCantShootInMelee = true
            };
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            pawn.jobs.TryTakeOrderedJob(JobGiver_ReloadFromInventory.MakeReloadJob(comp, item),
                requestQueueing: false);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder, true);
        }

        public static void GenerateAdditionalAmmo(Pawn p)
        {
            if (p.equipment?.Primary == null) return;
            if (!p.equipment.Primary.def.HasModExtension<GenerateWithAmmo>()) return;
            var ext = p.equipment.Primary.def.GetModExtension<GenerateWithAmmo>();
            foreach (var cc in ext.min)
            {
                var max = ext.max.Find(tdcc => tdcc.thingDef == cc.thingDef);
                var count = 0;
                if (max == null)
                {
                    Log.Warning(p.equipment.Primary.def.label + " has min number of " + cc.thingDef.label +
                                " but no max. This is not recommended.");
                    count = cc.count;
                }
                else
                {
                    count = new IntRange(cc.count, max.count).RandomInRange;
                }

                var t = ThingMaker.MakeThing(cc.thingDef);
                t.stackCount = count;
                p.inventory.innerContainer.TryAdd(t);
            }
        }
    }
}