using UnityEngine;

namespace PSI
{
    internal class PawnStats
    {
        public bool pawn_hasSickThought = false;
        public bool pawn_isNudist = false;
        public bool pawn_lacksBionic = false;

        public float pawn_TotalEfficiency = 1f;
        public float pawn_TooCold = -1f;
        public float pawn_TooHot = -1f;
        public float pawn_BleedRate = -1f;
        public Vector3 TargetPos = Vector3.zero;
        public float DiseaseDisappearance = 1f;
        public float pawn_ApparelHealth = 1f;
        public float pawn_Drunkness;
    }
}
