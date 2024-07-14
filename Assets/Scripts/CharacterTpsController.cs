using UnityEngine;

public class CharacterTpsController : MonoBehaviour
{   
    [Header("REFERENCES")]
    [SerializeField] private Transform tpsCameraRef;

    [Header("MOVEMENT")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpPower = 6f;
    [SerializeField] private float maxSlopeAngle = 55f;
    
    [Header("CAPSULE COLLIDER")]
    [SerializeField] private float capsuleRadius = 0.25f;
    [SerializeField] private float capsuleHeight = 1.85f;
    [SerializeField] private LayerMask capsuleCollisionLayer;
    [Header("PHYSICS")]
    [SerializeField] private float airFriction = 0.1f;
    [SerializeField] private float groundFriction = 0.5f;
    [SerializeField] private float gravityScale = 2f;


    private float capsuleSkinWidth = 0.015f;
    private float rotationSmoothVelocity;
    private bool isGrounded, isJumping;
    private Vector3 hVelocity,vVelocity,inputDir;
    private const float GRAVITY_FORCE = 9.8f;

    private void Update(){
        //detect jump input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJumping){
            Debug.Log("jump");
            vVelocity = Vector3.up*Mathf.Sqrt(jumpPower*2*GRAVITY_FORCE*gravityScale);
            isJumping = true;
        }

        //detect move input
        inputDir = GetMoveDirection();
    }

    private void FixedUpdate()
    {   
        if(inputDir.magnitude > 0.1f){
            hVelocity += inputDir*moveSpeed;
            Rotate(inputDir);
        }

        //horizontal air friction
        hVelocity = ApplyFriction(hVelocity, groundFriction);

        //gravity (Time.fixedDeltaTime is applied twice because acceleration is inverse time squared)
        vVelocity += Vector3.down*GRAVITY_FORCE*gravityScale*Time.fixedDeltaTime;

        //Collider check, Move character based on horizontal and vertical velocities, refresh isGrounded status
        FixedApplyForces(hVelocity,vVelocity);

        //FixedApplyForces also calculate if the character is grounder in the next frame we correct vVelocity last 
        vVelocity = isGrounded ? Vector3.zero : ApplyFriction(vVelocity,airFriction);
    }

    //get ZQSD keyboard axis values -1,0,1 values on Horizontal/vertical
    private Vector3 GetMoveDirection(){
        Vector3 camForward = new Vector3(tpsCameraRef.forward.x,0,tpsCameraRef.forward.z).normalized;
        Vector3 camRight = new Vector3(tpsCameraRef.right.x,0,tpsCameraRef.right.z).normalized;
        Vector3 inputDir = Input.GetAxisRaw("Horizontal") * camRight + Input.GetAxisRaw("Vertical") * camForward;
        return inputDir.normalized;
    }

    //Apply a specific friction factor to a vector velocity by multiplying them together.
    private Vector3 ApplyFriction(Vector3 velocity, float friction){
        velocity -= velocity*friction;
        if(velocity.magnitude < 0.001f){
            velocity = Vector3.zero;
        }
        return velocity;
    }
   
    //move the character in the input direction and apply gravity when needed
    private void FixedApplyForces(Vector3 moveAmount, Vector3 gravityAmount){
        Vector3 finalAmount = Vector3.zero;
       
        if(moveAmount.magnitude > 0){
            moveAmount = moveAmount*Time.fixedDeltaTime;
            moveAmount = CollideAndSlide(moveAmount,moveAmount, transform.position,false,0);
            finalAmount += moveAmount;
        }
        
        gravityAmount = gravityAmount*Time.fixedDeltaTime;
        gravityAmount = CollideAndSlide(gravityAmount,gravityAmount, transform.position+moveAmount, true,0);
        finalAmount += gravityAmount;

        transform.position += finalAmount;
    }

    //find the rotation toward's input direction based on where the camera is facing
    private void Rotate(Vector3 direction){
        float smoothAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y, 
            Mathf.Atan2(direction.x, direction.z)*Mathf.Rad2Deg, 
            ref rotationSmoothVelocity, 
            0.1f
        );
        transform.rotation = Quaternion.Euler(0f,smoothAngle,0f);
    }

    //handle bumping into object and calulate the updated direction after impact
    private Vector3 CollideAndSlide(Vector3 originalVelocity, Vector3 dynamicVelocity, Vector3 position, bool gravityPass, int depth){
        if(depth >= 3){
            return Vector3.zero;
        }

        //check collision of the capsule collider in the movement direction
        RaycastHit hit;
        bool collide = Physics.CapsuleCast(
            position+Vector3.up*(capsuleHeight-capsuleRadius),
            position+Vector3.up*capsuleRadius,
            capsuleRadius,
            dynamicVelocity.normalized,
            out hit,
            dynamicVelocity.magnitude+capsuleSkinWidth,
            capsuleCollisionLayer
        );
        
        //early return if we didn't hit anything
        if(!collide){
            //convert the direction based on the surface we are standing on
            if(gravityPass){
                isGrounded = false;
            }
            return dynamicVelocity;
        }

        //cut the vector in 2: before and past the hit point
        Vector3 distanceToSurface = dynamicVelocity.normalized * (hit.distance - capsuleSkinWidth);
        Vector3 distancePastSurface = dynamicVelocity - distanceToSurface;
        float slopeAngle = Vector3.Angle(Vector3.up,hit.normal);
        
        //prevent overlap when the movement would be within the skin width
        if(distanceToSurface.magnitude <= capsuleSkinWidth){
            distanceToSurface = Vector3.zero;
        }

        //gentle slope
        if (slopeAngle <= maxSlopeAngle){
            if(gravityPass){
                isGrounded = true;
                isJumping = false;
                return distanceToSurface;
            }
            distancePastSurface = ProjectAndScale(distancePastSurface, hit.normal);
        
        //wall or steep slope
        }else{
            if (!gravityPass && isGrounded){
                // find the sliding direction (left/right) when walking in a wall or steep slope
                distancePastSurface = ProjectAndScale(
                    new Vector3(distancePastSurface.x,0,distancePastSurface.z),
                    new Vector3(hit.normal.x,0,hit.normal.z)
                );
            }
            
            if(gravityPass){
                //adjusting falling direction to slide on the steep slope
                distancePastSurface = ProjectAndScale(distancePastSurface, hit.normal);
            }
        }
        return distanceToSurface + CollideAndSlide(originalVelocity,distancePastSurface, position+distanceToSurface,gravityPass,depth+1);
    }

    //project a vector on a plane based on a normal, but keep the same magnitude
    private Vector3 ProjectAndScale(Vector3 vector, Vector3 normal){
        float mag= vector.magnitude;
        return Vector3.ProjectOnPlane(vector, normal).normalized*mag;
    }
}