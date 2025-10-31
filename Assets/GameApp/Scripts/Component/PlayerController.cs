using System.Collections;
using System.Collections.Generic;
using GameApp.Character;
using UnityEngine;
using Zenject;

public class PlayerController : MonoBehaviour
{

    [Inject] private readonly GamePlayInput _gamePlayInput = null;
    [SerializeField] private CharacterControllerKCC _characterController = null;
    [SerializeField] private Camera _mainCamera = null;
    private CharacterInputData _characterInputData = new CharacterInputData();
    // Start is called before the first frame update
    void Start()
    {
        _characterController.SetCamera(_mainCamera);
        _characterInputData.UseCameraRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Update" + _gamePlayInput.PlayerMoveInput);
        _characterInputData.CameraRotation = _mainCamera.transform.rotation;
        _characterInputData.JumpDown = Input.GetKeyDown(KeyCode.Space);
        _characterInputData.MoveInput = new Vector2(_gamePlayInput.PlayerMoveInput.x, _gamePlayInput.PlayerMoveInput.z);
        _characterController.SetInput(ref _characterInputData);
    }
}
