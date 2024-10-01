using Fusion.Addons.SimpleKCC;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip[] _footstepAudioClips;
    [SerializeField] private AudioClip _landingAudioClip;
    [Range(0f, 1f)]
    [SerializeField] private float _footstepAudioVolume = 0.5f;

    private SimpleKCC _kcc;

    public void Init(SimpleKCC kcc)
    {
        _kcc = kcc;
    }

    // Animation event
    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight < 0.5f)
            return;

        if (_footstepAudioClips.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, _footstepAudioClips.Length);
            AudioSource.PlayClipAtPoint(_footstepAudioClips[index], _kcc.Position, _footstepAudioVolume);
        }

    }

    // Animation event
    private void OnLand(AnimationEvent animationEvent)
    {
        AudioSource.PlayClipAtPoint(_landingAudioClip, _kcc.Position, _footstepAudioVolume);
    }
}