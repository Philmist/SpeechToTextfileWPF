using System;
using System.Collections.Generic;
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
using Windows.Media.SpeechRecognition;
using Windows.Media.Capture;
using Windows.Globalization;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace SpeechToTextfileWPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool microphonePermissionGained = false;
        private volatile static bool isListening = false;
        private SpeechRecognizer speechRecognizer = null;
        private ConcurrentQueue<string> recognizeTextQueue = new ConcurrentQueue<string>();

        private Encoding textFileEncoding = Encoding.UTF8;

        public MainWindow()
        {
            InitializeComponent();
            RefreshSecondSlider.Value = 0;
        }

        private static uint HResultPrivacyStatementDeclined = 0x80045509;
        private static int NoCaptureDevicesHResult = -1072845856;
        /// <summary>
        /// MicrosoftのUWPサンプルからコピーしたマイクの使用許可を求めるコード。
        /// </summary>
        /// <returns>正常に許可が取れたらtrueを返す</returns>
        public async static Task<bool> RequestMicrophonePermission()
        {
            try
            {
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
                settings.MediaCategory = MediaCategory.Speech;
                MediaCapture capture = new MediaCapture();

                await capture.InitializeAsync(settings);
            }
            catch (TypeLoadException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception exception)
            {
                if (exception.HResult == NoCaptureDevicesHResult)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
            return true;
        }

        private async void RecognizeButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    RecognizeButton.IsEnabled = false;
                });
            });

            if (!microphonePermissionGained)
            {
                microphonePermissionGained = await RequestMicrophonePermission();
            }
            else
            {
                RecognizeText.Text = "Requesting Microphone Permission is denied.";
                RecognizeButton.IsEnabled = true;
                return;
            }

            if (microphonePermissionGained == true && speechRecognizer == null)
            {
                try
                {
                    await InitializeRecognizer(SpeechRecognizer.SystemSpeechLanguage);
                }
                catch (Exception)
                {
                    RecognizeText.Text = "Initiate recognizer failed.";
                    return;
                }
            }
            
            if (speechRecognizer == null) { return; }

            if (isListening == false)
            {
                if (speechRecognizer.State != SpeechRecognizerState.Idle) { return; }
                isListening = true;
                recognizeTextQueue = new ConcurrentQueue<string>();
                await StartContinuousDictation().ConfigureAwait(false);
                
                await ChangeDictationControlPanelState(false);
            }
            else
            {
                isListening = false;
                await speechRecognizer.ContinuousRecognitionSession.CancelAsync();
                
                await ChangeDictationControlPanelState(true);
            }

            await Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    RecognizeButton.IsEnabled = true;
                });
            });
        }

        /// <summary>
        /// 制御用のコントロールを無効化したり有効化したりする
        /// </summary>
        /// <param name="state">有効化したいならtrue</param>
        private async Task ChangeDictationControlPanelState(bool state)
        {
            await Task.Run(() =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    switch (state)
                    {
                        case true:
                            FileSelectButton.IsEnabled = true;
                            RefreshSecondSlider.IsEnabled = true;
                            RecognizeButton.Content = "Dictate";
                            break;
                        default:
                            FileSelectButton.IsEnabled = false;
                            RefreshSecondSlider.IsEnabled = false;
                            RecognizeButton.Content = "Stop";
                            break;
                    }
                });
            });
        }

        private async void RecognitionResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs generatedEventArgs)
        {
            if (generatedEventArgs.Result.Confidence == SpeechRecognitionConfidence.High || generatedEventArgs.Result.Confidence == SpeechRecognitionConfidence.Medium)
            {
                recognizeTextQueue.Enqueue(generatedEventArgs.Result.Text);
                await Task.Run(() =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        RecognizeText.Text = generatedEventArgs.Result.Text;

                    });
                });
            }
        }

        private async Task StartContinuousDictation()
        {
            if (isListening == true && speechRecognizer.State == SpeechRecognizerState.Idle)
            {
                await speechRecognizer.ContinuousRecognitionSession.StartAsync();
            }
        }

        private async void RecognitionCompleted(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionCompletedEventArgs eventArgs)
        {
            if (isListening == true)
            {
                await speechRecognizer.ContinuousRecognitionSession.StartAsync();
            }
        }

        /// <summary>
        /// 認識機(recognizer)が設定されているのならそれを破棄する。
        /// もし認識中なら認識を止める。
        /// </summary>
        private async Task DisposeRecognizer()
        {
            if (speechRecognizer != null)
            {
                if (isListening == true)
                {
                    isListening = false;
                    await speechRecognizer.ContinuousRecognitionSession.CancelAsync();
                }

                speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= RecognitionResultGenerated;
                speechRecognizer.ContinuousRecognitionSession.Completed -= RecognitionCompleted;

                speechRecognizer.Dispose();
                speechRecognizer = null;
            }
        }

        private async Task InitializeRecognizer(Language language)
        {
            await DisposeRecognizer();

            speechRecognizer = new SpeechRecognizer(language);
            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            speechRecognizer.Constraints.Add(dictationConstraint);
            SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                RecognizeButton.IsEnabled = false;
                RecognizeText.Text = result.Status.ToString();
            }

            speechRecognizer.ContinuousRecognitionSession.ResultGenerated += RecognitionResultGenerated;
            speechRecognizer.ContinuousRecognitionSession.Completed += RecognitionCompleted;
        }
    }
}
