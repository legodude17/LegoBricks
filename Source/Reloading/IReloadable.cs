// IReloadable.cs by Joshua Bennett
// 
// Created 2021-02-06

using Verse;

namespace Reloading
{
    public interface IReloadable
    {
        int ShotsRemaining { get; set; }
        int ItemsPerShot { get; }
        int MaxShots { get; }
        Thing Thing { get; }
        ThingDef CurrentProjectile { get; }
        ThingDef AmmoExample { get; }
        object Parent { get; }
        bool CanReloadFrom(Thing ammo);
        Thing Reload(Thing ammo);
        int ReloadTicks(Thing ammo);
        bool NeedsReload();
        void Unload();
        void Notify_ProjectileFired();
        void ReloadEffect(int curTick, int ticksTillDone);
    }
}