using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace PSI
{
    // ReSharper disable once InconsistentNaming
    internal class Dialog_Settings : Window
    {
        public string Page = "main";
        public bool CloseButtonClicked = true;
        public Window OptionsDialog;

        private static string _modPath;

        public Dialog_Settings()
        {
            _modPath = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons";

            closeOnEscapeKey = false;
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = false;
            forcePause = false;
        }

        private void DoHeading(Listing_Standard listing, string translatorKey, bool translate = true)
        {
            Text.Font = GameFont.Medium;
            listing.DoLabel(translate ? translatorKey.Translate() : translatorKey);
            Text.Font = GameFont.Small;
        }

        private void FillPageMain(Listing_Standard listing)
        {
            if (listing.DoTextButton("PSI.Settings.IconSet".Translate() + PSI.Settings.iconSet))
            {
                var options = PSI.IconSets.Select(setname => new FloatMenuOption(setname, () =>
                {
                    PSI.Settings.iconSet = setname;
                    PSI.Materials = new Materials(setname);
                    PSI.Materials.ReloadTextures(true);
                })).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = _modPath + "/Presets/Complete/";
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = strArray.Select(setname => new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                {
                    try
                    {
                        PSI.Settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                        PSI.SaveSettings();
                        PSI.Reinit();
                    }
                    catch (IOException)
                    {
                        Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                    }
                })).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.DoGap();

            DoHeading(listing, "PSI.Settings.Advanced");

            if (listing.DoTextButton("PSI.Settings.VisibilityButton".Translate()))
                Page = "showhide";

            if (listing.DoTextButton("PSI.Settings.ArrangementButton".Translate()))
                Page = "arrange";

            if (!listing.DoTextButton("PSI.Settings.SensitivityButton".Translate()))
                return;

            Page = "limits";
        }

        private void FillPageLimits(Listing_Standard listing)
        {
            DoHeading(listing, "PSI.Settings.Sensitivity.Header");
            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = _modPath + "/Presets/Sensitivity/";
                Log.Message(path);
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = strArray.Select(setname => new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                {
                    try
                    {
                        var settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                        PSI.Settings.limit_BleedMult = settings.limit_BleedMult;
                        PSI.Settings.limit_DiseaseLess = settings.limit_DiseaseLess;
                        PSI.Settings.limit_EfficiencyLess = settings.limit_EfficiencyLess;
                        PSI.Settings.limit_FoodLess = settings.limit_FoodLess;
                        PSI.Settings.limit_MoodLess = settings.limit_MoodLess;
                        PSI.Settings.limit_RestLess = settings.limit_RestLess;
                        PSI.Settings.limit_ApparelHealthLess = settings.limit_ApparelHealthLess;
                        PSI.Settings.limit_TempComfortOffset = settings.limit_TempComfortOffset;
                    }
                    catch (IOException)
                    {
                        Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                    }
                })).ToList();

                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.DoGap();

            listing.DoLabel("PSI.Settings.Sensitivity.Bleeding".Translate() + ("PSI.Settings.Sensitivity.Bleeding." + Math.Round(PSI.Settings.limit_BleedMult - 0.25)).Translate());
            PSI.Settings.limit_BleedMult = listing.DoSlider(PSI.Settings.limit_BleedMult, 0.5f, 5f);

            listing.DoLabel("PSI.Settings.Sensitivity.Injured".Translate() + (int)(PSI.Settings.limit_EfficiencyLess * 100.0) + "%");
            PSI.Settings.limit_EfficiencyLess = listing.DoSlider(PSI.Settings.limit_EfficiencyLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Food".Translate() + (int)(PSI.Settings.limit_FoodLess * 100.0) + "%");
            PSI.Settings.limit_FoodLess = listing.DoSlider(PSI.Settings.limit_FoodLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Mood".Translate() + (int)(PSI.Settings.limit_MoodLess * 100.0) + "%");
            PSI.Settings.limit_MoodLess = listing.DoSlider(PSI.Settings.limit_MoodLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Rest".Translate() + (int)(PSI.Settings.limit_RestLess * 100.0) + "%");
            PSI.Settings.limit_RestLess = listing.DoSlider(PSI.Settings.limit_RestLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.ApparelHealth".Translate() + (int)(PSI.Settings.limit_ApparelHealthLess * 100.0) + "%");
            PSI.Settings.limit_ApparelHealthLess = listing.DoSlider(PSI.Settings.limit_ApparelHealthLess, 0.01f, 0.99f);

            listing.DoLabel("PSI.Settings.Sensitivity.Temperature".Translate() + (int)PSI.Settings.limit_TempComfortOffset + "C");
            PSI.Settings.limit_TempComfortOffset = listing.DoSlider(PSI.Settings.limit_TempComfortOffset, -10f, 10f);

            if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                return;

            Page = "main";
        }

        private void FillPageShowHide(Listing_Standard listing)
        {
            listing.OverrideColumnWidth = 230f;
            DoHeading(listing, "PSI.Settings.Visibility.Header");
            listing.OverrideColumnWidth = 125f;
            listing.DoLabelCheckbox("PSI.Settings.Visibility.TargetPoint".Translate(), ref PSI.Settings.show_TargetPoint);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Aggressive".Translate(), ref PSI.Settings.show_Aggressive);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Dazed".Translate(), ref PSI.Settings.show_Dazed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Leave".Translate(), ref PSI.Settings.show_Leave);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Draft".Translate(), ref PSI.Settings.show_Draft);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Idle".Translate(), ref PSI.Settings.show_Idle);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Unarmed".Translate(), ref PSI.Settings.show_Unarmed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hungry".Translate(), ref PSI.Settings.show_Hungry);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Sad".Translate(), ref PSI.Settings.show_Sad);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Tired".Translate(), ref PSI.Settings.show_Tired);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Disease".Translate(), ref PSI.Settings.show_Disease);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.NightOwl".Translate(), ref PSI.Settings.show_NightOwl);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Greedy".Translate(), ref PSI.Settings.show_Greedy);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.DeadColonists".Translate(), ref PSI.Settings.show_DeadColonists);

            listing.OverrideColumnWidth = 230f;
            if (listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                Page = "main";
            listing.OverrideColumnWidth = 125f;
            listing.NewColumn();
            DoHeading(listing, " ", false);
            DoHeading(listing, " ", false);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Injury".Translate(), ref PSI.Settings.show_Effectiveness);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Bloodloss".Translate(), ref PSI.Settings.show_Bloodloss);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hot".Translate(), ref PSI.Settings.show_Hot);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Cold".Translate(), ref PSI.Settings.show_Cold);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Naked".Translate(), ref PSI.Settings.show_Naked);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Drunk".Translate(), ref PSI.Settings.show_Drunk);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.ApparelHealth".Translate(), ref PSI.Settings.show_ApparelHealth);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Pacific".Translate(), ref PSI.Settings.show_Pacific);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Jealous".Translate(), ref PSI.Settings.show_Jealous);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Lovers".Translate(), ref PSI.Settings.show_Lovers);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Prosthophile".Translate(), ref PSI.Settings.show_Prosthophile);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Prosthophobe".Translate(), ref PSI.Settings.show_Prosthophobe);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.RoomStatus".Translate(), ref PSI.Settings.show_RoomStatus);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.LeftUnburied".Translate(), ref PSI.Settings.show_LeftUnburied);
        }

        private void FillPageArrangement(Listing_Standard listing)
        {
            DoHeading(listing, "PSI.Settings.Arrangement.Header");

            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = _modPath + "/Presets/Position/";
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = strArray.Select(setname => new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                {
                    try
                    {
                        var settings = XmlLoader.ItemFromXmlFile<ModSettings>(setname);
                        PSI.Settings.iconDistanceX = settings.iconDistanceX;
                        PSI.Settings.iconDistanceY = settings.iconDistanceY;
                        PSI.Settings.iconOffsetX = settings.iconOffsetX;
                        PSI.Settings.iconOffsetY = settings.iconOffsetY;
                        PSI.Settings.iconsHorizontal = settings.iconsHorizontal;
                        PSI.Settings.iconsScreenScale = settings.iconsScreenScale;
                        PSI.Settings.iconsInColumn = settings.iconsInColumn;
                        PSI.Settings.iconSize = settings.iconSize;
                    }
                    catch (IOException)
                    {
                        Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                    }
                })).ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            }

            var num = (int)(PSI.Settings.iconSize * 4.5);

            if (num > 8)
                num = 8;
            else if (num < 0)
                num = 0;

            listing.DoLabel("PSI.Settings.Arrangement.IconSize".Translate() + ("PSI.Settings.SizeLabel." + num).Translate());
            PSI.Settings.iconSize = listing.DoSlider(PSI.Settings.iconSize, 0.5f, 2f);

            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconPosition".Translate(), (int)(PSI.Settings.iconDistanceX * 100.0), " , ", (int)(PSI.Settings.iconDistanceY * 100.0)));
            PSI.Settings.iconDistanceX = listing.DoSlider(PSI.Settings.iconDistanceX, -2f, 2f);
            PSI.Settings.iconDistanceY = listing.DoSlider(PSI.Settings.iconDistanceY, -2f, 2f);

            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconOffset".Translate(), (int)(PSI.Settings.iconOffsetX * 100.0), " , ", (int)(PSI.Settings.iconOffsetY * 100.0)));
            PSI.Settings.iconOffsetX = listing.DoSlider(PSI.Settings.iconOffsetX, -2f, 2f);
            PSI.Settings.iconOffsetY = listing.DoSlider(PSI.Settings.iconOffsetY, -2f, 2f);

            listing.DoLabelCheckbox("PSI.Settings.Arrangement.Horizontal".Translate(), ref PSI.Settings.iconsHorizontal);

            listing.DoLabelCheckbox("PSI.Settings.Arrangement.ScreenScale".Translate(), ref PSI.Settings.iconsScreenScale);

            listing.DoLabel("PSI.Settings.Arrangement.IconsPerColumn".Translate() + PSI.Settings.iconsInColumn);

            PSI.Settings.iconsInColumn = (int)listing.DoSlider(PSI.Settings.iconsInColumn, 1f, 9f);

            if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                return;

            Page = "main";
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (OptionsDialog == null)
                return;

            var rect = OptionsDialog.currentWindowRect;

            currentWindowRect = new Rect(rect.xMax - 300f, rect.yMin, 300f, rect.height);

            var listing = new Listing_Standard(inRect);

            DoHeading(listing, "Pawn State Icons", false);

            listing.OverrideColumnWidth = currentWindowRect.width;

            switch (Page)
            {
                case "showhide":
                    FillPageShowHide(listing);
                    break;
                case "arrange":
                    FillPageArrangement(listing);
                    break;
                case "limits":
                    FillPageLimits(listing);
                    break;
                default:
                    FillPageMain(listing);
                    break;
            }

            listing.End();
        }

        public override void PreClose()
        {
            PSI.SaveSettings();
            PSI.Reinit();
            CloseButtonClicked = true;
            base.PreClose();
        }
    }
}
