using TMPro;
using UnityEngine;

public class CodeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI codeTextUI;

    void Update()
    {
        if(HostSingleton.Instance != null)
            codeTextUI.text = HostSingleton.Instance.GameManager.JoinCode;
    }
}
