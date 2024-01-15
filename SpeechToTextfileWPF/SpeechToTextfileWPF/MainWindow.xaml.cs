#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Net.Sockets;
using FNF.Utility;
using System.Globalization;
using System.Threading;
using NAudio.CoreAudioApi;
using Microsoft.CognitiveServices.Speech.Translation;

namespace SpeechToTextfileWPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private volatile bool isListening = false;
        private ConcurrentQueue<string> textQueue;
        private string fileName = "";
        private BouyomiChanClient? bouyomiChan = null;

        // private SpeechRecognizer? recognizer = null;
        private TranslationRecognizer? recognizer = null;
        private SpeechConfig? speechConfig = null;
        private AudioConfig? audioConfig = null;
        private SourceLanguageConfig? sourceLanguage = null;

        private SpeechTranslationConfig? translationConfig = null;

        private List<AudioIF>? audioIFs;

        public class AudioIF
        {
            public string FriendlyName { get; set; } = "";
            public string ID { get; set; } = "";
        }

        public MainWindow()
        {
            InitializeComponent();
            textQueue = new ConcurrentQueue<string>();
            var audioEnumerator = new MMDeviceEnumerator();
            audioIFs = audioEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).Select(i => new AudioIF { FriendlyName = i.FriendlyName, ID = i.ID }).ToList();
            Dispatcher.Invoke(() => {
                CultureInfo cultureInfo = Thread.CurrentThread.CurrentUICulture;
                this.RecognizedTextBlock.Text = cultureInfo.Name;
                this.AudioInterfaceComboBox.ItemsSource = audioIFs;
                this.AudioInterfaceComboBox.SelectedIndex = 0;
            });
        }

        /// <summary>
        /// 現在のコントロールを有効化したり無効化したりする
        /// </summary>
        /// <param name="state">有効化するならtrue</param>
        /// <returns></returns>
        private async Task ChangeControls(bool state)
        {
            switch (state)
            {
                case true:
                    Dispatcher.Invoke(() =>
                    {
                        RefreshSecondSlider.IsEnabled = true;
                        AzureSubscriptionPanel.Visibility = Visibility.Visible;
                        FileSelectButton.IsEnabled = true;
                        BouyomiChanCheckBox.IsEnabled = true;
                        TextSendUrl.IsEnabled = true;
                        RecognizeButton.Content = "Recognize";
                    });
                    break;
                default:
                    await Task.Run(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            RefreshSecondSlider.IsEnabled = false;
                            AzureSubscriptionPanel.Visibility = Visibility.Hidden;
                            FileSelectButton.IsEnabled = false;
                            BouyomiChanCheckBox.IsEnabled = false;
                            TextSendUrl.IsEnabled = false;
                            RecognizeButton.Content = "Stop";
                        });
                    });
                    break;
            }
        }

        /// <summary>
        /// 認識用のボタンを有効化したり無効化したりする
        /// </summary>
        /// <param name="state">有効化するならtrue、無効化するならfalse</param>
        /// <returns></returns>
        private async Task ChangeStateRecognizeButton(bool state)
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    RecognizeButton.IsEnabled = state;
                });
            });

        }

        private async void UpdateRecognizedText(object sender, TranslationRecognitionEventArgs eventArgs)
        {
            if (eventArgs.Result.Reason == ResultReason.TranslatedSpeech)
            {
                // textQueue.Enqueue(eventArgs.Result.Text);
                Debug.WriteLine(eventArgs.Result.Translations.First().Value);
                textQueue.Enqueue(eventArgs.Result.Translations.First().Value);
                await Task.Run(() => {
                    Dispatcher.Invoke(() => { RecognizedTextBlock.Text = eventArgs.Result.Translations.First().Value; });
                });
            }
        }

        private async void RecognizeCanceled(object sender, TranslationRecognitionCanceledEventArgs eventArgs)
        {
            isListening = false;
            await ChangeControls(true);
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    RecognizedTextBlock.Text = eventArgs.ErrorDetails;
                });
            });
        }

        private async void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {
            await ChangeStateRecognizeButton(false);

            if (isListening == false)
            {
                var selectedIF = audioIFs[this.AudioInterfaceComboBox.SelectedIndex];
                if (selectedIF != null)
                {
                    Debug.WriteLine(selectedIF.FriendlyName);
                    audioConfig = AudioConfig.FromMicrophoneInput(selectedIF.ID);
                }
                else
                {
                    Debug.WriteLine("Default Mic");
                    audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                }
                
                await ChangeControls(false);
                try
                {
                    string subscriptionKey = AzureSubscriptionKeyTextBox.Text.Trim();
                    string region = AzureServiceRegionTextBox.Text.Trim();
                    if (subscriptionKey.Length == 0 || region.Length == 0)
                    {
                        await ChangeStateRecognizeButton(true);
                        await ChangeControls(true);
                        return;
                    }
                    textQueue = new ConcurrentQueue<string>();
                    translationConfig = SpeechTranslationConfig.FromSubscription(subscriptionKey, region);
                    translationConfig.SpeechRecognitionLanguage = LanguageFrom.Text.Trim();
                    translationConfig.AddTargetLanguage(LanguageTo.Text.Trim());
                    /*
                    speechConfig = SpeechConfig.FromEndpoint(endpointUri, subscriptionKey);
                    sourceLanguage = SourceLanguageConfig.FromLanguage("ja-JP");
                    recognizer = new SpeechRecognizer(speechConfig, sourceLanguage, audioConfig);
                    */
                    recognizer = new TranslationRecognizer(translationConfig, audioConfig);
                    

                    if (bouyomiChan != null)
                    {
                        bouyomiChan.Dispose();
                        bouyomiChan = null;
                    }

                    if (BouyomiChanCheckBox.IsChecked == true)
                    {
                        bouyomiChan = new BouyomiChanClient();
                    }

                    isListening = true;
                    recognizer.Recognized += UpdateRecognizedText;
                    recognizer.Canceled += RecognizeCanceled;
                    recognizer.SpeechStartDetected += (object s, RecognitionEventArgs args) => { Debug.WriteLine("S"); };
                    recognizer.SpeechEndDetected += (object s, RecognitionEventArgs args) => { Debug.WriteLine("E"); };

                    await recognizer.StartContinuousRecognitionAsync();

                    double refreshSecond = RefreshSecondSlider.Value;
                    string textUrl = TextSendUrl.Text;
                    _ = Task.Run(() => WriteToTextfile(refreshSecond, textUrl));

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    if (bouyomiChan != null)
                    {
                        bouyomiChan.Dispose();
                        bouyomiChan = null;
                    }
                    await ChangeControls(true);
                    await ChangeStateRecognizeButton(true);
                    return;
                }
            }
            else
            {
                if (recognizer != null) {
                    recognizer.Recognized -= UpdateRecognizedText;
                    recognizer.Canceled -= RecognizeCanceled;
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
                if (bouyomiChan != null)
                {
                    bouyomiChan.Dispose();
                    bouyomiChan = null;
                }
                isListening = false;
                await ChangeControls(true);
            }

            await ChangeStateRecognizeButton(true);
        }

        private void FileSelectButton_Click(object sender, RoutedEventArgs eventArgs)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Text File (.txt)|*.txt"
            };

            Nullable<bool> result = openFileDialog.ShowDialog();
            if (result == true)
            {
                fileName = openFileDialog.FileName;
                FileNameLabel.Text = fileName;
            }
        }

        private void WriteToTextfile(double refreshSecond, string url = "")
        {
            string trimedFileName = fileName.Trim();
            void writeToFile(string t)
            {
                if (trimedFileName != "")
                {
                    System.IO.File.WriteAllText(trimedFileName, t, Encoding.UTF8);
                }
            }
            void talk(string t)
            {
                if (bouyomiChan != null)
                {
                    bouyomiChan.AddTalkTask(t);
                }
            }

            var textHttpSender = new TextHttpSender(url.Trim());
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while(isListening == true)
            {
                if (textQueue.TryDequeue(out string text))
                {
                    try
                    {
                        writeToFile(text);
                        talk(text);
                        var recognizedText = new TextHttpSender.RecognizedText { text = text };
                        _ = textHttpSender.Send(recognizedText);
                        stopwatch.Restart();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(String.Format("Exception: {0}", ex.Message));
                        if (typeof(System.Net.WebException) == ex.GetType())
                        {
                            continue;
                        }
                        break;
                    }
                }

                if (refreshSecond > 0 && (stopwatch.ElapsedMilliseconds > (refreshSecond * 1000)))
                {
                    try
                    {
                        writeToFile("");
                        stopwatch.Restart();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(String.Format("Exception: {0}", ex.Message));
                        break;
                    }
                }
            }

            stopwatch.Stop();
        }

        private void OpenSettingDialogButton_Click(object sender, RoutedEventArgs eventArgs)
        {
            var Dialog = new SettingDialog();
            Dialog.ShowDialog();
        }
    }
}
