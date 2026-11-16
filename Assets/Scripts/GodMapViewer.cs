using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GodMapViewer : MonoBehaviour
{
    public Transform mapPedestal;
    public Transform hologramPlate;
    public Vector3 godViewPos = new Vector3(0f, 95f, 650f);
    public float godViewOrthoSize = 65f;
    public GameObject godMapUI;
    public Text godMapStatusText;
    public Button godMapToggleButton;

    Camera mainCam;
    ThirdPersonCamera thirdPersonCam;
    bool isGodViewActive;
    Vector3 savedCamPos;
    Quaternion savedCamRot;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null)
        {
            thirdPersonCam = mainCam.GetComponent<ThirdPersonCamera>();
        }

        if (godMapToggleButton != null)
        {
            godMapToggleButton.onClick.AddListener(ToggleGodView);
        }

        if (godMapUI != null)
        {
            godMapUI.SetActive(false);
        }
    }

    void Update()
    {
        // Slowly rotate holographic plate for high-tech aesthetic
        if (hologramPlate != null)
        {
            hologramPlate.Rotate(Vector3.up, 20f * Time.deltaTime, Space.World);
        }

#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null && k.mKey.wasPressedThisFrame)
        {
            ToggleGodView();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleGodView();
        }
#endif
    }

    public void ToggleGodView()
    {
        SetGodView(!isGodViewActive);
    }

    public void SetGodView(bool active)
    {
        isGodViewActive = active;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;
        if (thirdPersonCam == null) thirdPersonCam = mainCam.GetComponent<ThirdPersonCamera>();

        if (isGodViewActive)
        {
            if (thirdPersonCam != null) thirdPersonCam.enabled = false;
            mainCam.transform.position = godViewPos;
            mainCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (godMapUI != null) godMapUI.SetActive(true);
            if (godMapStatusText != null)
            {
                godMapStatusText.text = "GOD'S EYE TACTICAL OVERVIEW // LEVEL 5\n[M] or Tap 'GOD MAP' to Resume Action";
            }
        }
        else
        {
            if (thirdPersonCam != null) thirdPersonCam.enabled = true;
            if (godMapUI != null) godMapUI.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (isGodViewActive) SetGodView(false);
    }
}
