using Cinemachine;
using Framework.Core;
using GameApp.Character;
using GameApp.Demo;
using UnityEngine;
using Zenject;

public class PlayerController : MonoBehaviour
{

    [Inject] private readonly GamePlayInput _gamePlayInput = null;
    [SerializeField] private DemoCharacterController _characterController = null;
    [SerializeField] private CameraFollow _cameraFollow = null;
    private DemoPlayerInputs _characterInputData = new();
    private CinemachineBrain brain = null;
    // Start is called before the first frame update
    void Start()
    {
        brain = FindObjectOfType<CinemachineBrain>();
        _gamePlayInput.CameraRotateInputObserver.OnChange.Add(OnCameraRotateInput, this, false);
        _gamePlayInput.PlayerMoveInputObserver.OnChange.Add(OnPlayerMoveInput, this, false);
    }

    // Update is called once per frame
    void Update()
    {
        _characterInputData.CameraRotation = brain.transform.rotation;
        _characterController.SetInputs(ref _characterInputData);
    }

    void OnCameraRotateInput(Vector2 value)
    {
        if (_gamePlayInput.IsTouchCameraRotateAreaObserver.Value)
        {
            _cameraFollow.QueueYawPitchInput(value.x, value.y);
        }
    }
    void OnPlayerMoveInput(Vector2 value)
    {
        _characterInputData.MoveAxisForward = value.y;
        _characterInputData.MoveAxisRight = value.x;
    }

    private void OnDestroy()
    {
        _gamePlayInput.CameraRotateInputObserver.OnChange.RemoveTarget(this);
        _gamePlayInput.PlayerMoveInputObserver.OnChange.RemoveTarget(this);
    }

}
