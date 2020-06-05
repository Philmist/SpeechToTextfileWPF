#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeechToTextfileWPF
{
    [XmlRoot(ElementName = "Settings", IsNullable = false)]
    public class SettingStore
    {
        [XmlArrayItem(typeof(GoogleAuthentication))]
        [XmlArrayItem(typeof(AzureSpeechToTextKeyEndpointAuthentication))]
        [XmlArrayItem(typeof(AzureTextAnalyticsKeyEndpointAuthentication))]
        [XmlArrayItem(typeof(AmiVoiceCloudAuthentication))]
        public List<AuthenticationStore> Authentications = new List<AuthenticationStore>();

        public List<SpeechToTextSetting> SpeechToTextSettings = new List<SpeechToTextSetting>();

        public BouyomiChanSetting BouyomiChanSetting = new BouyomiChanSetting();
    }

    public class AuthenticationStore
    {
        public string Name = "";
        public Guid AuthenticationGuid = new Guid();

        public void InitializeGuid()
        {
            AuthenticationGuid = new Guid();
        }
    }

    public class GoogleAuthentication : AuthenticationStore
    {
    }

    public class AzureKeyEndpointAuthentication : AuthenticationStore
    {
        public string EndpointUri = "";
        public string Key = "";
    }

    public class AzureSpeechToTextKeyEndpointAuthentication : AzureKeyEndpointAuthentication
    {
    }

    public class AzureTextAnalyticsKeyEndpointAuthentication : AzureKeyEndpointAuthentication
    {
    }

    public class AmiVoiceCloudAuthentication : AuthenticationStore
    {
        public string Key = "";
        public string EndpointUri = "";
    }

    public class SpeechToTextSetting
    {
        public Guid Guid = new Guid();
        public Guid? AuthenticationGuid = null;
        public string TextFileName = "";
        public int FileRestainSeconds = 0;
    }

    public class TextFilterBase
    {
        public Guid Guid = new Guid();
    }

    public class TextPrefixAddFilter : TextFilterBase
    {
        public string PrefixString = "";
    }

    public class BouyomiChanSetting
    {
        public Guid? TextFilterGuid;
        public bool Enable = false;
        public bool UseNetwork = false;
        public IPAddress? IpAddress;
    }
}
