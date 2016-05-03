using System;
using System.Collections.Generic;
using System.IO;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PSI
{
    internal class PSI : MonoBehaviour
    {
        private static double fDelta = 0.0;
        public static bool inGame = false;
        private static Dictionary<Pawn, PawnStats> stats_dict = new Dictionary<Pawn, PawnStats>();
        private static bool iconsEnabled = true;
        private static Dialog_Settings SettingsDialog = new Dialog_Settings();
        public static Settings settings = new Settings();
        private static readonly Color targetColor = new Color(1f, 1f, 1f, 0.6f);
        private static float worldScale = 1f;
        public static string[] iconSets = new string[1] {"default"};
        public static Materials materials = new Materials("default");
        private static PawnCapacityDef[] pawnCapacities;
        private static Vector3[] iconPosVectors;

        public PSI()
        {
            reinit(true, true, true);
        }

        public static void reinit(bool reloadSettings = true, bool reloadIconSet = true, bool recalcIconPos = true)
        {
            pawnCapacities = new PawnCapacityDef[11]
            {
        PawnCapacityDefOf.BloodFiltration,
        PawnCapacityDefOf.BloodPumping,
        PawnCapacityDefOf.Breathing,
        PawnCapacityDefOf.Consciousness,
        PawnCapacityDefOf.Eating,
        PawnCapacityDefOf.Hearing,
        PawnCapacityDefOf.Manipulation,
        PawnCapacityDefOf.Metabolism,
        PawnCapacityDefOf.Moving,
        PawnCapacityDefOf.Sight,
        PawnCapacityDefOf.Talking
            };
            if (reloadSettings)
                settings = loadSettings("psi-settings.cfg");
            if (reloadIconSet)
                LongEventHandler.ExecuteWhenFinished((Action)(() =>
                {
                    materials = new Materials(settings.IconSet);
                    var Settings = XmlLoader.ItemFromXmlFile<Settings>(GenFilePaths.CoreModsFolderPath + "/PSI/Textures/UI/Overlays/PawnStateIcons/" + settings.IconSet + "/iconset.cfg", true);
                    settings.IconSizeMult = Settings.IconSizeMult;
                    materials.reloadTextures(true);
                }));
            if (!recalcIconPos)
                return;
            recalcIconPositions();
        }

        public static Settings loadSettings(string path = "psi-settings.cfg")
        {
            var Settings = XmlLoader.ItemFromXmlFile<Settings>(path, true);
            var path1 = GenFilePaths.CoreModsFolderPath + "/PSI/Textures/UI/Overlays/PawnStateIcons/";
            if (Directory.Exists(path1))
            {
                iconSets = Directory.GetDirectories(path1);
                for (var index = 0; index < iconSets.Length; ++index)
                    iconSets[index] = new DirectoryInfo(iconSets[index]).Name;
            }
            return Settings;
        }

        public static void saveSettings(string path = "psi-settings.cfg")
        {
            XmlSaver.SaveDataObject((object)settings, path);
        }

        private void DrawIcon(Vector3 bodyPos, Vector3 posOffset, Icons icon, Color c)
        {
            var material = materials[icon];
            if ((UnityEngine.Object)material == (UnityEngine.Object)null)
                return;
            LongEventHandler.ExecuteWhenFinished((Action)(() =>
            {
                material.color = c;
                var color = GUI.color;
                GUI.color = c;
                Vector2 vector2;
                if (settings.IconsScreenScale)
                {
                    vector2 = bodyPos.ToScreenPosition();
                    vector2.x += posOffset.x * 45f;
                    vector2.y -= posOffset.z * 45f;
                }
                else
                    vector2 = (bodyPos + posOffset).ToScreenPosition();
                var num1 = worldScale;
                if (settings.IconsScreenScale)
                    num1 = 45f;
                var num2 = num1 * (settings.IconSizeMult * 0.5f);
                var position = new Rect(vector2.x, vector2.y, num2 * settings.IconSize, num2 * settings.IconSize);
                position.x -= position.width * 0.5f;
                position.y -= position.height * 0.5f;
                GUI.DrawTexture(position, material.mainTexture, ScaleMode.ScaleToFit, true);
                GUI.color = color;
            }));
        }

        private void DrawIcon(Vector3 bodyPos, int num, Icons icon, Color c)
        {
            DrawIcon(bodyPos, iconPosVectors[num], icon, c);
        }

        private void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v)
        {
            DrawIcon(bodyPos, num, icon, new Color(1f, v, v));
        }

        private void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2)
        {
            DrawIcon(bodyPos, num, icon, Color.Lerp(c1, c2, v));
        }

        private void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2, Color c3)
        {
            if ((double)v < 0.5)
                DrawIcon(bodyPos, num, icon, Color.Lerp(c1, c2, v * 2f));
            else
                DrawIcon(bodyPos, num, icon, Color.Lerp(c2, c3, (float)(((double)v - 0.5) * 2.0)));
        }

        public static void recalcIconPositions()
        {
            iconPosVectors = new Vector3[18];
            for (var index = 0; index < iconPosVectors.Length; ++index)
            {
                var num1 = index / settings.IconsInColumn;
                var num2 = index % settings.IconsInColumn;
                if (settings.IconsHorizontal)
                {
                    var num3 = num1;
                    num1 = num2;
                    num2 = num3;
                }
                iconPosVectors[index] = new Vector3((float)(-0.600000023841858 * (double)settings.IconDistanceX - 0.550000011920929 * (double)settings.IconSize * (double)settings.IconOffsetX * (double)num1), 3f, (float)(-0.600000023841858 * (double)settings.IconDistanceY + 0.550000011920929 * (double)settings.IconSize * (double)settings.IconOffsetY * (double)num2));
            }
        }

        public void updateColonistStats(Pawn colonist)
        {
            if (!stats_dict.ContainsKey(colonist))
                stats_dict.Add(colonist, new PawnStats());
            var pawnStats = stats_dict[colonist];
            pawnStats.isNudist = false;
            foreach (var trait in colonist.story.traits.allTraits)
            {
                switch (trait.def.defName)
                {
                    case "Nudist":
                        pawnStats.isNudist = true;
                        continue;
                    default:
                        continue;
                }
            }
            var val1 = 10f;
            foreach (var activity in pawnCapacities)
            {
                if (activity != PawnCapacityDefOf.Consciousness)
                    val1 = Math.Min(val1, colonist.health.capacities.GetEfficiency(activity));
            }
            if ((double)val1 < 0.0)
                val1 = 0.0f;
            pawnStats.total_efficiency = val1;
            pawnStats.targetPos = Vector3.zero;
            if (colonist.jobs.curJob != null)
            {
                var jobDriver = colonist.jobs.curDriver;
                var job = colonist.jobs.curJob;
                var targetInfo = job.targetA;
                if (jobDriver is JobDriver_HaulToContainer || jobDriver is JobDriver_HaulToCell || (jobDriver is JobDriver_FoodDeliver || jobDriver is JobDriver_FoodFeedPatient) || jobDriver is JobDriver_TakeToBed)
                    targetInfo = job.targetB;
                if (jobDriver is JobDriver_DoBill)
                {
                    var jobDriverDoBill = (JobDriver_DoBill)jobDriver;
                    if ((double)jobDriverDoBill.workLeft == 0.0)
                        targetInfo = job.targetA;
                    else if ((double)jobDriverDoBill.workLeft <= 0.00999999977648258)
                        targetInfo = job.targetB;
                }
                if (jobDriver is JobDriver_Hunt && colonist.carrier != null && colonist.carrier.CarriedThing != null)
                    targetInfo = job.targetB;
                if (job.def == JobDefOf.Wait)
                    targetInfo = (TargetInfo)((Thing)null);
                if (jobDriver is JobDriver_Ingest)
                    targetInfo = (TargetInfo)((Thing)null);
                if (job.def == JobDefOf.LayDown && colonist.InBed())
                    targetInfo = (TargetInfo)((Thing)null);
                if (!job.playerForced && job.def == JobDefOf.Goto)
                    targetInfo = (TargetInfo)((Thing)null);
                bool flag;
                if (targetInfo != (TargetInfo)((Thing)null))
                {
                    var cell = targetInfo.Cell;
                    flag = false;
                }
                else
                    flag = true;
                if (!flag)
                {
                    var vector3 = targetInfo.Cell.ToVector3Shifted();
                    pawnStats.targetPos = vector3 + new Vector3(0.0f, 3f, 0.0f);
                }
            }
            var temperatureForCell = GenTemperature.GetTemperatureForCell(colonist.Position);
            pawnStats.tooCold = (float)(((double)colonist.ComfortableTemperatureRange().min - (double)settings.LimitTempComfortOffset - (double)temperatureForCell) / 10.0);
            pawnStats.tooHot = (float)(((double)temperatureForCell - (double)colonist.ComfortableTemperatureRange().max - (double)settings.LimitTempComfortOffset) / 10.0);
            pawnStats.tooCold = Mathf.Clamp(pawnStats.tooCold, 0.0f, 2f);
            pawnStats.tooHot = Mathf.Clamp(pawnStats.tooHot, 0.0f, 2f);
            pawnStats.diseaseDisappearance = 1f;
            pawnStats.drunkness = DrugUtility.DrunknessPercent(colonist);
            foreach (HediffWithComps hediffWithComps in colonist.health.hediffSet.hediffs)
            {
                if (hediffWithComps != null && !((Hediff)hediffWithComps).FullyImmune() && (hediffWithComps.Visible && !((Hediff)hediffWithComps).IsOld()) && ((hediffWithComps.CurStage == null || hediffWithComps.CurStage.everVisible) && (hediffWithComps.def.tendable || hediffWithComps.def.naturallyHealed)))
                    pawnStats.diseaseDisappearance = Math.Min(pawnStats.diseaseDisappearance, colonist.health.immunity.GetImmunity(hediffWithComps.def));
            }
            var num1 = 999f;
            var wornApparel = colonist.apparel.WornApparel;
            for (var index = 0; index < wornApparel.Count; ++index)
            {
                var num2 = (float)wornApparel[index].HitPoints / (float)wornApparel[index].MaxHitPoints;
                if ((double)num2 >= 0.0 && (double)num2 < (double)num1)
                    num1 = num2;
            }
            pawnStats.apparelHealth = num1;
            pawnStats.bleedRate = Mathf.Clamp01(colonist.health.hediffSet.BleedingRate * settings.LimitBleedMult);
            stats_dict[colonist] = pawnStats;
        }

        public virtual void FixedUpdate()
        {
            fDelta += (double)Time.fixedDeltaTime;
            if (fDelta < 0.1)
                return;
            fDelta = 0.0;
            inGame = (bool)((UnityEngine.Object)GameObject.Find("CameraMap"));
            if (!inGame || !iconsEnabled)
                return;
            foreach (var colonist in Find.Map.mapPawns.FreeColonistsAndPrisoners)
            {
                try
                {
                    updateColonistStats(colonist);
                }
                catch (Exception ex)
                {
                    Log.Notify_Exception(ex);
                }
            }
        }

        public void updateOptionsDialog()
        {
            var dialogOptions = Find.WindowStack.WindowOfType<Dialog_Options>();
            var flag1 = dialogOptions != null;
            var flag2 = Find.WindowStack.IsOpen(typeof(Dialog_Settings));
            if (flag1 && flag2)
            {
                SettingsDialog.OptionsDialog = (Window)dialogOptions;
                recalcIconPositions();
            }
            else if (flag1 && !flag2)
            {
                if (!SettingsDialog.CloseButtonClicked)
                {
                    Find.UIRoot.windows.Add((Window)SettingsDialog);
                    SettingsDialog.Page = "main";
                }
                else
                    dialogOptions.Close(true);
            }
            else if (!flag1 && flag2)
            {
                SettingsDialog.Close(false);
            }
            else
            {
                if (flag1 || flag2)
                    return;
                SettingsDialog.CloseButtonClicked = false;
            }
        }

        public void drawAnimalIcons(Pawn animal)
        {
            var num1 = 0;
            if (animal.Dead || animal.holder != null)
                return;
            var drawPos = animal.DrawPos;
            if (!settings.ShowAggressive || animal.MentalStateDef != MentalStateDefOf.Berserk && animal.MentalStateDef != MentalStateDefOf.Manhunter)
                return;
            var bodyPos = drawPos;
            var num2 = num1;
            var num3 = 1;
            var num4 = num2 + num3;
            var num5 = 1;
            var red = Color.red;
            DrawIcon(bodyPos, num2, (Icons)num5, red);
        }

        public void drawColonistIcons(Pawn colonist)
        {
            var num1 = 0;
            PawnStats pawnStats;
            if (colonist.Dead || colonist.holder != null || (!stats_dict.TryGetValue(colonist, out pawnStats) || colonist.drafter == null) || colonist.skills == null)
                return;
            var drawPos = colonist.DrawPos;
            if (colonist.skills.GetSkill(SkillDefOf.Melee).TotallyDisabled && colonist.skills.GetSkill(SkillDefOf.Shooting).TotallyDisabled)
            {
                if (settings.ShowPacific)
                    DrawIcon(drawPos, num1++, Icons.Pacific, Color.white);
            }
            else if (settings.ShowUnarmed && colonist.equipment.Primary == null && !colonist.IsPrisonerOfColony)
                DrawIcon(drawPos, num1++, Icons.Unarmed, Color.white);
            if (settings.ShowIdle && colonist.mindState.IsIdle)
                DrawIcon(drawPos, num1++, Icons.Idle, Color.white);
            if (settings.ShowDraft && colonist.drafter.Drafted)
                DrawIcon(drawPos, num1++, Icons.Draft, Color.white);
            if (settings.ShowSad && (double)colonist.needs.mood.CurLevel < (double)settings.LimitMoodLess)
                DrawIcon(drawPos, num1++, Icons.Sad, colonist.needs.mood.CurLevel / settings.LimitMoodLess);
            if (settings.ShowHungry && (double)colonist.needs.food.CurLevel < (double)settings.LimitFoodLess)
                DrawIcon(drawPos, num1++, Icons.Hungry, colonist.needs.food.CurLevel / settings.LimitFoodLess);
            if (settings.ShowTired && (double)colonist.needs.rest.CurLevel < (double)settings.LimitRestLess)
                DrawIcon(drawPos, num1++, Icons.Tired, colonist.needs.rest.CurLevel / settings.LimitRestLess);
            if (settings.ShowNaked && !pawnStats.isNudist && colonist.apparel.PsychologicallyNude)
                DrawIcon(drawPos, num1++, Icons.Naked, Color.white);
            if (settings.ShowCold && (double)pawnStats.tooCold > 0.0)
            {
                if ((double)pawnStats.tooCold >= 0.0)
                {
                    if ((double)pawnStats.tooCold <= 1.0)
                        DrawIcon(drawPos, num1++, Icons.Freezing, pawnStats.tooCold, new Color(1f, 1f, 1f, 0.3f), new Color(0.86f, 0.86f, 1f, 1f));
                    else if ((double)pawnStats.tooCold <= 1.5)
                        DrawIcon(drawPos, num1++, Icons.Freezing, (float)(((double)pawnStats.tooCold - 1.0) * 2.0), new Color(0.86f, 0.86f, 1f, 1f), new Color(1f, 0.86f, 0.86f));
                    else
                        DrawIcon(drawPos, num1++, Icons.Freezing, (float)(((double)pawnStats.tooCold - 1.5) * 2.0), new Color(1f, 0.86f, 0.86f), Color.red);
                }
            }
            else if (settings.ShowHot && (double)pawnStats.tooHot > 0.0 && (double)pawnStats.tooCold >= 0.0)
            {
                if ((double)pawnStats.tooHot <= 1.0)
                    DrawIcon(drawPos, num1++, Icons.Hot, pawnStats.tooHot, new Color(1f, 1f, 1f, 0.3f), new Color(1f, 0.7f, 0.0f, 1f));
                else
                    DrawIcon(drawPos, num1++, Icons.Hot, pawnStats.tooHot - 1f, new Color(1f, 0.7f, 0.0f, 1f), Color.red);
            }
            if (settings.ShowAggressive && colonist.MentalStateDef == MentalStateDefOf.Berserk)
                DrawIcon(drawPos, num1++, Icons.Aggressive, Color.red);
            if (settings.ShowLeave && colonist.MentalStateDef == MentalStateDefOf.GiveUpExit)
                DrawIcon(drawPos, num1++, Icons.Leave, Color.red);
            if (settings.ShowDazed && colonist.MentalStateDef == MentalStateDefOf.DazedWander)
                DrawIcon(drawPos, num1++, Icons.Dazed, Color.red);
            if (colonist.MentalStateDef == MentalStateDefOf.PanicFlee)
                DrawIcon(drawPos, num1++, Icons.Panic, Color.yellow);
            if (settings.ShowDrunk)
            {
                if (colonist.MentalStateDef == MentalStateDefOf.BingingAlcohol)
                    DrawIcon(drawPos, num1++, Icons.Drunk, Color.red);
                else if ((double)pawnStats.drunkness > 0.05)
                    DrawIcon(drawPos, num1++, Icons.Drunk, pawnStats.drunkness, new Color(1f, 1f, 1f, 0.2f), Color.white, new Color(1f, 0.1f, 0.0f));
            }
            if (settings.ShowEffectiveness && (double)pawnStats.total_efficiency < (double)settings.LimitEfficiencyLess)
                DrawIcon(drawPos, num1++, Icons.Effectiveness, pawnStats.total_efficiency / settings.LimitEfficiencyLess);
            if (settings.ShowDisease && (double)pawnStats.diseaseDisappearance < (double)settings.LimitDiseaseLess)
                DrawIcon(drawPos, num1++, Icons.Disease, pawnStats.diseaseDisappearance / settings.LimitDiseaseLess);
            if (settings.ShowBloodloss && (double)pawnStats.bleedRate > 0.0)
                DrawIcon(drawPos, num1++, Icons.Bloodloss, new Color(1f, 0.0f, 0.0f, pawnStats.bleedRate));
            if (settings.ShowApparelHealth && (double)pawnStats.apparelHealth < (double)settings.LimitApparelHealthLess)
            {
                var bodyPos = drawPos;
                var num2 = num1;
                var num3 = 1;
                var num4 = num2 + num3;
                var num5 = 19;
                var num6 = (double)pawnStats.apparelHealth / (double)settings.LimitApparelHealthLess;
                DrawIcon(bodyPos, num2, (Icons)num5, (float)num6);
            }
            if (!settings.ShowTargetPoint || !(pawnStats.targetPos != Vector3.zero))
                return;
            DrawIcon(pawnStats.targetPos, Vector3.zero, Icons.Target, targetColor);
        }

        public virtual void OnGUI()
        {
            if (!inGame || Find.TickManager.Paused)
                updateOptionsDialog();
            if (!iconsEnabled || !inGame)
                return;
            foreach (var pawn in Find.Map.mapPawns.AllPawns)
            {
                if (pawn != null && pawn.RaceProps != null)
                {
                    if (pawn.RaceProps.Animal)
                        drawAnimalIcons(pawn);
                    else if (pawn.IsColonist || pawn.IsPrisonerOfColony)
                        drawColonistIcons(pawn);
                }
            }
        }

        public virtual void Update()
        {
            if (!inGame)
                return;
            if (Input.GetKeyUp(KeyCode.F11))
            {
                iconsEnabled = !iconsEnabled;
                if (iconsEnabled)
                    Messages.Message("PSI.Enabled".Translate(), MessageSound.Standard);
                else
                    Messages.Message("PSI.Disabled".Translate(), MessageSound.Standard);
            }
            worldScale = (float)Screen.height / (2f * Camera.current.orthographicSize);
        }
    }
}
