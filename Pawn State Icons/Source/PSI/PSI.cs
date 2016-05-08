using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;


namespace PSI
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once InconsistentNaming
    internal class PSI : MonoBehaviour
    {
        private static double _fDelta;

        private static bool _inGame;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static Dictionary<Pawn, PawnStats> _statsDict = new Dictionary<Pawn, PawnStats>();

        private static bool _iconsEnabled = true;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private static Dialog_Settings _modSettingsDialog = new Dialog_Settings();

        private static readonly Color _targetColor = new Color(1f, 1f, 1f, 0.6f);

        private static float _worldScale = 1f;

        private static PawnCapacityDef[] _pawnCapacities;

        private static Vector3[] _iconPosVectors;

        private static string _modPath;

        public static ModSettings Settings = new ModSettings();
        public static Materials Materials = new Materials();
        public static string[] IconSets = { "default" };


        public PSI()
        {
            _modPath = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons";
            Reinit();
        }

        public static void Reinit(bool reloadSettings = true, bool reloadIconSet = true, bool recalcIconPos = true)
        {
            _pawnCapacities = new[]
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
                PawnCapacityDefOf.Talking,
            };

            if (reloadSettings)
            {
                Settings = LoadSettings();
            }
            if (reloadIconSet)
            {
                Materials = new Materials(Settings.iconSet);
                var modSettings = XmlLoader.ItemFromXmlFile<ModSettings>(_modPath + "/Textures/UI/Overlays/PawnStateIcons/" + Settings.iconSet + "/iconset.cfg");
                Settings.iconSizeMult = modSettings.iconSizeMult;
                Materials.ReloadTextures(true);
            }
            if (recalcIconPos)
            {
                RecalcIconPositions();
            }
        }

        private static ModSettings LoadSettings(string settingsFile = "settings.cfg")
        {
            var result = XmlLoader.ItemFromXmlFile<ModSettings>(_modPath + "/" + settingsFile);
            var settingsFilePath = _modPath + "/Textures/UI/Overlays/PawnStateIcons/";
            if (!Directory.Exists(settingsFilePath)) return result;
            IconSets = Directory.GetDirectories(settingsFilePath);
            for (var i = 0; i < IconSets.Length; i++)
            {
                IconSets[i] = new DirectoryInfo(IconSets[i]).Name;
            }
            return result;
        }

        public static void SaveSettings(string path = "settings.cfg")
        {
            XmlSaver.SaveDataObject(Settings, _modPath + "/" + path);
        }

        #region Draw icons

        private static void DrawIcon(Vector3 bodyPos, Vector3 posOffset, Icons icon, Color color)
        {

            var material = Materials[icon];
            if (material == null)
                return;
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                material.color = color;
                var guiColor = GUI.color;
                GUI.color = color;
                Vector2 vector2;
                if (Settings.iconsScreenScale)
                {
                    vector2 = bodyPos.ToScreenPosition();
                    vector2.x += posOffset.x * 45f;
                    vector2.y -= posOffset.z * 45f;
                }
                else
                    vector2 = (bodyPos + posOffset).ToScreenPosition();
                var num1 = _worldScale;
                if (Settings.iconsScreenScale)
                    num1 = 45f;
                var num2 = num1 * (Settings.iconSizeMult * 0.5f);
                var position = new Rect(vector2.x, vector2.y, num2 * Settings.iconSize, num2 * Settings.iconSize);
                position.x -= position.width * 0.5f;
                position.y -= position.height * 0.5f;
                GUI.DrawTexture(position, material.mainTexture, ScaleMode.ScaleToFit, true);
                GUI.color = guiColor;
            });

        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, Color color)
        {
            DrawIcon(bodyPos, _iconPosVectors[num], icon, color);
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v)
        {
            DrawIcon(bodyPos, num, icon, new Color(1f, v, v));
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2)
        {
            DrawIcon(bodyPos, num, icon, Color.Lerp(c1, c2, v));
        }

        private static void DrawIcon(Vector3 bodyPos, int num, Icons icon, float v, Color c1, Color c2, Color c3)
        {
            DrawIcon(bodyPos, num, icon,
                v < 0.5 ? Color.Lerp(c1, c2, v * 2f) : Color.Lerp(c2, c3, (float)((v - 0.5) * 2.0)));
        }

        private static void RecalcIconPositions()
        {
            _iconPosVectors = new Vector3[18];
            for (var index = 0; index < _iconPosVectors.Length; ++index)
            {
                var num1 = index / Settings.iconsInColumn;
                var num2 = index % Settings.iconsInColumn;
                if (Settings.iconsHorizontal)
                {
                    var num3 = num1;
                    num1 = num2;
                    num2 = num3;
                }
                _iconPosVectors[index] = new Vector3((float)(-0.600000023841858 * Settings.iconDistanceX - 0.550000011920929 * Settings.iconSize * Settings.iconOffsetX * num1), 3f, (float)(-0.600000023841858 * Settings.iconDistanceY + 0.550000011920929 * Settings.iconSize * Settings.iconOffsetY * num2));
            }
        }

        #endregion

        private static void UpdateColonistStats(Pawn colonist)
        {

            if (!_statsDict.ContainsKey(colonist))
                _statsDict.Add(colonist, new PawnStats());

            var pawnStats = _statsDict[colonist];

            // Efficiency
            var efficiency = (from activity in _pawnCapacities where activity != PawnCapacityDefOf.Consciousness select colonist.health.capacities.GetEfficiency(activity)).Concat(new[] {10f}).Min();

            if (efficiency < 0.0)
                efficiency = 0.0f;

            pawnStats.pawn_TotalEfficiency = efficiency;

            // Target
            pawnStats.TargetPos = Vector3.zero;

            if (colonist.jobs.curJob != null)
            {
                var jobDriver = colonist.jobs.curDriver;
                var job = colonist.jobs.curJob;
                var targetInfo = job.targetA;

                if (jobDriver is JobDriver_HaulToContainer || jobDriver is JobDriver_HaulToCell || (jobDriver is JobDriver_FoodDeliver || jobDriver is JobDriver_FoodFeedPatient) || jobDriver is JobDriver_TakeToBed)
                    targetInfo = job.targetB;

                var doBill = jobDriver as JobDriver_DoBill;
                if (doBill != null)
                {
                    var jobDriverDoBill = doBill;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (jobDriverDoBill.workLeft == 0.0)
                        targetInfo = job.targetA;
                    else if (jobDriverDoBill.workLeft <= 0.00999999977648258)
                        targetInfo = job.targetB;
                }

                if (jobDriver is JobDriver_Hunt && colonist.carrier?.CarriedThing != null)
                    targetInfo = job.targetB;

                if (job.def == JobDefOf.Wait)
                    targetInfo = null;

                if (jobDriver is JobDriver_Ingest)
                    targetInfo = null;

                if (job.def == JobDefOf.LayDown && colonist.InBed())
                    targetInfo = null;

                if (!job.playerForced && job.def == JobDefOf.Goto)
                    targetInfo = null;

                var flag = targetInfo == null;
                if (!flag)
                {
                    var vector3 = targetInfo.Cell.ToVector3Shifted();
                    pawnStats.TargetPos = vector3 + new Vector3(0.0f, 3f, 0.0f);
                }
            }

            // Temperature
            var temperatureForCell = GenTemperature.GetTemperatureForCell(colonist.Position);
            pawnStats.pawn_TooCold = (float)((colonist.ComfortableTemperatureRange().min - (double)Settings.limit_TempComfortOffset - temperatureForCell) / 10.0);
            pawnStats.pawn_TooHot = (float)((temperatureForCell - (double)colonist.ComfortableTemperatureRange().max - Settings.limit_TempComfortOffset) / 10.0);
            pawnStats.pawn_TooCold = Mathf.Clamp(pawnStats.pawn_TooCold, 0.0f, 2f);
            pawnStats.pawn_TooHot = Mathf.Clamp(pawnStats.pawn_TooHot, 0.0f, 2f);


            // Health Calc
            pawnStats.DiseaseDisappearance = 1f;
            pawnStats.pawn_Drunkness = DrugUtility.DrunknessPercent(colonist);


            foreach (var hediff in colonist.health.hediffSet.hediffs)
            {
                var hediffWithComps = (HediffWithComps)hediff;
                if (hediffWithComps != null
                    && !hediffWithComps.FullyImmune()
                    && hediffWithComps.Visible 
                    && !hediffWithComps.IsOld() 
                    && (hediffWithComps.CurStage == null 
                    || hediffWithComps.CurStage.everVisible) 
                    && (hediffWithComps.def.tendable 
                    || hediffWithComps.def.naturallyHealed) 
                    && hediffWithComps.def.PossibleToDevelopImmunity())

                    pawnStats.DiseaseDisappearance = Math.Min(pawnStats.DiseaseDisappearance, colonist.health.immunity.GetImmunity(hediffWithComps.def));
            }

            // Apparel Calc
            var num1 = 999f;
            var wornApparel = colonist.apparel.WornApparel;
            foreach (var apparel in wornApparel)
            {
                var num2 = apparel.HitPoints / (float)apparel.MaxHitPoints;
                if (num2 >= 0.0 && num2 < (double)num1)
                    num1 = num2;
            }

            pawnStats.pawn_ApparelHealth = num1;

            pawnStats.pawn_BleedRate = Mathf.Clamp01(colonist.health.hediffSet.BleedingRate * Settings.limit_BleedMult);

            _statsDict[colonist] = pawnStats;

        }

        private static bool HasMood(Pawn pawn, string mood)
        {
            return pawn.needs.mood.thoughts.DistinctThoughtDefs.Contains(ThoughtDef.Named(mood));
        }

        // ReSharper disable once UnusedMember.Global
        public virtual void FixedUpdate()
        {
            _fDelta += Time.fixedDeltaTime;

            if (_fDelta < 0.1)
                return;
            _fDelta = 0.0;
            _inGame = GameObject.Find("CameraMap");

            if (!_inGame || !_iconsEnabled)
                return;

            foreach (var colonist in Find.Map.mapPawns.FreeColonistsAndPrisoners)
            {
                UpdateColonistStats(colonist);
            }
        }

        private static void UpdateOptionsDialog()
        {
            var dialogOptions = Find.WindowStack.WindowOfType<Dialog_Options>();
            var flag1 = dialogOptions != null;
            var flag2 = Find.WindowStack.IsOpen(typeof(Dialog_Settings));

            if (flag1 && flag2)
            {
                _modSettingsDialog.OptionsDialog = dialogOptions;
                RecalcIconPositions();
            }
            else if (flag1)
            {
                if (!_modSettingsDialog.CloseButtonClicked)
                {
                    Find.UIRoot.windows.Add(_modSettingsDialog);
                    _modSettingsDialog.Page = "main";
                }
                else
                    dialogOptions.Close();
            }
            else if (flag2)
            {
                _modSettingsDialog.Close(false);
            }
            else
            {
                _modSettingsDialog.CloseButtonClicked = false;
            }
        }

        private static void DrawAnimalIcons(Pawn animal)
        {
            if (animal.Dead || animal.holder != null)
                return;
            var drawPos = animal.DrawPos;

            if (!Settings.show_Aggressive || animal.MentalStateDef != MentalStateDefOf.Berserk && animal.MentalStateDef != MentalStateDefOf.Manhunter)
                return;
            var bodyPos = drawPos;
            DrawIcon(bodyPos, 0, Icons.Aggressive, Color.red);
        }

        private static void DrawColonistIcons(Pawn colonist)
        {
            var counter = 0;
            PawnStats pawnStats;
            if (colonist.Dead || colonist.holder != null || (!_statsDict.TryGetValue(colonist, out pawnStats) || colonist.drafter == null) || colonist.skills == null)
                return;

            var drawPos = colonist.DrawPos;

            // Target Point 
            if (Settings.show_TargetPoint || (pawnStats.TargetPos != Vector3.zero))
                DrawIcon(pawnStats.TargetPos, Vector3.zero, Icons.Target, _targetColor);

            // Pacifc + Unarmed
            if (colonist.skills.GetSkill(SkillDefOf.Melee).TotallyDisabled && colonist.skills.GetSkill(SkillDefOf.Shooting).TotallyDisabled)
            {
                if (Settings.show_Pacific)
                    DrawIcon(drawPos, counter++, Icons.Pacific, Color.white);
            }
            else if (Settings.show_Unarmed && colonist.equipment.Primary == null && !colonist.IsPrisonerOfColony)
                DrawIcon(drawPos, counter++, Icons.Unarmed, Color.white);

            // Idle
            if (Settings.show_Idle && colonist.mindState.IsIdle)
                DrawIcon(drawPos, counter++, Icons.Idle, Color.white);

            //Drafted
            if (Settings.show_Draft && colonist.drafter.Drafted)
                DrawIcon(drawPos, counter++, Icons.Draft, Color.white);

            // Bad Mood
            if (Settings.show_Sad && colonist.needs.mood.CurLevel < (double)Settings.limit_MoodLess)
                DrawIcon(drawPos, counter++, Icons.Sad, colonist.needs.mood.CurLevel / Settings.limit_MoodLess);

            // Hungry
            if (Settings.show_Hungry && colonist.needs.food.CurLevel < (double)Settings.limit_FoodLess)
                DrawIcon(drawPos, counter++, Icons.Hungry, colonist.needs.food.CurLevel / Settings.limit_FoodLess);

            //Tired
            if (Settings.show_Tired && colonist.needs.rest.CurLevel < (double)Settings.limit_RestLess)
                DrawIcon(drawPos, counter++, Icons.Tired, colonist.needs.rest.CurLevel / Settings.limit_RestLess);

            // Too Cold & too hot
            if (Settings.show_Cold && pawnStats.pawn_TooCold > 0.0)
            {
                if (pawnStats.pawn_TooCold >= 0.0)
                {
                    if (pawnStats.pawn_TooCold <= 1.0)
                        DrawIcon(drawPos, counter++, Icons.Freezing, pawnStats.pawn_TooCold, new Color(1f, 1f, 1f, 0.3f), new Color(0.86f, 0.86f, 1f, 1f));
                    else if (pawnStats.pawn_TooCold <= 1.5)
                        DrawIcon(drawPos, counter++, Icons.Freezing, (float)((pawnStats.pawn_TooCold - 1.0) * 2.0), new Color(0.86f, 0.86f, 1f, 1f), new Color(1f, 0.86f, 0.86f));
                    else
                        DrawIcon(drawPos, counter++, Icons.Freezing, (float)((pawnStats.pawn_TooCold - 1.5) * 2.0), new Color(1f, 0.86f, 0.86f), Color.red);
                }
            }
            else if (Settings.show_Hot && pawnStats.pawn_TooHot > 0.0 && pawnStats.pawn_TooCold >= 0.0)
            {
                if (pawnStats.pawn_TooHot <= 1.0)
                    DrawIcon(drawPos, counter++, Icons.Hot, pawnStats.pawn_TooHot, new Color(1f, 1f, 1f, 0.3f), new Color(1f, 0.7f, 0.0f, 1f));
                else
                    DrawIcon(drawPos, counter++, Icons.Hot, pawnStats.pawn_TooHot - 1f, new Color(1f, 0.7f, 0.0f, 1f), Color.red);
            }

            // Mental States
            if (Settings.show_Aggressive && colonist.MentalStateDef == MentalStateDefOf.Berserk)
                DrawIcon(drawPos, counter++, Icons.Aggressive, Color.red);

            if (Settings.show_Leave && colonist.MentalStateDef == MentalStateDefOf.GiveUpExit)
                DrawIcon(drawPos, counter++, Icons.Leave, Color.red);

            if (Settings.show_Dazed && colonist.MentalStateDef == MentalStateDefOf.DazedWander)
                DrawIcon(drawPos, counter++, Icons.Dazed, Color.red);

            if (colonist.MentalStateDef == MentalStateDefOf.PanicFlee)
                DrawIcon(drawPos, counter++, Icons.Panic, Color.yellow);

            // Binging on alcohol
            if (Settings.show_Drunk)
            {
                if (colonist.MentalStateDef == MentalStateDefOf.BingingAlcohol)
                    DrawIcon(drawPos, counter++, Icons.Drunk, Color.red);
                else if (pawnStats.pawn_Drunkness > 0.05)
                    DrawIcon(drawPos, counter++, Icons.Drunk, pawnStats.pawn_Drunkness, new Color(1f, 1f, 1f, 0.2f), Color.white, Color.red);
            }

            // Effectiveness
            if (Settings.show_Effectiveness && pawnStats.pawn_TotalEfficiency < (double)Settings.limit_EfficiencyLess)
                DrawIcon(drawPos, counter++, Icons.Effectiveness, pawnStats.pawn_TotalEfficiency / Settings.limit_EfficiencyLess);

            // Disease
            if (Settings.show_Disease)
            {
                if (HasMood(colonist, "Sick"))
                    DrawIcon(drawPos, counter++, Icons.Sick, Color.white);

                if (colonist.health.ShouldBeTendedNow && !colonist.health.ShouldDoSurgeryNow)
                    DrawIcon(drawPos, counter++, Icons.MedicalAttention, new Color(1f, 0.5f, 0f));
                else
                if (colonist.health.ShouldBeTendedNow && colonist.health.ShouldDoSurgeryNow)
                {
                    DrawIcon(drawPos, counter++, Icons.MedicalAttention, new Color(1f, 0.5f, 0f));
                    DrawIcon(drawPos, counter++, Icons.MedicalAttention, Color.blue);
                }
                else
                if (colonist.health.ShouldDoSurgeryNow)
                    DrawIcon(drawPos, counter++, Icons.MedicalAttention, Color.blue);

                if ((pawnStats.DiseaseDisappearance < Settings.limit_DiseaseLess) && (colonist.health.summaryHealth.SummaryHealthPercent < 1f))
                {
                    if ((pawnStats.DiseaseDisappearance / Settings.limit_DiseaseLess) < colonist.health.summaryHealth.SummaryHealthPercent)
                        DrawIcon(drawPos, counter++, Icons.Disease, pawnStats.DiseaseDisappearance / Settings.limit_DiseaseLess, Color.red, Color.white);
                    else
                        DrawIcon(drawPos, counter++, Icons.Disease, colonist.health.summaryHealth.SummaryHealthPercent, Color.red, Color.white);
                }

                else if (pawnStats.DiseaseDisappearance < Settings.limit_DiseaseLess)
                    DrawIcon(drawPos, counter++, Icons.Disease, pawnStats.DiseaseDisappearance / Settings.limit_DiseaseLess, Color.red, Color.white);

                else if (colonist.health.summaryHealth.SummaryHealthPercent < 1f)
                    DrawIcon(drawPos, counter++, Icons.Disease, colonist.health.summaryHealth.SummaryHealthPercent, Color.red, Color.white);
            }

            // Bloodloss
            if (Settings.show_Bloodloss && pawnStats.pawn_BleedRate > 0.0f)
                DrawIcon(drawPos, counter++, Icons.Bloodloss, pawnStats.pawn_BleedRate, Color.red, Color.white);


            // Apparel
            if (Settings.show_ApparelHealth && pawnStats.pawn_ApparelHealth < (double)Settings.limit_ApparelHealthLess)
            {
                var bodyPos = drawPos;
                var num2 = counter;
                var num6 = pawnStats.pawn_ApparelHealth / (double)Settings.limit_ApparelHealthLess;
                DrawIcon(bodyPos, num2, Icons.ApparelHealth, (float)num6);
            }

            // Traiits and bad thoughts

            // Room Status
            if (Settings.show_RoomStatus && HasMood(colonist, "Crowded"))
            {
                DrawIcon(drawPos, counter++, Icons.Crowded, Color.white);
            }

            if (Settings.show_Prosthophile && HasMood(colonist, "ProsthophileNoProsthetic"))
            {
                DrawIcon(drawPos, counter++, Icons.Prosthophile, Color.white);
            }

            if (Settings.show_Prosthophobe && HasMood(colonist, "ProsthophobeUnhappy"))
            {
                DrawIcon(drawPos, counter++, Icons.Prosthophobe, Color.white);
            }

            if (Settings.show_NightOwl && HasMood(colonist, "NightOwlDuringTheDay"))
            {
                DrawIcon(drawPos, counter++, Icons.NightOwl, Color.white);
            }

            if (Settings.show_Greedy && HasMood(colonist, "Greedy"))
            {
                DrawIcon(drawPos, counter++, Icons.Greedy, Color.white);
            }

            if (Settings.show_Jealous && HasMood(colonist, "Jealous"))
            {
                DrawIcon(drawPos, counter++, Icons.Jealous, Color.white);
            }

            if (Settings.show_Lovers && HasMood(colonist, "WantToSleepWithSpouseOrLover"))
            {
                DrawIcon(drawPos, counter++, Icons.Love, Color.red);
            }

            if (Settings.show_Naked && HasMood(colonist, "Naked"))
            {
                DrawIcon(drawPos, counter++, Icons.Naked, Color.white);
            }

            if (Settings.show_LeftUnburied && HasMood(colonist, "ColonistLeftUnburied"))
            {
                DrawIcon(drawPos, counter++, Icons.LeftUnburied, Color.white);
            }

            if (!Settings.show_DeadColonists) return;

            var color25To21 = Color.red;
            var color20To16 = new Color(1f, 0.5f, 0f);
            var color15To11 = Color.yellow;
            var color10 = new Color(1f, 1f, 0.5f);
            var color9AndLess = Color.white;
            var colorMoodBoost = Color.green;

            // Close Family & friends / -25
            if (HasMood(colonist, "MySonDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color25To21);
            }
            if (HasMood(colonist, "MyDaughterDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color25To21);
            }
            if (HasMood(colonist, "MyFianceDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color25To21);
            }
            if (HasMood(colonist, "MyFianceeDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color25To21);
            }
            if (HasMood(colonist, "MyLoverDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color25To21);
            }

            // -20

            if (HasMood(colonist, "MyHusbandDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color20To16);
            }
            if (HasMood(colonist, "MyWifeDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color20To16);
            }

            // Friend depends on social
            if (HasMood(colonist, "PawnWithGoodOpinionDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color10);
            }

            // Not-so-close family / -15
            if (HasMood(colonist, "MyBrotherDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color15To11);
            }
            if (HasMood(colonist, "MySisterDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color15To11);
            }
            if (HasMood(colonist, "MyGrandchildDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color15To11);
            }

            // -10
            if (HasMood(colonist, "MyFatherDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color10);
            }
            if (HasMood(colonist, "MyMotherDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color10);
            }
            if (HasMood(colonist, "MyNieceDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color10);
            }
            if (HasMood(colonist, "MyNephewDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color10);
            }
            if (HasMood(colonist, "MyAuntDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color10);
            }
            if (HasMood(colonist, "MyUncleDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color10);
            }

            //
            if (HasMood(colonist, "BondedAnimalDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color15To11);
            }
          
            // Not family, more whiter icon
            if (HasMood(colonist, "KilledColonist"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color9AndLess);
            }
            if (HasMood(colonist, "KilledColonyAnimal"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color9AndLess);
            }
          
            // Everyone else / < -10
            if (HasMood(colonist, "MyGrandparentDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color9AndLess);
            }
            if (HasMood(colonist, "MyHalfSiblingDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color9AndLess);
            }
            if (HasMood(colonist, "MyCousinDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color9AndLess);
            }
            if (HasMood(colonist, "MyKinDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, color9AndLess);
            }

            // Non family
            //if (HasMood(colonist, "WitnessedDeathAlly"))
            //{
            //    DrawIcon(drawPos, counter++, Icons.DeadColonist, color_9andLess);
            //}
            //if (HasMood(colonist, "WitnessedDeathStranger"))
            //{
            //    DrawIcon(drawPos, counter++, Icons.DeadColonist, color_9andLess);
            //}
            if (HasMood(colonist, "WitnessedDeathStrangerBloodlust"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, colorMoodBoost);
            }
            if (HasMood(colonist, "KilledHumanlikeBloodlust"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, colorMoodBoost);
            }

            // Haters
            if (HasMood(colonist, "PawnWithBadOpinionDied"))
            {
                DrawIcon(drawPos, counter++, Icons.DeadColonist, colorMoodBoost);
            }

            if (HasMood(colonist, "KilledMajorColonyEnemy"))
            {
                DrawIcon(drawPos, counter, Icons.DeadColonist, colorMoodBoost);
            }
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public virtual void OnGUI()
        {
            if (!_inGame || Find.TickManager.Paused)
                UpdateOptionsDialog();
            if (!_iconsEnabled || !_inGame)
                return;
            foreach (var pawn in Find.Map.mapPawns.AllPawns)
            {
                if (pawn?.RaceProps == null) continue;
                if (pawn.RaceProps.Animal)
                    DrawAnimalIcons(pawn);
                else if (pawn.IsColonist || pawn.IsPrisonerOfColony)
                    DrawColonistIcons(pawn);
            }
        }

        // ReSharper disable once UnusedMember.Global
        public virtual void Update()
        {
            if (!_inGame)
                return;
            if (Input.GetKeyUp(KeyCode.F11))
            {
                _iconsEnabled = !_iconsEnabled;
                Messages.Message(_iconsEnabled ? "PSI.Enabled".Translate() : "PSI.Disabled".Translate(),
                    MessageSound.Standard);
            }
            _worldScale = Screen.height / (2f * Camera.current.orthographicSize);
        }
    }
}
