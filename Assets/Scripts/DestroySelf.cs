using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    private void Start()
    {
        TrackGenerator.DestroyObjs += DestroyMe;
    }
    public void DestroyMe()
    {
        TrackGenerator.DestroyObjs -= DestroyMe;
        Destroy(this.gameObject);
    }
}
