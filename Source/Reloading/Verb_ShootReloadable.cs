using Verse;

namespace Reloading
{
    public class Verb_ShootReloadable : Verb_Shoot, IReloadingVerb
    {
        public override ThingDef Projectile =>
            Reloadable?.CurrentProjectile ?? base.Projectile;

        public IReloadable Reloadable
        {
            get
            {
                if (EquipmentSource != null &&
                    EquipmentSource.AllComps.FirstOrFallback(comp => comp is IReloadable) is IReloadable r1)
                    return r1;

                if (HediffCompSource?.parent != null &&
                    HediffCompSource.parent.comps.FirstOrFallback(comp => comp is IReloadable) is IReloadable r2
                )
                    return r2;

                return null;
            }
        }

        protected override bool TryCastShot()
        {
            if (Reloadable == null) return false;
            var flag = base.TryCastShot();
            Reloadable.Notify_ProjectileFired();
            return flag;
        }

        public override bool Available()
        {
            if (Reloadable == null) return false;
            return Reloadable.ShotsRemaining > 0 && base.Available();
        }
    }
}