using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using DiscordRPC;
using DiscordRPC.Message;
using QTubePresence.Core;
using Label = System.Windows.Controls.Label;

namespace QTubePresence
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow
    {
        private readonly BrowserTitleGrabber _browserTitleGrabber;

        private static readonly Regex YoutubeTitlePattern =
            new Regex(@"(\(.*\))?.*(. - YouTube)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex YoutubeNotificationPattern =
            new Regex(@"(\([0-9]*\)?)", RegexOptions.Compiled);

        private static DiscordRpcClient _client;

        private readonly Label _playingLabel = new Label();
        private readonly Label _statusLabel = new Label();

        public MainWindow()
        {
            _browserTitleGrabber = new BrowserTitleGrabber();

            InitializeComponent();

            //Create a client connection with discord
            _client = new DiscordRpcClient("856048168060059668");
            _client.OnReady += OnReadyTrigger;
            //Set the status of the client
            _statusLabel.Content = "Connecting...";
            //initialize the client
            _client.Initialize();


            RootGrid.Children.Add(_playingLabel);
            RootGrid.Children.Add(_statusLabel);

            Task.Run(UpdateTitleList);
        }


        private void OnReadyTrigger(object sender, ReadyMessage args)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { _statusLabel.Content = "Connected!"; }),
                DispatcherPriority.Render);
        }

        private async Task UpdateTitleList()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        var titleList = await _browserTitleGrabber.GetTabTitles(BrowserTitleGrabber.EBrowser.Firefox);
                        var titleToShow = ParseFirstVideoTitle(titleList);


                        try
                        {
                            _client.SetPresence(new RichPresence()
                            {
                                Details = titleToShow,
                                State = "Listening",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "q-tube-logo",
                                    LargeImageText = titleToShow
                                },
                                Buttons = new[]
                                {
                                    new Button()
                                    {
                                        Label = "Listen (WIP)",
                                        Url = "https://moe.quill/",
                                    }
                                }
                            });
                        }
                        catch (Exception ignored)
                        {
                            // ignored
                        }

                        _playingLabel.Content = titleToShow;
                    }), DispatcherPriority.Render);
                    Thread.Sleep(3000);
                }
            });
        }

        private static string ParseFirstVideoTitle(IEnumerable<string> tabNames)
        {
            foreach (var tabName in tabNames)
            {
                if (!YoutubeTitlePattern.IsMatch(tabName)) continue;
                var nameWithoutNotification = YoutubeNotificationPattern.Replace(tabName, "");
                var nameWithoutYoutube = nameWithoutNotification.Replace(" - YouTube", "");
                return nameWithoutYoutube;
            }

            return "";
        }
    }
}