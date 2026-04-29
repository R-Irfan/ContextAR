using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class IPInputUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField ipInputField;
    public Button saveButton;
    public TextMeshProUGUI statusText; // optional
    public ExperienceLayerController experienceLayerController;
    public GameObject AppConfigUICanvas;
    private bool savedIP = false;

    private void Start()
    {
        AppConfigUICanvas.SetActive(true);

        // Load saved IP into input field
        if (AppConfig.HasIP())
        {
            savedIP = true;
            ipInputField.text = AppConfig.GetIP();
        }

        saveButton.onClick.AddListener(OnSaveClicked);
    }

    private void OnSaveClicked()
    {
        string ip = ipInputField.text.Trim();

        if (IPValidator.IsValidIPv4(ip))
        {
          
            AppConfig.SetIP(ip);
            
            experienceLayerController.serverUrl = "http://" + AppConfig.GetIP()  + ":8000";
            Debug.Log("Exp Layer Ctrl Server IP set to :" + experienceLayerController.serverUrl);
            SetStatus("IP Saved", Color.green);
            StartCoroutine(HideAPPConfigUI());
        }
        else
        {
            SetStatus("Invalid IP Address", Color.red);
        }
    }

	public void OnValueChanged(string value)
    {
    if (IPValidator.IsValidIPv4(value))
        ipInputField.image.color = Color.green;
    else
        ipInputField.image.color = Color.red;
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }

        Debug.Log(message);
    }

    IEnumerator HideAPPConfigUI() {
        yield return new WaitForSeconds(1);

        AppConfigUICanvas.SetActive(false);

    }
}