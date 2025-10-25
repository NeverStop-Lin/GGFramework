using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Zenject;

namespace Framework.Core
{
    public class UICenter : IUI
    {
        [Inject]
        private DiContainer _container;

        private readonly ConcurrentDictionary<Type, UiState> _uiStates = new();


        public UiLifeCycle<T> Show<T>(params object[] args) where T : IBaseUI, new()
        {
            var uiKey = typeof(T);
            //      FrameworkLogger.Info($"��ʼ��ʾUI: {uiKey.Name}");

            var uiState = _uiStates.GetOrAdd(uiKey, _ => new UiState());

            // ��ʼ������TaskCompletionSource
            uiState.CreateTcs = new TaskCompletionSource<object>();
            uiState.ShowTcs = new TaskCompletionSource<object>();
            uiState.ReadyTcs = new TaskCompletionSource<object>();
            uiState.HideTcs = new TaskCompletionSource<object>();

            if (uiState.Ui == null)
            {
                //    FrameworkLogger.Info($"������UIʵ��: {uiKey.Name}");
                var ui = _container.Resolve<PlaceholderFactory<Type, IBaseUI>>().Create(typeof(T));
                ui.Initialize();
                uiState.Ui = ui;
                _ = CreateAndShowAsync(ui, args, uiState);
            }
            else
            {
                //     FrameworkLogger.Info($"��������UIʵ��: {uiKey.Name}");
                _ = ShowAsync(uiState.Ui, args, uiState);
            }

            return new UiLifeCycle<T>
            {
                //  Action = uiState.Ui?.Action,
                ShowTask = uiState.ShowTcs.Task,
                HideTask = uiState.HideTcs.Task,
                Target = uiState.Ui is T ? (T)uiState.Ui : default
            };
        }

        private async Task CreateAndShowAsync(IBaseUI ui, object[] args, UiState uiState)
        {
            try
            {
                //   FrameworkLogger.Info($"��ʼ��������: {ui.GetType().Name}");
                await CreateAsync(ui, args, uiState);
                await ShowAsync(ui, args, uiState);
                await ReadyAsync(ui, args, uiState);
                //      FrameworkLogger.Info($"UI׼������: {ui.GetType().Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"UI�����쳣: {ui.GetType().Name}, {ex.Message}");
                RemoveUi(ui.GetType(), ex);
            }
        }

        private async Task CreateAsync(IBaseUI ui, object[] args, UiState uiState)
        {
            try
            {
                //        FrameworkLogger.Info($"ִ�д�������: {ui.GetType().Name}");
                var result = await ui.DoCreate(args);
                uiState.CreateTcs?.SetResult(result);
                //          FrameworkLogger.Info($"�����ɹ�: {ui.GetType().Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"����ʧ��: {ui.GetType().Name}, {ex.Message}");
                uiState.CreateTcs?.SetException(ex);
                throw;
            }
        }

        private async Task ShowAsync(IBaseUI ui, object[] args, UiState uiState)
        {
            try
            {
                //           FrameworkLogger.Info($"ִ����ʾ����: {ui.GetType().Name}");
                var result = await ui.DoShow(args);
                uiState.ShowTcs?.SetResult(result);
                //          FrameworkLogger.Info($"��ʾ�ɹ�: {ui.GetType().Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"��ʾʧ��: {ui.GetType().Name}, {ex.Message}");
                uiState.ShowTcs?.SetException(ex);
                throw;
            }
        }


        private async Task ReadyAsync(IBaseUI ui, object[] args, UiState uiState)
        {
            try
            {
                //       FrameworkLogger.Info($"ִ��׼������: {ui.GetType().Name}");
                var result = await ui.DoReady(args);
                uiState.ReadyTcs?.SetResult(result);
                //      FrameworkLogger.Info($"׼�����: {ui.GetType().Name}");
            }
            catch (Exception ex)
            {
                FrameworkLogger.Error($"׼��ʧ��: {ui.GetType().Name}, {ex.Message}");
                uiState.ReadyTcs?.SetException(ex);
                throw;
            }
        }

        public Task<object> Hide<T>(params object[] args) { return Hide(typeof(T), args); }

        public Task<object> Hide<T>(T target, params object[] args) where T : Type
        {
            Type uiKey = (Type)target;

            //      FrameworkLogger.Info($"��ʼ����UI: {uiKey.Name}");

            if (!_uiStates.TryGetValue(uiKey, out var uiState))
                return Task.FromResult<object>(null);

            if (uiState.Ui != null) return HideAsync(uiState.Ui, args, uiState);

            //     FrameworkLogger.Warn($"�������ز����ڵ�UI: {uiKey.Name}");
            uiState.HideTcs?.SetResult(null);
            return Task.FromResult<object>(null);
        }

        private async Task<object> HideAsync(IBaseUI ui, object[] args, UiState uiState)
        {
            try
            {
                // FrameworkLogger.Info($"ִ�����ز���: {ui.GetType().Name}");
                var result = await ui.DoHide(args);
                uiState.HideTcs?.SetResult(result);
                FrameworkLogger.Info($"���سɹ�: {ui.GetType().Name}");
            }
            catch (Exception ex)
            {
                // FrameworkLogger.Error($"����ʧ��: {ui.GetType().Name}, {ex.Message}");
                uiState.HideTcs?.SetException(ex);
            }
            finally
            {
                //   FrameworkLogger.Info($"����UI��Դ: {ui.GetType().Name}");
                //   RemoveUi(ui.GetType());
            }

            return uiState.HideTcs != null ? uiState.HideTcs.Task.Result : Task.CompletedTask;
        }

        private void RemoveUi(Type uiKey, Exception exception = null)
        {
            if (!_uiStates.TryRemove(uiKey, out var uiState)) return;

            if (uiState.Ui is IDisposable disposable)
            {
                FrameworkLogger.Info($"�ͷ�UI��Դ: {uiKey.Name}");
                disposable.Dispose();
            }

            // ͳһ�������״̬
            var tcsList = new[]
            {
                uiState.CreateTcs, uiState.ShowTcs, uiState.ReadyTcs, uiState.HideTcs
            };
            foreach (var tcs in tcsList)
            {
                if (tcs == null) continue;

                if (exception != null)
                {
                    FrameworkLogger.Error($"��������쳣: {uiKey.Name}, {exception.Message}");
                    tcs.TrySetException(exception);
                }
                else
                {
                    tcs.TrySetCanceled();
                }
            }
        }

        private class UiState
        {
            public IBaseUI Ui { get; set; }
            public TaskCompletionSource<object> CreateTcs { get; set; }
            public TaskCompletionSource<object> ShowTcs { get; set; }
            public TaskCompletionSource<object> ReadyTcs { get; set; }
            public TaskCompletionSource<object> HideTcs { get; set; }
        }
    }
}