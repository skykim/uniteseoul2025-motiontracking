using TMPro;
using UnityEngine;

public class BallDetector : MonoBehaviour
{
    public TMP_Text hitText;
    public AudioClip audioIceBall;
    public AudioClip audioFireBall;
    public AudioSource audioSource;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("iceBallEffect"))
        {
            audioSource.PlayOneShot(audioIceBall);            
            hitText.color = Color.white;
            hitText.text = "Good";            
            Debug.Log("IceBall Touch Detected!");
        }
        else if (collision.gameObject.name.Contains("fireBallEffect"))
        {
            audioSource.PlayOneShot(audioFireBall);
            hitText.color = Color.red;
            hitText.text = "Oops!";
            Debug.Log("FireBall Touch Detected!");
        }
    }
}
