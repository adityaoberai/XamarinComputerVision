using System;
using System.Collections.Generic;
using System.Text;

namespace XamarinComputerVision.Helpers
{
    public class ComputerVisionAPICredentials
    {
        public string FaceAPIKey { get; set; } = "<Azure Face API Key>";
        public string CVAPIKey { get; set; } = "<Azure Computer Vision API Key>";
        public string Endpoint { get; set; } = "https://centralindia.api.cognitive.microsoft.com/"; //change the region in the endpoint based on the region of you Azure resource
    }
}
