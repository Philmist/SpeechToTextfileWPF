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
        private BouyomiChanClient bouyomiChan = null;

        private SpeechRecognizer recognizer = null;
        private SpeechConfig speechConfig = null;
        private AudioConfig audioConfig = null;
        private SourceLanguageConfig sourceLanguage = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 現在のコントロールを有効化したり無効化したりする
        /// </summary>
        /// <param name="state">有効化するならtrue</param>
        /// <returns></returns>
        private async Task changeControls(bool state)
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
        private async Task changeStateRecognizeButton(bool state)
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    RecognizeButton.IsEnabled = state;
                });
            });

        }

        private async void UpdateRecognizedText(object sender, SpeechRecognitionEventArgs eventArgs)
        {
            if (eventArgs.Result.Reason == ResultReason.RecognizedSpeech)
            {
                textQueue.Enqueue(eventArgs.Result.Text);
                await Task.Run(() => {
                    Dispatcher.Invoke(() => { RecognizedTextBlock.Text = eventArgs.Result.Text; });
                });
            }
        }

        private async void RecognizeCanceled(object sender, SpeechRecognitionCanceledEventArgs eventArgs)
        {
            isListening = false;
            await changeControls(true);
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    RecognizedTextBlock.Text = eventArgs.Reason.ToString();
                });
            });
        }

        private async void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {
            await changeStateRecognizeButton(false);

            if (isListening == false)
            {
                if (audioConfig == null)
                {
                    audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                }
                
                await changeControls(false);
                try
                {
                    string subscriptionKey = AzureSubscriptionKeyTextBox.Text.Trim();
                    Uri endpointUri = new Uri(AzureServiceEndpointUriTextBox.Text.Trim());
                    if (subscriptionKey.Length == 0 || endpointUri.IsWellFormedOriginalString() != true)
                    {
                        await changeStateRecognizeButton(true);
                        await changeControls(true);
                        return;
                    }
                    textQueue = new ConcurrentQueue<string>();
                    speechConfig = SpeechConfig.FromEndpoint(endpointUri, subscriptionKey);
                    sourceLanguage = SourceLanguageConfig.FromLanguage("ja-JP");
                    recognizer = new SpeechRecognizer(speechConfig, sourceLanguage, audioConfig);

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
                    await recognizer.StartContinuousRecognitionAsync();

                    double refreshSecond = RefreshSecondSlider.Value;
                    _ = Task.Run(() => writeToTextfile(refreshSecond));

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    if (bouyomiChan != null)
                    {
                        bouyomiChan.Dispose();
                        bouyomiChan = null;
                    }
                    await changeControls(true);
                    await changeStateRecognizeButton(true);
                    return;
                }
            }
            else
            {
                recognizer.Recognized -= UpdateRecognizedText;
                recognizer.Canceled -= RecognizeCanceled;
                await recognizer.StopContinuousRecognitionAsync();
                if (bouyomiChan != null)
                {
                    bouyomiChan.Dispose();
                    bouyomiChan = null;
                }
                isListening = false;
                await changeControls(true);
            }

            await changeStateRecognizeButton(true);
        }

        private void FileSelectButton_Click(object sender, RoutedEventArgs eventArgs)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = ".txt";
            openFileDialog.Filter = "Text File (.txt)|*.txt";

            Nullable<bool> result = openFileDialog.ShowDialog();
            if (result == true)
            {
                fileName = openFileDialog.FileName;
                FileNameLabel.Text = fileName;
            }
        }

        private void writeToTextfile(double refreshSecond)
        {
            string trimedFileName = fileName.Trim();
            Action<string> writeToFile = (string t) =>
            {
                if (trimedFileName != "")
                {
                    System.IO.File.WriteAllText(trimedFileName, t, Encoding.UTF8);
                }
            };
            Action<string> talk = (string t) => {
                if (bouyomiChan != null)
                {
                    bouyomiChan.AddTalkTask(t);
                }
            };

            string text;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while(isListening == true)
            {
                if (textQueue.TryDequeue(out text))
                {
                    try
                    {
                        writeToFile(text);
                        talk(text);
                        stopwatch.Restart();
                    }
                    catch (Exception)
                    {
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
                    catch (Exception)
                    {
                        break;
                    }
                }
            }

            stopwatch.Stop();
        }
    }
}
