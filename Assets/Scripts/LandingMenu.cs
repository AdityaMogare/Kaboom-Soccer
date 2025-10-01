using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LandingMenu : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private Button rulesTabButton;
    [SerializeField] private Button controlsTabButton;

    [Header("Content Roots")]
    [SerializeField] private GameObject rulesContentRoot;     
    [SerializeField] private GameObject controlsContentRoot;  

    [Header("Bottom Bar")]
    [SerializeField] private Button startButton;  
    [SerializeField] private Button quitButton;   

    [Header("Overlay Root")]
    [SerializeField] private GameObject landingUIRoot; 

    void Awake()
    {
        
        if (landingUIRoot == null)
        {
            Transform t = transform;
            while (t.parent != null && t.name != "LandingUI") t = t.parent;
            if (t.name == "LandingUI") landingUIRoot = t.gameObject;
        }

      
        if (rulesTabButton)    rulesTabButton.onClick.AddListener(ShowRules);
        if (controlsTabButton) controlsTabButton.onClick.AddListener(ShowControls);
        if (startButton)       startButton.onClick.AddListener(StartGame);
        if (quitButton)        quitButton.onClick.AddListener(QuitGame);

        ShowRules(); 
    }

    public void ShowRules()
    {
        if (rulesContentRoot) rulesContentRoot.SetActive(true);
        if (controlsContentRoot) controlsContentRoot.SetActive(false);

        if (rulesTabButton) rulesTabButton.interactable = false;
        if (controlsTabButton) controlsTabButton.interactable = true;
    }

    public void ShowControls()
    {
        if (rulesContentRoot) rulesContentRoot.SetActive(false);
        if (controlsContentRoot) controlsContentRoot.SetActive(true);

        if (rulesTabButton) rulesTabButton.interactable = true;
        if (controlsTabButton) controlsTabButton.interactable = false;
    }

    public void StartGame()
    {
        Debug.Log("[LandingMenu] Start pressed");

       
        if (landingUIRoot != null)
        {
            landingUIRoot.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[LandingMenu] landingUIRoot not set, hiding Card only.");
            gameObject.SetActive(false);
        }

       
        var gm = GameManager.Instance ?? Object.FindFirstObjectByType<GameManager>();
        if (gm != null) gm.BeginGame();
        else Debug.LogWarning("[LandingMenu] GameManager not found in scene.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
