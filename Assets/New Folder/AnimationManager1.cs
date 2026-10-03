using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;

    public void PlayAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.Play("mixamo.com", 0, 0f);
        }
    }
}
