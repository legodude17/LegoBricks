using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ModBase
{
    public interface ICustomRenderer
    {
        void Render(string label, string tooltip, FieldInfo info, object obj, Listing_Standard listing,
            SettingsRenderer renderer);
    }

    public class CustomDefDrawer : ICustomRenderer
    {
        private Def curDef;
        private Vector2 scrollPos = new Vector2(0, 0);
        private string searchBar = "";
        private bool setRect;
        private Rect viewRect;

        public void Render(string label, string tooltip, FieldInfo info, object obj, Listing_Standard listing,
            SettingsRenderer renderer)
        {
            curDef = (Def) info.GetValue(obj);
            var rect = listing.GetRect(300f);
            Widgets.Label(rect.LeftHalf(), label);
            var rect1 = rect.RightHalf();
            searchBar = Widgets.TextField(rect1.TopPartPixels(30f), searchBar);
            var defs = GenDefDatabase.GetAllDefsInDatabaseForDef(info.FieldType);
            var listing1 = new Listing_Standard();
            var rect2 = rect1.BottomPartPixels(270f);
            if (!setRect)
            {
                viewRect = rect2.AtZero();
                setRect = true;
            }

            listing1.BeginScrollView(rect2, ref scrollPos, ref viewRect);
            foreach (var def1 in defs.Where(def =>
                def.label.Contains(searchBar) || def.description.Contains(searchBar) ||
                def.defName.Contains(searchBar)))
            {
                var rect4 = listing1.GetRect(20f);
                if (Widgets.ButtonText(rect4, def1.label))
                {
                    curDef = def1 == curDef ? null : def1;
                    info.SetValue(obj, curDef, renderer);
                }

                TooltipHandler.TipRegion(rect4, def1.description);

                if (def1 == curDef)
                {
                    GUI.color = Color.yellow;
                    Widgets.DrawBox(rect4, 2);
                    GUI.color = Color.white;
                }
            }

            listing1.EndScrollView(ref viewRect);
            TooltipHandler.TipRegion(rect.LeftHalf(), tooltip);
        }
    }
}