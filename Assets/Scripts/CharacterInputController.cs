using UnityEngine;

public class CharacterInputController : MonoBehaviour
{
    [SerializeField] private Transform tpsCameraRef;

    public bool isJumpingPressed {get; private set;}
    public bool isRunningPressed {get; private set;}
    public Vector3 moveInput {get; private set;}
    public Vector3 cameraRelativeMoveInput  {get; private set;}
    
    void Update()
    {
        isJumpingPressed = Input.GetKeyDown(KeyCode.Space);
        isRunningPressed = Input.GetKey(KeyCode.LeftShift);
        moveInput = new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));
        cameraRelativeMoveInput = ConvertInputToCameraReferential(moveInput);
    }

    private Vector3 ConvertInputToCameraReferential(Vector3 input){
        Vector3 camForward = new Vector3(tpsCameraRef.forward.x,0,tpsCameraRef.forward.z).normalized;
        Vector3 camRight = new Vector3(tpsCameraRef.right.x,0,tpsCameraRef.right.z).normalized;
        return (input.x*camRight+input.z*camForward).normalized;
    }
}
