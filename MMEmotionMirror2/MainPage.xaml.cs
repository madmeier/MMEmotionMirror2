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
using Windows.UI.Xaml.Media.Imaging;

using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;

using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;

using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;



// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MMEmotionMirror2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture mediaCapture;
        private StorageFile photoFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        private DispatcherTimer timer;

        private const int showsAnger = 0;
        private const int showsContempt = 1;
        private const int showsDisgust = 2;
        private const int showsFear = 3;
        private const int showsHappyiness = 4;
        private const int showsNeutrality = 5;
        private const int showsSadness = 6;
        private const int showsSurprise = 7;

        private SolidColorBrush blueBrush = new SolidColorBrush(Windows.UI.Colors.Blue);
        private SolidColorBrush whiteBrush = new SolidColorBrush(Windows.UI.Colors.White);
        private Windows.UI.Xaml.Shapes.Ellipse[] emotionLights = new Windows.UI.Xaml.Shapes.Ellipse[8];





        public MainPage()
        {
            this.InitializeComponent();
            initCamera();

 
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10000);
            timer.Tick += takePhoto;
            timer.Start();

            emotionLights[showsAnger] = AngerLight;
            emotionLights[showsContempt] = ContemptLight;
            emotionLights[showsDisgust] = DisgustLight;
            emotionLights[showsFear] = FearLight;
            emotionLights[showsHappyiness] = HappinessLight;
            emotionLights[showsNeutrality] = NeutralLight;
            emotionLights[showsSadness] = SadnessLight;
            emotionLights[showsSurprise] = SurpriseLight;


        }

        /// <summary>
        /// For an extension ...
        /// </summary>
        /// <param name="desiredPanel"></param>
        /// <returns></returns>
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            // Get available devices for capturing pictures
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Get the desired camera by panel
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        /// <summary>
        /// Initializes the camera and displays preview
        /// </summary>
        private async void initCamera()
        {
            // Disable all buttons until initialization completes

            try
            {


                status.Text = "Initializing camera to capture audio and video...";
                // Use default initialization
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                // Set callbacks for failure and recording limit exceeded
                status.Text = "Device successfully initialized for video recording!";
                mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);

                // Start Preview                
                previewElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                status.Text = "Camera preview succeeded";

            }
            catch (Exception ex)
            {
                status.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;


            }
        }

        /// <summary>
        /// Callback in case there is a media capture error
        /// </summary>
        /// <param name="currentCaptureObject"></param>
        /// <param name="currentFailure"></param>
        private  void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
             status.Text = "MediaCaptureFailed: " + currentFailure.Message;
        }




        /// <summary>
        /// Interface with project oxford
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        private async Task<Emotion[]> UploadAndDetectEmotions(string imageFilePath)
        {
            string subscriptionKey = "xxx";
            EmotionServiceClient emotionServiceClient = new EmotionServiceClient(subscriptionKey);

            Stream imageFileStream = await Task.Run(() => openFile(imageFilePath));
            Emotion[] emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream);
            return emotionResult;
        }


        /// <summary>
        /// A little method to allows async stream opening
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        private FileStream openFile(string imageFilePath)
        {

            FileStream imageFileStream = File.OpenRead(imageFilePath);
            return imageFileStream;
        }


       /// <summary>
       /// callbak to take a photo, store it and pass it to oxford for emotional analysis
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
       private async void takePhoto(object sender, object e)
        {
            try
            {
                 captureImage.Source = null;

                photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                    PHOTO_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
                status.Text = "Take Photo succeeded: " + photoFile.Path;

                IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(photoStream);
                captureImage.Source = bitmap;

                Emotion[] emotionResult = await UploadAndDetectEmotions(photoFile.Path);

                for (int i = 0; i < 8; i++)
                {
                     emotionLights[i].Fill = whiteBrush;
                }

                int emotionResultCount = 0;
 
                if (emotionResult != null && emotionResult.Length > 0)
                {
                    foreach (Emotion emotion in emotionResult)
                    {


                        float maxEmotion = 0.0F;
                        int maxEmotionType = -1;

                        AngerSlide.Value = 100 * emotion.Scores.Anger;
                        if (emotion.Scores.Anger > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Anger;
                            maxEmotionType = showsAnger;
                        }

                        ContemptSlide.Value = 100 * emotion.Scores.Contempt;
                        if (emotion.Scores.Contempt > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Contempt;
                            maxEmotionType = showsContempt;
                        }

                        DisgustSlide.Value = 100 * emotion.Scores.Disgust;
                        if (emotion.Scores.Disgust > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Disgust;
                            maxEmotionType = showsDisgust;
                        }

                        FearSlide.Value = 100 * emotion.Scores.Fear;
                        if (emotion.Scores.Fear > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Fear;
                            maxEmotionType = showsFear;
                        }

                        HappinessSlide.Value = 100 * emotion.Scores.Happiness;
                        if (emotion.Scores.Happiness > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Happiness;
                            maxEmotionType = showsHappyiness;
                        }

                        NeutralSlide.Value = 100 * emotion.Scores.Neutral;
                        if (emotion.Scores.Neutral > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Neutral;
                            maxEmotionType = showsNeutrality;
                        }

                        SadnessSlide.Value = 100 * emotion.Scores.Sadness;
                        if (emotion.Scores.Sadness > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Sadness;
                            maxEmotionType = showsSadness;
                        }

                        SurpriseSlide.Value = 100 * emotion.Scores.Surprise;
                        if (emotion.Scores.Surprise > maxEmotion)
                        {
                            maxEmotion = emotion.Scores.Surprise;
                            maxEmotionType = showsSurprise;
                        }

                        emotionResultCount++;

                        status.Text = "max = " + maxEmotion + " at " + maxEmotionType;
                        emotionLights[maxEmotionType].Fill = blueBrush;

                    }

                }
                else
                {
                    AngerSlide.Value = 0;
                    ContemptSlide.Value = 0;
                    DisgustSlide.Value = 0;
                    FearSlide.Value = 0;
                    HappinessSlide.Value = 0;
                    NeutralSlide.Value =0;
                    SadnessSlide.Value = 0;
                    SurpriseSlide.Value = 0;


                    status.Text = "no emotions detected";
                }


            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
            finally
            {
            }
        }



    }
}
    

