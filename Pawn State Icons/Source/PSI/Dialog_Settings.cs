using System;
using System.Collections.Generic;
using System.IO;
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

        public Dialog_Settings()
        {
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
            if (listing.DoTextButton("PSI.Settings.IconSet".Translate() + PSI.settings.IconSet))
            {
                var options = new List<FloatMenuOption>();
                foreach (var str in PSI.iconSets)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(setname, () =>
                    {
                        PSI.settings.IconSet = setname;
                        PSI.materials = new Materials(setname);
                        PSI.materials.reloadTextures(true);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Presets/Complete/";
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = new List<FloatMenuOption>();
                foreach (var str in strArray)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                    {
                        try
                        {
                            PSI.settings = XmlLoader.ItemFromXmlFile<Settings>(setname);
                            PSI.saveSettings();
                            PSI.reinit();
                        }
                        catch (IOException)
                        {
                            Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                        }
                    }));
                }
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
                var path = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Presets/Sensitivity/";
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = new List<FloatMenuOption>();
                foreach (var str in strArray)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                    {
                        try
                        {
                            var settings = XmlLoader.ItemFromXmlFile<Settings>(setname);
                            PSI.settings.LimitBleedMult = settings.LimitBleedMult;
                            PSI.settings.LimitDiseaseLess = settings.LimitDiseaseLess;
                            PSI.settings.LimitEfficiencyLess = settings.LimitEfficiencyLess;
                            PSI.settings.LimitFoodLess = settings.LimitFoodLess;
                            PSI.settings.LimitMoodLess = settings.LimitMoodLess;
                            PSI.settings.LimitRestLess = settings.LimitRestLess;
                            PSI.settings.LimitApparelHealthLess = settings.LimitApparelHealthLess;
                            PSI.settings.LimitTempComfortOffset = settings.LimitTempComfortOffset;
                        }
                        catch (IOException)
                        {
                            Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                        }
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            listing.DoGap();
            listing.DoLabel("PSI.Settings.Sensitivity.Bleeding".Translate() + ("PSI.Settings.Sensitivity.Bleeding." + Math.Round(PSI.settings.LimitBleedMult - 0.25)).Translate());
            PSI.settings.LimitBleedMult = listing.DoSlider(PSI.settings.LimitBleedMult, 0.5f, 5f);
            listing.DoLabel("PSI.Settings.Sensitivity.Injured".Translate() + (int)(PSI.settings.LimitEfficiencyLess * 100.0) + "%");
            PSI.settings.LimitEfficiencyLess = listing.DoSlider(PSI.settings.LimitEfficiencyLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.Food".Translate() + (int)(PSI.settings.LimitFoodLess * 100.0) + "%");
            PSI.settings.LimitFoodLess = listing.DoSlider(PSI.settings.LimitFoodLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.Mood".Translate() + (int)(PSI.settings.LimitMoodLess * 100.0) + "%");
            PSI.settings.LimitMoodLess = listing.DoSlider(PSI.settings.LimitMoodLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.Rest".Translate() + (int)(PSI.settings.LimitRestLess * 100.0) + "%");
            PSI.settings.LimitRestLess = listing.DoSlider(PSI.settings.LimitRestLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.ApparelHealth".Translate() + (int)(PSI.settings.LimitApparelHealthLess * 100.0) + "%");
            PSI.settings.LimitApparelHealthLess = listing.DoSlider(PSI.settings.LimitApparelHealthLess, 0.01f, 0.99f);
            listing.DoLabel("PSI.Settings.Sensitivity.Temperature".Translate() + (int)PSI.settings.LimitTempComfortOffset + "C");
            PSI.settings.LimitTempComfortOffset = listing.DoSlider(PSI.settings.LimitTempComfortOffset, -10f, 10f);
            if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                return;
            Page = "main";
        }

        private void FillPageShowHide(Listing_Standard listing)
        {
            listing.OverrideColumnWidth = 230f;
            DoHeading(listing, "PSI.Settings.Visibility.Header");
            listing.OverrideColumnWidth = 95f;
            listing.DoLabelCheckbox("PSI.Settings.Visibility.TargetPoint".Translate(), ref PSI.settings.ShowTargetPoint);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Aggressive".Translate(), ref PSI.settings.ShowAggressive);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Dazed".Translate(), ref PSI.settings.ShowDazed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Leave".Translate(), ref PSI.settings.ShowLeave);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Draft".Translate(), ref PSI.settings.ShowDraft);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Idle".Translate(), ref PSI.settings.ShowIdle);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Unarmed".Translate(), ref PSI.settings.ShowUnarmed);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hungry".Translate(), ref PSI.settings.ShowHungry);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Sad".Translate(), ref PSI.settings.ShowSad);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Tired".Translate(), ref PSI.settings.ShowTired);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Disease".Translate(), ref PSI.settings.ShowDisease);
            listing.OverrideColumnWidth = 230f;
            if (listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                Page = "main";
            listing.OverrideColumnWidth = 95f;
            listing.NewColumn();
            DoHeading(listing, " ", false);
            DoHeading(listing, " ", false);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Injury".Translate(), ref PSI.settings.ShowEffectiveness);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Bloodloss".Translate(), ref PSI.settings.ShowBloodloss);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Hot".Translate(), ref PSI.settings.ShowHot);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Cold".Translate(), ref PSI.settings.ShowCold);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Naked".Translate(), ref PSI.settings.ShowNaked);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Drunk".Translate(), ref PSI.settings.ShowDrunk);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.ApparelHealth".Translate(), ref PSI.settings.ShowApparelHealth);
            listing.DoLabelCheckbox("PSI.Settings.Visibility.Pacific".Translate(), ref PSI.settings.ShowPacific);
        }

        private void FillPageArrangement(Listing_Standard listing)
        {
            DoHeading(listing, "PSI.Settings.Arrangement.Header");
            if (listing.DoTextButton("PSI.Settings.LoadPresetButton".Translate()))
            {
                var strArray = new string[0];
                var path = GenFilePaths.CoreModsFolderPath + "/Pawn State Icons/Presets/Position/";
                if (Directory.Exists(path))
                    strArray = Directory.GetFiles(path, "*.cfg");
                var options = new List<FloatMenuOption>();
                foreach (var str in strArray)
                {
                    var setname = str;
                    options.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(setname), () =>
                    {
                        try
                        {
                            var settings = XmlLoader.ItemFromXmlFile<Settings>(setname);
                            PSI.settings.IconDistanceX = settings.IconDistanceX;
                            PSI.settings.IconDistanceY = settings.IconDistanceY;
                            PSI.settings.IconOffsetX = settings.IconOffsetX;
                            PSI.settings.IconOffsetY = settings.IconOffsetY;
                            PSI.settings.IconsHorizontal = settings.IconsHorizontal;
                            PSI.settings.IconsScreenScale = settings.IconsScreenScale;
                            PSI.settings.IconsInColumn = settings.IconsInColumn;
                            PSI.settings.IconSize = settings.IconSize;
                        }
                        catch (IOException)
                        {
                            Log.Error("PSI.Settings.LoadPreset.UnableToLoad".Translate() + setname);
                        }
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            var num = (int)(PSI.settings.IconSize * 4.5);
            if (num > 8)
                num = 8;
            else if (num < 0)
                num = 0;
            listing.DoLabel("PSI.Settings.Arrangement.IconSize".Translate() + ("PSI.Settings.SizeLabel." + num).Translate());
            PSI.settings.IconSize = listing.DoSlider(PSI.settings.IconSize, 0.5f, 2f);
            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconPosition".Translate(), (int) (PSI.settings.IconDistanceX * 100.0), " , ", (int) (PSI.settings.IconDistanceY * 100.0)));
            PSI.settings.IconDistanceX = listing.DoSlider(PSI.settings.IconDistanceX, -2f, 2f);
            PSI.settings.IconDistanceY = listing.DoSlider(PSI.settings.IconDistanceY, -2f, 2f);
            listing.DoLabel(string.Concat("PSI.Settings.Arrangement.IconOffset".Translate(), (int) (PSI.settings.IconOffsetX * 100.0), " , ", (int) (PSI.settings.IconOffsetY * 100.0)));
            PSI.settings.IconOffsetX = listing.DoSlider(PSI.settings.IconOffsetX, -2f, 2f);
            PSI.settings.IconOffsetY = listing.DoSlider(PSI.settings.IconOffsetY, -2f, 2f);
            listing.DoLabelCheckbox("PSI.Settings.Arrangement.Horizontal".Translate(), ref PSI.settings.IconsHorizontal);
            listing.DoLabelCheckbox("PSI.Settings.Arrangement.ScreenScale".Translate(), ref PSI.settings.IconsScreenScale);
            listing.DoLabel("PSI.Settings.Arrangement.IconsPerColumn".Translate() + PSI.settings.IconsInColumn);
            PSI.settings.IconsInColumn = (int)listing.DoSlider(PSI.settings.IconsInColumn, 1f, 9f);
            if (!listing.DoTextButton("PSI.Settings.ReturnButton".Translate()))
                return;
            Page = "main";
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (OptionsDialog == null)
                return;
            var rect = OptionsDialog.currentWindowRect;
            currentWindowRect = new Rect(rect.xMax - 240f, rect.yMin, 240f, rect.height);
            var listing = new Listing_Standard(inRect);
            DoHeading(listing, "Pawn State Icons", false);
            listing.OverrideColumnWidth = currentWindowRect.width;
            if (Page == "showhide")
                FillPageShowHide(listing);
            else if (Page == "arrange")
                FillPageArrangement(listing);
            else if (Page == "limits")
                FillPageLimits(listing);
            else
                FillPageMain(listing);
            listing.End();
        }

        public override void PreClose()
        {
            PSI.saveSettings();
            PSI.reinit();
            CloseButtonClicked = true;
            base.PreClose();
        }
    }
}
