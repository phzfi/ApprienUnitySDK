// From https://github.com/goedleIO/unity_http_mocking with MIT license

using UnityEngine.Networking;

namespace Apprien
{
    public interface IDownloadHandler
    {
        string text { get; }
    }

    public class DownloadHandlerWrapper : IDownloadHandler
    {
        public DownloadHandlerWrapper(DownloadHandler handler)
        {
            _unityDownloadHandler = handler;
        }

        DownloadHandler _unityDownloadHandler { get; set; }

        public string text => _unityDownloadHandler.text;
    }
}
