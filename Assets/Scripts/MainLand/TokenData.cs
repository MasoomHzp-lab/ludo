using UnityEngine;

public class TokenData : MonoBehaviour
{
 public int currentTileIndex = 0;
    public bool isFinished = false;
    public Vector3 homePosition;

    private void Start()
    {
        homePosition = transform.position; // موقعیت اولیه خانه
    }
}
