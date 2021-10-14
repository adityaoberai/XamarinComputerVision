using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plugin.Media;
using Plugin.Media.Abstractions;
using XamarinComputerVision.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinComputerVision
{
    public partial class MainPage : ContentPage
    {
        public MediaFile capturedImage;
        public string filePath;
        public ComputerVisionAPICredentials credentials;
        public MainPage()
        {
            InitializeComponent();

            Capture.Source = null;
            credentials = new ComputerVisionAPICredentials();
        }

        private async void CaptureImage_Clicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", ":( No camera available.", "OK");
                return;
            }

            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                DefaultCamera = CameraDevice.Front,
                PhotoSize = PhotoSize.Medium,
                Directory = "LHDLearn2022",
                Name = "capture.jpg",
                SaveToAlbum = true
            });

            if (file == null)
            {
                return;
            }

            capturedImage = file;
            filePath = file.Path;

            Capture.Source = ImageSource.FromStream(() =>
            {
                var stream = file.GetStream();
                return stream;
            });
            GetEmotion.IsEnabled = true;
            ExtractText.IsEnabled = true;
        }

        private async void GetEmotion_Clicked(object sender, EventArgs e)
        {
            if (Capture.Source == null)
            {
                await DisplayAlert("Capture An Image", "Image Seems To Be Missing", "Ok");
            }
            else
            {
                try
                {
                    await DetectEmotion();
                }
                catch(Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "Ok");
                }
            }
        }

        private async Task DetectEmotion()
        {
            var client = new FaceClient(new ApiKeyServiceClientCredentials(credentials.FaceAPIKey))
            { Endpoint = credentials.Endpoint };

            var responseList = await client.Face.DetectWithStreamAsync(capturedImage.GetStream(), returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.Emotion, FaceAttributeType.Age });
            var face = responseList.FirstOrDefault();
            var predominant = FindPredominantEmotion(face.FaceAttributes.Emotion);
            await DisplayAlert("Predominant Emotion", predominant, "Ok");
        }

        private string FindPredominantEmotion(Emotion emotion)
        {
            double max = 0;
            PropertyInfo prop = null;

            var emotionValues = typeof(Emotion).GetProperties();

            foreach(PropertyInfo property in emotionValues)
            {
                var value = (double)property.GetValue(emotion);

                if(value>max)
                {
                    max = value;
                    prop = property;
                }
            }

            return prop.Name.ToString();
        }

        private async void ExtractText_Clicked(object sender, EventArgs e)
        {
            try
            {
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", credentials.CVAPIKey);

                string url = credentials.Endpoint + "/vision/v3.1/read/analyze";

                HttpResponseMessage response;

                string operationLocation;

                byte[] byteData = new BinaryReader(capturedImage.GetStream()).ReadBytes((int)capturedImage.GetStream().Length);

                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    response = await client.PostAsync(url, content);
                }

                if (response.IsSuccessStatusCode)
                    operationLocation =
                        response.Headers.GetValues("Operation-Location").FirstOrDefault();
                else
                {
                    string errorString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("\n\nResponse:\n{0}\n",
                        JToken.Parse(errorString).ToString());
                    return;
                }

                string contentString;
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    response = await client.GetAsync(operationLocation);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 60 && contentString.IndexOf("\"status\":\"succeeded\"") == -1);

                if (i == 60 && contentString.IndexOf("\"status\":\"succeeded\"") == -1)
                {
                    Console.WriteLine("\nTimeout error.\n");
                    return;
                }

                dynamic readResponse = JsonConvert.DeserializeObject(JToken.Parse(contentString).ToString());

                string extract = "";

                foreach (var result in readResponse?.analyzeResult.readResults)
                {
                    foreach (var line in result.lines)
                    {
                        extract += line.text + "\n";
                    }
                }

                await DisplayAlert("Extracted Text", extract, "Ok");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error Occured", ex.Message, "Ok");
            }
        }
    }
}