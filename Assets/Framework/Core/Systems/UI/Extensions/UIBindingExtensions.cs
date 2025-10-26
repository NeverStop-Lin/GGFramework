using UnityEngine;
using UnityEngine.UI;

namespace Framework.Core
{
    /// <summary>
    /// UI数据绑定扩展方法
    /// 用于将UI组件与IObservers集成，实现数据驱动UI
    /// </summary>
    public static class UIBindingExtensions
    {
        #region Text绑定
        
        /// <summary>
        /// 绑定Text到字符串Observer
        /// UI会自动响应数据变化
        /// </summary>
        /// <param name="text">Text组件</param>
        /// <param name="observer">字符串Observer</param>
        /// <example>
        /// <code>
        /// var nameObserver = GridFramework.Observer.Value("玩家");
        /// _nameText.BindText(nameObserver);
        /// 
        /// nameObserver.Value = "新名字";  // UI自动更新
        /// </code>
        /// </example>
        public static void BindText(this Text text, IValueObserver<string> observer)
        {
            if (text == null || observer == null) return;
            
            // 立即设置当前值
            text.text = observer.Value;
            
            // 监听变化
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (text != null)
                {
                    text.text = newVal;
                }
            }, null, false);
        }
        
        /// <summary>
        /// 绑定Text到数字Observer
        /// </summary>
        /// <param name="text">Text组件</param>
        /// <param name="observer">整数Observer</param>
        /// <param name="format">格式化字符串，{0}表示数值</param>
        /// <example>
        /// <code>
        /// var goldObserver = GridFramework.Observer.Value(100);
        /// _goldText.BindNumber(goldObserver, "金币: {0}");
        /// 
        /// goldObserver.Value += 50;  // 显示 "金币: 150"
        /// </code>
        /// </example>
        public static void BindNumber(this Text text, IValueObserver<int> observer, string format = "{0}")
        {
            if (text == null || observer == null) return;
            
            // 立即设置当前值
            text.text = string.Format(format, observer.Value);
            
            // 监听变化
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (text != null)
                {
                    text.text = string.Format(format, newVal);
                }
            }, null, false);
        }
        
        /// <summary>
        /// 绑定Text到浮点数Observer
        /// </summary>
        public static void BindNumber(this Text text, IValueObserver<float> observer, string format = "{0:F2}")
        {
            if (text == null || observer == null) return;
            
            text.text = string.Format(format, observer.Value);
            
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (text != null)
                {
                    text.text = string.Format(format, newVal);
                }
            }, null, false);
        }
        
        #endregion
        
        #region GameObject/组件激活状态绑定
        
        /// <summary>
        /// 绑定GameObject的激活状态到bool Observer
        /// </summary>
        /// <example>
        /// <code>
        /// var visibleObserver = GridFramework.Observer.Value(true);
        /// _panel.BindActive(visibleObserver);
        /// 
        /// visibleObserver.Value = false;  // Panel自动隐藏
        /// </code>
        /// </example>
        public static void BindActive(this GameObject obj, IValueObserver<bool> observer)
        {
            if (obj == null || observer == null) return;
            
            obj.SetActive(observer.Value);
            
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (obj != null)
                {
                    obj.SetActive(newVal);
                }
            }, null, false);
        }
        
        /// <summary>
        /// 绑定组件的enabled状态到bool Observer
        /// </summary>
        public static void BindEnabled(this Behaviour component, IValueObserver<bool> observer)
        {
            if (component == null || observer == null) return;
            
            component.enabled = observer.Value;
            
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (component != null)
                {
                    component.enabled = newVal;
                }
            }, null, false);
        }
        
        #endregion
        
        #region Image绑定
        
        /// <summary>
        /// 绑定Image的fillAmount到浮点数Observer
        /// </summary>
        /// <example>
        /// <code>
        /// var progressObserver = GridFramework.Observer.Value(0f);
        /// _progressBar.BindFillAmount(progressObserver);
        /// 
        /// progressObserver.Value = 0.5f;  // 进度条显示50%
        /// </code>
        /// </example>
        public static void BindFillAmount(this Image image, IValueObserver<float> observer)
        {
            if (image == null || observer == null) return;
            
            image.fillAmount = observer.Value;
            
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (image != null)
                {
                    image.fillAmount = newVal;
                }
            }, null, false);
        }
        
        /// <summary>
        /// 绑定Image的color到颜色Observer
        /// </summary>
        public static void BindColor(this Image image, IValueObserver<Color> observer)
        {
            if (image == null || observer == null) return;
            
            image.color = observer.Value;
            
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (image != null)
                {
                    image.color = newVal;
                }
            }, null, false);
        }
        
        #endregion
        
        #region Slider绑定
        
        /// <summary>
        /// 绑定Slider的value到浮点数Observer（双向绑定）
        /// </summary>
        /// <example>
        /// <code>
        /// var volumeObserver = GridFramework.Observer.Cache("volume", 0.5f);
        /// _volumeSlider.BindValue(volumeObserver);
        /// 
        /// // UI改变 -> 数据更新
        /// // 数据改变 -> UI更新
        /// </code>
        /// </example>
        public static void BindValue(this Slider slider, IValueObserver<float> observer, bool twoWay = true)
        {
            if (slider == null || observer == null) return;
            
            // 初始值
            slider.value = observer.Value;
            
            // 数据 -> UI
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (slider != null && !Mathf.Approximately(slider.value, newVal))
                {
                    slider.value = newVal;
                }
            }, null, false);
            
            // UI -> 数据（双向绑定）
            if (twoWay)
            {
                slider.onValueChanged.AddListener(value =>
                {
                    if (!Mathf.Approximately(observer.Value, value))
                    {
                        observer.Value = value;
                    }
                });
            }
        }
        
        #endregion
        
        #region Toggle绑定
        
        /// <summary>
        /// 绑定Toggle的isOn到bool Observer（双向绑定）
        /// </summary>
        public static void BindToggle(this Toggle toggle, IValueObserver<bool> observer, bool twoWay = true)
        {
            if (toggle == null || observer == null) return;
            
            // 初始值
            toggle.isOn = observer.Value;
            
            // 数据 -> UI
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (toggle != null && toggle.isOn != newVal)
                {
                    toggle.isOn = newVal;
                }
            }, null, false);
            
            // UI -> 数据（双向绑定）
            if (twoWay)
            {
                toggle.onValueChanged.AddListener(value =>
                {
                    if (observer.Value != value)
                    {
                        observer.Value = value;
                    }
                });
            }
        }
        
        #endregion
        
        #region InputField绑定
        
        /// <summary>
        /// 绑定InputField到字符串Observer（双向绑定）
        /// </summary>
        public static void BindInput(this InputField inputField, IValueObserver<string> observer, bool twoWay = true)
        {
            if (inputField == null || observer == null) return;
            
            // 初始值
            inputField.text = observer.Value;
            
            // 数据 -> UI
            observer.OnChange.Add((newVal, oldVal) =>
            {
                if (inputField != null && inputField.text != newVal)
                {
                    inputField.text = newVal;
                }
            }, null, false);
            
            // UI -> 数据（双向绑定）
            if (twoWay)
            {
                inputField.onEndEdit.AddListener(value =>
                {
                    if (observer.Value != value)
                    {
                        observer.Value = value;
                    }
                });
            }
        }
        
        #endregion
    }
}
