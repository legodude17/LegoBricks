using HarmonyLib;
using HeavyWeapons;
using RimWorld;
using Verse;

namespace HeavyWeaponsExt
{
    public class Main : Mod
    {
        public Main(ModContentPack content) : base(content)
        {
            var harm = new Harmony("legodude17.heavyweaponsx");
            harm.Patch(AccessTools.Method(typeof(Patch_FloatMenuMakerMap.AddHumanlikeOrders_Fix), "CanEquip"),
                new HarmonyMethod(GetType(), "EquipPrefix"));
        }

        public static bool EquipPrefix(ref bool __result, Pawn pawn, HeavyWeapon options)
        {
            if (pawn?.story?.traits?.HasTrait(TraitDef.Named("SYR_StrongBack")) ?? false)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}