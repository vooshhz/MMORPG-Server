using UnityEngine;
using TMPro;

public class AutoFillTMPLogin : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Default Values")]
    [SerializeField] [TextArea] private string defaultEmail;
    [SerializeField] [TextArea] private string defaultPassword;

    void Start()
    {
        if (emailInput != null)
            emailInput.text = defaultEmail;

        if (passwordInput != null)
            passwordInput.text = defaultPassword;
    }
}
