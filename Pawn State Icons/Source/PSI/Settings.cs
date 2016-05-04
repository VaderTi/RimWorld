namespace PSI
{
    internal class Settings
    {
        public float icon_Size = 1f;
        public float icon_SizeMult = 1f;
        public float icon_DistanceX = 1f;
        public float icon_DistanceY = 1f;
        public float icon_OffsetX = 1f;
        public float icon_OffsetY = 1f;

        public int icons_InColumn = 3;
        public bool icons_Horizontal;
        public bool icons_ScreenScale = true;
        public string icon_Set = "default";

        public bool show_TargetPoint = true;
        public bool show_Aggressive = true;
        public bool show_Dazed = true;
        public bool show_Leave = true;
        public bool show_Draft = true;
        public bool show_Idle = true;
        public bool show_Unarmed = true;
        public bool show_Hungry = true;
        public bool show_Sad = true;
        public bool show_Tired = true;
        public bool show_Disease = true;
        public bool show_Effectiveness = true;
        public bool show_Bloodloss = true;
        public bool show_Hot = true;
        public bool show_Cold = true;
        public bool show_Naked = true;
        public bool show_Drunk = true;
        public bool show_ApparelHealth = true;
        public bool show_Pacific = true;

        public float limit_MoodLess = 0.25f;
        public float limit_FoodLess = 0.25f;
        public float limit_RestLess = 0.25f;
        public float limit_EfficiencyLess = 0.33f;
        public float limit_DiseaseLess = 1f;
        public float limit_BleedMult = 3f;
        public float limit_ApparelHealthLess = 0.33f;
        public float limit_TempComfortOffset;
    }
}
