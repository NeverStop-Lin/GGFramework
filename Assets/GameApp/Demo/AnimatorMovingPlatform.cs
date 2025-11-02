using KinematicCharacterController;
using UnityEngine;

public class AnimatorMovingPlatform : MonoBehaviour, IMoverController
{
    public PhysicsMover Mover;
    public Animator Animator;
    
    private Vector3 _positionBeforeAnim;
    private Quaternion _rotationBeforeAnim;
    
    void Start()
    {
        if (Mover == null)
        {
            throw new System.InvalidOperationException("PhysicsMover is required");
        }
        
        if (Animator == null)
        {
            Animator = GetComponent<Animator>();
        }
        
        if (Animator == null)
        {
            throw new System.InvalidOperationException("Animator component is required");
        }
        
        Mover.MoverController = this;
        
        // 确保 Animator 应用 Root Motion
        // Animator.applyRootMotion = true;
    }
    
    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        // 记录 Animator 应用移动前的位置和旋转
        _positionBeforeAnim = transform.position;
        _rotationBeforeAnim = transform.rotation;
        
        // 让 Animator 更新并应用 Root Motion
        // 这会临时改变 transform，我们需要读取这个变化
        Animator.Update(deltaTime);
        
        // 从 transform 读取 Animator 驱动的目标位置和旋转
        goalPosition = transform.position;
        goalRotation = transform.rotation;
        
        // 重置 transform 到更新前的位置
        // 实际的移动由 PhysicsMover 处理
        transform.position = _positionBeforeAnim;
        transform.rotation = _rotationBeforeAnim;
    }
}