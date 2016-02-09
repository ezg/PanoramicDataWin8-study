using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using PanoramicDataWin8.model.view;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace PanoramicDataWin8.view.setting
{
    public class Settings
    {
        public Mode Mode { get; set; }
        public Dataset Dataset { get; set; }
        public int Seed { get; set; }
        public string Participant { get; set; }

        public Settings() { }

        public Settings Clone()
        {
            return new Settings() {Dataset = this.Dataset, Mode = this.Mode, Seed = this.Seed, Participant = this.Participant};
        }
    }

    public sealed partial class SettingsDialogView : ContentDialog
    {
        public Settings Settings { get; private set; }

        public bool Load { get; set; }

        public SettingsDialogView(Settings settings = null)
        {
            this.InitializeComponent();

            Load = false;

            if (settings == null)
            {
                Settings = new Settings();
            }
            else
            {
                Settings = settings;
            }

            if (Settings.Mode == Mode.batch)
            {
                rbBatch.IsChecked = true;
            }
            else if (Settings.Mode == Mode.instantaneous)
            {
                rbInstantaneous.IsChecked = true;
            }
            else if (Settings.Mode == Mode.progressive)
            {
                rbProgressive.IsChecked = true;
            }


            if (Settings.Dataset == Dataset.ds1)
            {
                rbDs1.IsChecked = true;
            }
            else if (Settings.Dataset == Dataset.ds2)
            {
                rbDs2.IsChecked = true;
            }
            else if (Settings.Dataset == Dataset.ds3)
            {
                rbDs3.IsChecked = true;
            }
            else if (Settings.Dataset == Dataset.ds4)
            {
                rbDs4.IsChecked = true;
            }

            sliderSeed.Value = Settings.Seed;
            textBoxParticipant.Text = Settings.Participant;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        { 
            ContentDialogButtonClickDeferral deferral = args.GetDeferral();
            /*if (await SomeAsyncSignInOperation())
            {
                this.Settings = SignInSettings.SignInOK;
            }
            else
            {
                this.Settings = SignInSettings.SignInFail;
            }*/
            Load = true;
            this.Settings = new Settings();
            Settings.Seed = (int)sliderSeed.Value;
            Settings.Mode = rbBatch.IsChecked.Value ? Mode.batch : rbInstantaneous.IsChecked.Value ? Mode.instantaneous : Mode.progressive;
            Settings.Dataset = rbDs1.IsChecked.Value ? Dataset.ds1 : rbDs2.IsChecked.Value ? Dataset.ds2 : rbDs3.IsChecked.Value ? Dataset.ds3 : Dataset.ds4;
            Settings.Participant = textBoxParticipant.Text.Trim();
            deferral.Complete();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Load = false;
        }
    }
}