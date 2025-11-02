using UnityEngine;
using RTLTMPro;
using UnityEngine.Events;

public class Spinner : MonoBehaviour
{

    public float reducer=0.05f;
    public float multiplyer = 0;
    bool accelerating = false;
    public bool isStoped = true;
    public bool isSpinned = false;
    public PointerTrigger pointerTrigger;
    public RTLTextMeshPro scoreText;
    public UnityEvent OnStopped;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   isStoped = true;
        multiplyer = 0;
        
        // accelerating = false;
        
        // scoreText.text ="";
    }


    public void Spin()
    {
        
            multiplyer = 1;
            reducer = Random.Range(0.01f, 0.5f);
            accelerating = true;
            isStoped = false;
            isSpinned = false;
        
    }

    void spinWheel()
    {   
        isSpinned = true;
        
          if (multiplyer > 0)
        {
            float rotationAmount = 360f * multiplyer * Time.deltaTime;
            transform.Rotate(Vector3.forward, rotationAmount);
            // transform.Rotate(Vector3.forward, 1 * multiplyer);
        }

        else
        {
          if (!isStoped)
            {
                isStoped = true;
                OnStopped?.Invoke();
            }
        }

        if (multiplyer < 6 && accelerating)
        {

            multiplyer += 3f * Time.deltaTime;
        }
        else
        {
            accelerating = false;

        }
        if (!accelerating && multiplyer > 0)
        {
            multiplyer -= reducer * Time.deltaTime * 60f;
            if (multiplyer < 0f) multiplyer = 0f;
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (!isStoped)
        {
            spinWheel();
        }       
    }
}
