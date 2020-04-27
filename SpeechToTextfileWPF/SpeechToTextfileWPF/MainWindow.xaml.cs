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

namespace SpeechToTextfileWPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private string subscriptionKey;
        private string serviceRegion;

        private bool isListening = false;
        private ConcurrentQueue<string> textQueue;

        private SpeechRecognizer recognizer = null;
        private SpeechConfig speechConfig = null;
        private AudioConfig audioConfig = null;

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
                await Task.Run(() => {
                    Dispatcher.Invoke(() => { RecognizedTextBlock.Text = eventArgs.Result.Text; });
                });
            }
        }

        private async void RecognizeCanceled(object sender, SpeechRecognitionCanceledEventArgs eventArgs)
        {
            await changeControls(true);
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    RecognizedTextBlock.Text = eventArgs.Reason.ToString();
                });
            });
            isListening = false;
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
                    speechConfig = SpeechConfig.FromSubscription(AzureSubscriptionKeyTextBox.Text, AzureServiceRegionTextBox.Text);
                    recognizer = new SpeechRecognizer(speechConfig, audioConfig);
                    recognizer.Recognized += UpdateRecognizedText;
                    recognizer.Canceled += RecognizeCanceled;
                    await recognizer.StartContinuousRecognitionAsync();
                    isListening = true;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    await changeControls(true);
                    await changeStateRecognizeButton(true);
                    return;
                }

            }
            else
            {
                await recognizer.StopContinuousRecognitionAsync();
                recognizer.Recognized -= UpdateRecognizedText;
                recognizer.Canceled -= RecognizeCanceled;
                isListening = false;
            }

            await changeStateRecognizeButton(true);
        }
    }
}
