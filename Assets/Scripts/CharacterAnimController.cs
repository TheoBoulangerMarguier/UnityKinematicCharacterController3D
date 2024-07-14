using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterTpsController))]
[RequireComponent(typeof(CharacterInputController))]

public class CharacterAnimController : MonoBehaviour
{
    private Animator animator;
    private CharacterTpsController tpsController;
    private CharacterInputController inputController;
    private float moveValue;

    private void Awake() {
        animator = GetComponent<Animator>();
        tpsController = GetComponent<CharacterTpsController>();
        inputController = GetComponent<CharacterInputController>();
    }

    private void Update(){
        //calculating move velocity
        moveValue = Vector3.Dot(tpsController.hVelocity.normalized,transform.forward);
        moveValue = inputController.isRunningPressed ?  Mathf.Round(moveValue)*2 : moveValue;

        //send info to animator
        animator.SetFloat("forwardValue", moveValue);
        animator.SetBool("falling", tpsController.isFalling);
        animator.SetBool("jumping", tpsController.isJumping);
        animator.SetBool("grounded", tpsController.isGrounded);
    }
    
}
