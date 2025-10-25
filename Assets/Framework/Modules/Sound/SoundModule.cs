
using Framework.Core;
using UnityEngine;
using Zenject;

namespace Framework.Modules.Sound
{
    /// <summary>
    /// 音效管理模块
    /// 提供背景音乐和音效的播放功能
    /// </summary>
    public class SoundModule
    {
        [Inject]
        IObservers _observers;

        private GameObject _soundObject;
        private AudioSource _audioSource;

        IValueObserver<bool> _musicSwitch;
        IValueObserver<bool> _effectSwitch;

        public bool MusicSwitch
        {
            get
            {
                if (_musicSwitch == null)
                {
                    _musicSwitch = _observers.Cache("musicSwitch", true);
                    _musicSwitch.OnChange.Add((v) =>
                    {
                        if (!v)
                        {
                            _audioSource.Stop();
                        }
                        else
                        {
                            _audioSource.Play();
                        }
                    });
                }

                return _musicSwitch.Value;
            }
            set { _musicSwitch.Value = value; }
        }
        public bool EffectSwitch
        {
            get
            {
                if (_effectSwitch == null)
                {
                    _effectSwitch = _observers.Cache("effectSwitch", true);
                }
                return _effectSwitch.Value;
            }
            set
            {
                _effectSwitch.Value = value;
            }
        }

        public SoundModule()
        {


            // 动态创建声音对�?
            _soundObject = new GameObject("SoundManager");
            Object.DontDestroyOnLoad(_soundObject); // 跨场景不销�?

            // 添加音频组件
            _audioSource = _soundObject.AddComponent<AudioSource>();
            ConfigureAudioSource();


        }

        private void ConfigureAudioSource()
        {
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.volume = 0.5f;
        }

        public void PlayMusic(string name)
        {
            var clip = Resources.Load<AudioClip>($"Sound/{name}");
            PlayMusic(clip);
        }

        // 播放背景音乐
        public void PlayMusic(AudioClip clip)
        {
            if (_audioSource.isPlaying)
                _audioSource.Stop();
            _audioSource.clip = clip;

            if (!MusicSwitch) return;

            _audioSource.Play();
        }

        public void PlayEffect(string name)
        {
            var clip = Resources.Load<AudioClip>($"Sound/{name}");
            PlayEffect(clip);
        }
        public void PlayEffect(AudioClip clip)
        {
            if (!EffectSwitch) return;
            _audioSource.PlayOneShot(clip);
        }


        // 销毁时清理资源
        ~SoundModule()
        {
            if (_soundObject != null)
                Object.Destroy(_soundObject);
        }
    }
}