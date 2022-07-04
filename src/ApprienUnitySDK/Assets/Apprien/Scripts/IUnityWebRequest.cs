// From https://github.com/goedleIO/unity_http_mocking with MIT license

using UnityEngine.Networking;

namespace Apprien
{
    public interface IUnityWebRequest
    {
        UnityWebRequest.Result result { get; }
        bool isDone { get; }
        bool isHttpError { get; }
        bool isNetworkError { get; }
        string url { get; set; }
        long responseCode { get; }
        string method { get; set; }
        bool chunkedTransfer { get; set; }
        int timeout { get; set; }
        string error { get; }
        IDownloadHandler downloadHandler { get; set; }
        UploadHandler uploadHandler { get; set; }
        UnityWebRequest unityWebRequest { get; set; }
        UnityWebRequestAsyncOperation SendWebRequest();
        void SetRequestHeader(string name, string value);
        string GetRequestHeader(string name);
    }

    public class UnityWebRequestWrapper : IUnityWebRequest
    {
        public UnityWebRequestWrapper(UnityWebRequest webRequest)
        {
            _unityWebRequest = webRequest;
            _downloadHandler = new DownloadHandlerWrapper(webRequest.downloadHandler);
        }

        UnityWebRequest _unityWebRequest { get; set; }
        IDownloadHandler _downloadHandler { get; set; }

        public UnityWebRequest.Result result
        {
            get { return _unityWebRequest.result; }
        }

        public bool isDone
        {
            get { return _unityWebRequest.isDone; }
        }

        public bool isNetworkError
        {
            get { return _unityWebRequest.isNetworkError; }

        }

        public bool isHttpError
        {
            get { return _unityWebRequest.isHttpError; }
        }

        public string url
        {
            get { return _unityWebRequest.url; }
            set { _unityWebRequest.url = value; }
        }

        public string method
        {
            get { return _unityWebRequest.method; }
            set { _unityWebRequest.method = value; }
        }

        public bool chunkedTransfer
        {
            get { return _unityWebRequest.chunkedTransfer; }
            set { _unityWebRequest.chunkedTransfer = value; }
        }

        public int timeout
        {
            get { return _unityWebRequest.timeout; }
            set { _unityWebRequest.timeout = value; }
        }

        public string error
        {
            get { return _unityWebRequest.error; }
        }

        public IDownloadHandler downloadHandler
        {
            get { return _downloadHandler; }
            set { _downloadHandler = value; }
        }

        public UnityWebRequest unityWebRequest
        {
            get { return _unityWebRequest; }
            set { _unityWebRequest = value; }
        }

        public UploadHandler uploadHandler
        {
            get { return _unityWebRequest.uploadHandler; }
            set { _unityWebRequest.uploadHandler = value; }
        }

        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            return _unityWebRequest.SendWebRequest();
        }

        public long responseCode
        {
            get { return _unityWebRequest.responseCode; }
        }

        public void SetRequestHeader(string name, string value)
        {
            _unityWebRequest.SetRequestHeader(name, value);
        }

        public string GetRequestHeader(string name)
        {
            return _unityWebRequest.GetRequestHeader(name);
        }
    }
}
