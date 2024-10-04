using TMPro;
using UnityEngine;

public class UINameplate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nicknameText;
    private Transform _cameraTransform;

    private void Awake()
    {
        _cameraTransform = Camera.main.transform;
        _nicknameText.text = string.Empty;
    }

    private void LateUpdate()
    {
        transform.rotation = _cameraTransform.rotation;
    }

    public void SetNickname(string nickname)
    {
        _nicknameText.text = nickname;
    }
}