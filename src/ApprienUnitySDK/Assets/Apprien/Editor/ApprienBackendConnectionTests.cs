#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Apprien;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

using NSubstitute;
using UnityEngine.Networking;

namespace ApprienUnitySDK.ExampleProject.Tests
{
    [TestFixture]
    public class ApprienBackendConnectionTests
    {
        private IApprienBackendConnection _backend;
        private ITimeProvider _timeProviderMock;
        private IDownloadHandler _downloadHandler;

        private string _gamePackageName;
        private string _token;
        private string _apprienIdentifier;

        [SetUp]
        public void SetUp()
        {
            _gamePackageName = "dummy.package.name";
            _token = "dummy-token";
            _apprienIdentifier = "FF"; // One byte as a hex string

            _timeProviderMock = Substitute.For<ITimeProvider>();
            _downloadHandler = Substitute.For<IDownloadHandler>();
            _backend = new ApprienBackendConnection(_gamePackageName, ApprienIntegrationType.GooglePlayStore, _token, _apprienIdentifier, _timeProviderMock);
        }

        [TearDown]
        public void TearDown()
        {

        }

        [UnityTest]
        public IEnumerator FetchingPricesSuccessfullyShouldReturnCorrectJSON()
        {
            var responseBody = "<some working JSON>";
            _downloadHandler.text.Returns(responseBody);

            var request = Substitute.For<IUnityWebRequest>();
            request.isDone.Returns(true);
#if UNITY_2020_1_OR_NEWER
            request.result.Returns(UnityWebRequest.Result.Success);
#else
            request.isHttpError.Returns(false);
            request.isNetworkError.Returns(false);
#endif
            request.downloadHandler.Returns(_downloadHandler);

            var fetch = _backend.FetchApprienPrices(request);

            while (fetch.MoveNext())
            {
                yield return null;
            }

            var response = fetch.Current;

            Assert.AreEqual(true, response.Success);
            Assert.AreEqual(responseBody, response.JSON);
        }

        [UnityTest]
        public IEnumerator FetchingPricesNetworkErrorShouldBeCaught()
        {
            // We are expecting a network error
            LogAssert.Expect(LogType.Error, new Regex(".*NETWORK error.*"));

            var responseBody = "";
            _downloadHandler.text.Returns(responseBody);

            var request = Substitute.For<IUnityWebRequest>();
            request.isDone.Returns(true);
            request.method.Returns("MOCK");
            request.url.Returns("http://mock.url");
#if UNITY_2020_1_OR_NEWER
            request.result.Returns(UnityWebRequest.Result.ConnectionError);
#else
            request.isHttpError.Returns(false);
            request.isNetworkError.Returns(true);
#endif
            request.downloadHandler.Returns(_downloadHandler);

            var fetch = _backend.FetchApprienPrices(request);

            while (fetch.MoveNext())
            {
                yield return null;
            }

            var response = fetch.Current;

            Assert.AreEqual(false, response.Success);
        }

        [UnityTest]
        public IEnumerator FetchingPricesHTTPErrorShouldBeCaught()
        {
            // We are expecting a network error
            LogAssert.Expect(LogType.Error, new Regex(".*HTTP error.*"));

            var responseBody = "server HTTP error body";
            _downloadHandler.text.Returns(responseBody);

            var request = Substitute.For<IUnityWebRequest>();
            request.isDone.Returns(true);
            request.method.Returns("MOCK");
            request.url.Returns("http://mock.url");
            request.responseCode.Returns(500);
#if UNITY_2020_1_OR_NEWER
            request.result.Returns(UnityWebRequest.Result.ProtocolError);
#else
            request.isHttpError.Returns(true);
            request.isNetworkError.Returns(false);
#endif
            request.downloadHandler.Returns(_downloadHandler);

            var fetch = _backend.FetchApprienPrices(request);

            while (fetch.MoveNext())
            {
                yield return null;
            }

            var response = fetch.Current;

            Assert.AreEqual(false, response.Success);
        }

        [UnityTest]
        public IEnumerator FetchingPricesDataProcessingErrorShouldBeCaught()
        {
            // We are expecting a network error
            LogAssert.Expect(LogType.Error, new Regex(".*HTTP error.*"));

            var responseBody = "server HTTP error body";
            _downloadHandler.text.Returns(responseBody);

            var request = Substitute.For<IUnityWebRequest>();
            request.isDone.Returns(true);
            request.method.Returns("MOCK");
            request.url.Returns("http://mock.url");
            request.responseCode.Returns(200);
#if UNITY_2020_1_OR_NEWER
            request.result.Returns(UnityWebRequest.Result.DataProcessingError);
#else
            request.isHttpError.Returns(true);
            request.isNetworkError.Returns(true);
#endif
            request.downloadHandler.Returns(_downloadHandler);

            var fetch = _backend.FetchApprienPrices(request);

            while (fetch.MoveNext())
            {
                yield return null;
            }

            var response = fetch.Current;

            Assert.AreEqual(false, response.Success);
        }

        [UnityTest]
        public IEnumerator FetchingPricesTimeoutShouldBeCaught()
        {
            var now = new DateTime(2022, 7, 4);
            // Make the first GetTimeNow return now, and the second call to return a timeouted value
            _timeProviderMock.GetTimeNow().Returns(now, now.AddSeconds(_backend.RequestTimeout + 2f));

            var request = Substitute.For<IUnityWebRequest>();
            request.isDone.Returns(false);
#if UNITY_2020_1_OR_NEWER
            request.result.Returns(UnityWebRequest.Result.InProgress);
#else
            request.isHttpError.Returns(false);
            request.isNetworkError.Returns(false);
#endif

            var fetch = _backend.FetchApprienPrices(request);

            while (fetch.MoveNext())
            {
                yield return null;
            }

            var response = fetch.Current;

            Assert.AreEqual(false, response.Success);
        }

        [UnityTest]
        public IEnumerator FetchingPricesRequestInProgressShouldWait()
        {
            var now = new DateTime(2022, 7, 4);
            // Make the first GetTimeNow return now, and the second call to return a value halfway
            _timeProviderMock.GetTimeNow().Returns(now, now.AddSeconds(_backend.RequestTimeout / 2f));

            var request = Substitute.For<IUnityWebRequest>();
            request.isDone.Returns(false);
#if UNITY_2020_1_OR_NEWER
            request.result.Returns(UnityWebRequest.Result.InProgress);
#else
            request.isHttpError.Returns(false);
            request.isNetworkError.Returns(false);
#endif

            var fetch = _backend.FetchApprienPrices(request);

            // Assert that the request is still in progress
            Assert.AreEqual(true, fetch.MoveNext());

            request.isDone.Returns(true);

            yield break;
        }
    }
}
#endif
