using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchCameraControl : MonoBehaviour
{
    // Start is called before the first frame update
    
    

    private Camera cameratarget;
    private Vector2 PrevPoint;



    public float Speed = 10.0f;

    public float RotateSpeed = 1.0f;

    //public float perspectiveZoomSpeed = .5f;

    //public float pinchRatio = 0.5f;
    //public float pinchThreshold = 1f;
    private Transform tr;

    void Start ()
    {
        //카메라 자신의 transform 컴포넌트를 tr에 할당
        tr = GetComponent<Transform> ();
        cameratarget = Camera.main; 



        
        // keyH = Input.GetAxis("Horizontal") * speed_move * Time.deltaTime;
        // keyV = Input.GetAxis("Vertical") * speed_move * Time.deltaTime;
        // transform.Translate(Vector3.right * keyH);
        // transform.Translate(Vector3.forward * keyV);

        // if (Input.GetMouseButton(0))
        // {
        //     // 마우스 왼쪽 버튼을 누르고 있는 도중의 처리
        //     float mouseX = Input.GetAxis("Mouse X");
        //     float mouseY = Input.GetAxis("Mouse Y");
        //     transform.Rotate(Vector3.up * speed_rota * mouseX);
        //     transform.Rotate(Vector3.left * speed_rota * mouseY);
        // }
    }

    void Update()
    {
        Vector3 PositionInfo = tr.position;
        PositionInfo = Vector3.Normalize (PositionInfo);


        Touch[] touches = Input.touches;

        if (cameratarget)
        {

            if(Input.touchCount == 3)
            {
                // 세 손가락으로 터치할 때, 후진
                cameratarget.transform.Translate(Vector3.back * Time.deltaTime * Speed);
            }
            else if(Input.touchCount == 2)
            {
                cameratarget.transform.Translate(Vector3.forward * Time.deltaTime * Speed);

                // // 손가락 두 개일 때, 카메라 이동
                // Touch touch1 = Input.GetTouch(0);
                // Touch touch2 = Input.GetTouch(1);
                // Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                // Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
                // float prevTouchDeltaMag = (touch1PrevPos - touch2PrevPos).magnitude;
                // float touchDeltaMag = (touch1.position - touch2.position).magnitude;
                // float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
                // Vector3 moveDir = (transform.right * touch1.deltaPosition.x + transform.up * touch1.deltaPosition.y).normalized;
                // transform.position += moveDir * deltaMagnitudeDiff * Speed;

                // // 두 손가락 사이의 거리 계산
                // float pinchDistance = Vector2.Distance(touch0.position, touch1.position);

                // if (pinchDistance > pinchThreshold)
                // {
                //     // 두 손가락 사이의 거리가 pinchThreshold 이상일 때, 전진
                    
                // }
            }
            else if (Input.touchCount == 1)
            {           
                // 손가락 하나일 때, 카메라 회전
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    float x = touch.deltaPosition.x * RotateSpeed;
                    float y = touch.deltaPosition.y * RotateSpeed;
                    transform.Rotate(Vector3.up, x, Space.World);
                    transform.Rotate(Vector3.right, -y, Space.Self);
                }
            }
          









            
            // else if(Input.touchCount == 2)
            // {
            //     // 두 손가락으로 터치할 때
            //     Touch touch0 = Input.GetTouch(0);
            //     Touch touch1 = Input.GetTouch(1);

                

                // // 두 손가락 사이의 거리 계산
                // float pinchDistance = Vector2.Distance(touch0.position, touch1.position);

                // if (pinchDistance > pinchThreshold)
                // {
                //     // 두 손가락 사이의 거리가 pinchThreshold 이상일 때, 전진
                    
                // }
            //}
            // else if(Input.touchCount == 1)
            // {
            //     Touch touch = Input.GetTouch (0);
            //     if(touch.phase == TouchPhase.Began)
            //     {
            //         PrevPoint = touch.position - touch.deltaPosition;
            //     }
            //     else if(touch.phase == TouchPhase.Moved)
            //     {
            //         Vector2 nowPos = touch.position - touch.deltaPosition;
            //         Vector3 movePos = (Vector3)(PrevPoint - nowPos) * Time.deltaTime * Speed;
            //         cameratarget.transform.Translate(movePos); 
            //         PrevPoint = touch.position - touch.deltaPosition;
            //     }
            // }
            
            // if (Input.touchCount == 2) // 두 손가락 터치 시
            // {
            //     // 두 손가락의 현재 위치와 이전 위치 구하기
            //     Touch touch1 = Input.GetTouch(0);
            //     Touch touch2 = Input.GetTouch(1);

            //     Vector2 curPos1 = touch1.position;
            //     Vector2 curPos2 = touch2.position;
            //     Vector2 prevPos1 = curPos1 - touch1.deltaPosition;
            //     Vector2 prevPos2 = curPos2 - touch2.deltaPosition;

            //     // 두 손가락 간 거리 구하기
            //     float curDist = Vector2.Distance(curPos1, curPos2);
            //     float prevDist = Vector2.Distance(prevPos1, prevPos2);

            //     // 거리 차이에 따라 카메라 이동
            //     if (Mathf.Abs(curDist - prevDist) > pinchThreshold)
            //     {
            //         float pinchAmount = (curDist - prevDist) * pinchRatio;
            //         cameratarget.transform.Translate(Vector3.forward * pinchAmount);
            //     }
            // }

            // if (Input.touchCount == 2)
            // {
            //     Touch touchZero = Input.GetTouch(0);
            //     Touch touchOne = Input.GetTouch(1);

            //     Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            //     Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            //     float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            //     float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            //     float deltaMagnitudediff = prevTouchDeltaMag - touchDeltaMag;

            //     tr.position = tr.position + (PositionInfo * deltaMagnitudediff * orthoZoomSpeed);
            // }
        }
    }
    
    



}
