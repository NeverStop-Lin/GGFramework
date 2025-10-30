using Cinemachine;
using UnityEngine;
using Zenject;

class CameraFollowControler : MonoBehaviour
{
    [Inject] public GamePlayInput GamePlayInput;
    public CinemachineFreeLook cinemachineFreeLookAuto;
    public CinemachineFreeLook cinemachineFreeLookTouch;
    public bool lastTouchState = false;
    private void Update()
    {
        if (cinemachineFreeLookAuto != null && cinemachineFreeLookTouch != null)
        {
            if (GamePlayInput.IsTouchCameraRotateArea)
            {
                cinemachineFreeLookAuto.Priority = 1;
                cinemachineFreeLookTouch.Priority = 10;
                lastTouchState = true;
                cinemachineFreeLookAuto.gameObject.SetActive(false);
                cinemachineFreeLookAuto.transform.position = cinemachineFreeLookTouch.transform.position;
                cinemachineFreeLookTouch.m_YAxis.m_InputAxisValue =
                    (1 - Mathf.Exp(-GamePlayInput.CameraRotateInput.y * Time.deltaTime)) * -30;
                cinemachineFreeLookTouch.m_XAxis.m_InputAxisValue =
                    (1 - Mathf.Exp(-GamePlayInput.CameraRotateInput.x * Time.deltaTime)) * -30;
            }
            else
            {
                cinemachineFreeLookAuto.Priority = 10;
                cinemachineFreeLookTouch.Priority = 1;
                cinemachineFreeLookAuto.gameObject.SetActive(true);

                // if (lastTouchState != GamePlayInput.IsTouchCameraRotateArea)
                // {
                //     lastTouchState = false;
                //     cinemachineFreeLookTouch.m_XAxis.m_InputAxisValue = 0;
                //     cinemachineFreeLookTouch.m_YAxis.m_InputAxisValue = 0;
                // }
                // cinemachineFreeLookAuto.gameObject.SetActive(true);

                // var a = Vector3.ProjectOnPlane(
                //     cinemachineFreeLookAuto.VirtualCameraGameObject.transform.position -
                //     cinemachineFreeLookAuto.Follow.transform.position,
                //     Vector3.up
                // );
                // var targetX = Vector3.SignedAngle(-Vector3.forward, a, Vector3.up);
                // var targetY = cinemachineFreeLookAuto.m_YAxis.Value;
                // cinemachineFreeLookTouch.m_XAxis.m_InputAxisValue = cinemachineFreeLookTouch.m_XAxis.Value -
                //     Mathf.LerpAngle(cinemachineFreeLookTouch.m_XAxis.Value, targetX, Time.deltaTime);
                // cinemachineFreeLookTouch.m_YAxis.m_InputAxisValue =
                // Mathf.LerpAngle(cinemachineFreeLookTouch.m_YAxis.Value, targetY, Time.deltaTime);
            }
        }
    }
}