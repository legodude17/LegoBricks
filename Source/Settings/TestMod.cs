using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Settings
{
    // public class TestMod : Mod
    // {
    //     private readonly SettingsRenderer renderer;
    //     public TestSettings Settings;
    //
    //     public TestMod(ModContentPack content) : base(content)
    //     {
    //         SettingsRenderer.__DEBUG = true;
    //         Settings = GetSettings<TestSettings>();
    //         renderer = new SettingsRenderer(Settings);
    //     }
    //
    //     public override string SettingsCategory()
    //     {
    //         return "Test 1";
    //     }
    //
    //     public override void DoSettingsWindowContents(Rect inRect)
    //     {
    //         base.DoSettingsWindowContents(inRect);
    //         renderer.Render(inRect);
    //     }
    // }

    public class TestSettings : ModSettings, ISettingsLabeler, ISettingsTooltipper, IEnumerable<TestSettingsTab>
    {
        public string CustomStory;
        public bool Enabled;
        public int Number;
        public float Number2;

        private List<TestSettingsTab> tabs = new List<TestSettingsTab>();

        public IEnumerator<TestSettingsTab> GetEnumerator()
        {
            return tabs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string GetLabel(string key)
        {
            switch (key)
            {
                case "Enabled":
                    return "Enable Feature?";
                case "CustomStory":
                    return "Your custom story!";
                case "Number1":
                    return "The first number:";
                case "Number2":
                    return "The second number:";
            }

            return key;
        }

        public string GetTooltip(string key)
        {
            switch (key)
            {
                case "Enabled":
                    return "Feature does things x, y, and z.";
            }

            return null;
        }

        public void LogTabs()
        {
            foreach (var tab in tabs) tab.Log();
        }

        public void InitTabs()
        {
            if (tabs.NullOrEmpty())
                tabs = new List<TestSettingsTab>
                {
                    new TestSettingsTab(1), new TestSettingsTab(2), new TestSettingsTab(3), new TestSettingsTab(4)
                };
            foreach (var tab in tabs)
                while (tabs.Except(tab).Any(tab2 => tab.Index == tab2.Index))
                    tab.Index++;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref Enabled, "Enabled", true);
            Scribe_Values.Look(ref CustomStory, "CustomStory", "Hello this is the default");
            Scribe_Values.Look(ref Number, "Number");
            Scribe_Values.Look(ref Number2, "Number2");
            Scribe_Collections.Look(ref tabs, "tabs", LookMode.Deep);
            InitTabs();
            LogTabs();
        }
    }

    public class TestSettingsTab : ISettingsLabeler, ISettingsTooltipper, INamedSettings, IExposable
    {
        public bool Enabled;
        internal int Index;
        public int Number;
        public float Number2;

        public TestSettingsTab()
        {
        }

        public TestSettingsTab(int idx)
        {
            Index = idx;
        }


        public void ExposeData()
        {
            Scribe_Values.Look(ref Enabled, "Enabled");
            Scribe_Values.Look(ref Number, "Number");
            Scribe_Values.Look(ref Number2, "Number2");
            Scribe_Values.Look(ref Index, "Index");
        }

        public string Name => "Feature " + Index;

        public string GetLabel(string key)
        {
            switch (key)
            {
                case "Enabled":
                    return "Enable " + Name + "?";
                case "Number":
                    return "First number for this feature!";
                case "Number2":
                    return "Your custom story!";
            }

            return key;
        }

        public string GetTooltip(string key)
        {
            switch (key)
            {
                case "Enabled":
                    return Name + " does things a, b, and c.";
            }

            return null;
        }

        public void Log()
        {
            Verse.Log.Message("tab " + Index + " is " + (Enabled ? "Enabled" : "Not Enabled"));
        }
    }
}