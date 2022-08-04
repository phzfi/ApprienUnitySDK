using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeoDefence
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance;

        [SerializeField] private AnimationCurve _transitionCurve;
        [SerializeField] private Image _transitionPanelImage;

        private void Awake()
        {
            if (Instance == null)
            {
                transform.parent = null;
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private bool _inTransition;
        private bool _inMiddlePoint;
        private float _timer;
        private float _duration;

        public void OrderTransition(float time)
        {
            _inTransition = true;
            _duration = time;
        }

        private void Update()
        {
            if (_inTransition)
            {
                _timer += Time.deltaTime;

                if (_transitionPanelImage.color.a >= 1)
                {
                    _inMiddlePoint = true;
                }

                if (_timer > _duration)
                {
                    _inTransition = false;
                    _timer = 0;
                    _inMiddlePoint = false;
                }

                _transitionPanelImage.color = new Color(_transitionPanelImage.color.r,
                    _transitionPanelImage.color.g,
                    _transitionPanelImage.color.b,
                    _transitionCurve.Evaluate(_timer / _duration));
            }
        }

        public bool GetInTransition()
        {
            return _inTransition;
        }

        public bool GetInMiddlePoint()
        {
            return _inMiddlePoint;
        }
    }
}

/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace gbpickup
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                transform.parent = null;
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [SerializeField] private AnimationCurve _transitionCurve;
        [SerializeField] private Image _transitionPanelImage;
        [SerializeField] private GameObject _getReadyMessage;
        [SerializeField] private GameObject _timeOutMessage;
        [SerializeField] private GameObject _transitionParticles;
        private bool _inTransition;
        private bool _inMiddlePoint;
        private float _timer;
        private float _duration;
        private TransitionMessage _transitionMessage;

        public void OrderTransition(float time, TransitionMessage message, bool particles)
        {
            _inTransition = true;
            _transitionMessage = message;
            _duration = time;
            _transitionParticles.SetActive(particles);
        }

        private void Update()
        {
            if (_inTransition)
            {
                //_timer += _duration/Time.deltaTime;
                _timer += Time.deltaTime;

                if (_timer > _duration / 2)
                {
                    _inMiddlePoint = true;
                    if (_transitionMessage == TransitionMessage.GetReady)
                    {
                        _getReadyMessage.SetActive(true);
                        _timeOutMessage.SetActive(false);
                    }
                    if (_transitionMessage == TransitionMessage.TimeOut)
                    {
                        _getReadyMessage.SetActive(false);
                        _timeOutMessage.SetActive(true);
                    }
                }

                if (_timer > _duration)
                {
                    print("Setting Back to false");
                    _inTransition = false;
                    _timer = 0;
                    _inMiddlePoint = false;
                    _transitionMessage = TransitionMessage.None;
                    _getReadyMessage.SetActive(false);
                    _timeOutMessage.SetActive(false);
                    _transitionParticles.SetActive(false);
                }

                _transitionPanelImage.color = new Color(_transitionPanelImage.color.r,
                    _transitionPanelImage.color.g,
                    _transitionPanelImage.color.b,
                    _transitionCurve.Evaluate(_timer / _duration));
            }
        }

        public bool GetInTransition()
        {
            return _inTransition;
        }

        public bool GetInMiddlePoint()
        {
            return _inMiddlePoint;
        }
    }
}
*/

