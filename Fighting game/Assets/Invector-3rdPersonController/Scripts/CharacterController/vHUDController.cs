using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class vHUDController : MonoBehaviour 
{

#region General Variables

    #region Health/Stamina Variables
    [Header("Health/Stamina")]
	public Slider healthSlider;
	public Slider staminaSlider;
	[Header("DamageHUD")]
	public Image damageImage;
	public float flashSpeed = 5f;
	public Color flashColour = new Color(1f, 0f, 0f, 0.1f);   
	[HideInInspector] public bool damaged;
    #endregion

    #region Action Text Variables
    [HideInInspector] public bool showInteractiveText = true;
    [Header("Interactable Text-Icon")]
    public Text interactableText;
    public Image interactableIcon;
    public Sprite joystickButton;
    public Sprite keyboardButton;
    [HideInInspector] public bool controllerInput;
    #endregion

    #region Equip Weapon Display    
    [HideInInspector] public bool showEquipText = true;    
    [Header("Equip Weapon Display")]
    public Text equipText;
    public Image equipLeftIcon;
    public Image equipRightIcon;
    public Sprite k_leftSideButton;
    public Sprite k_rightSideButton;
    public Sprite j_leftSideButton;
    public Sprite j_rightSideButton;    
    #endregion

    #region Display Controls Variables
    [Header("Controls Display")]
    public Image displayControls;
    public Sprite joystickControls;
    public Sprite keyboardControls;
    #endregion

    #region Debug Info Variables
    [Header("Debug Window")]
    public GameObject debugPanel;
    #endregion

    #region Change Input Text Variables
    [Header("Text with FadeIn/Out")]
    public Text fadeText;
    private float textDuration, fadeDuration, durationTimer, timer;
    private Color startColor, endColor;
    private bool fade;
    #endregion

    #endregion

    private static vHUDController _instance;
    public static vHUDController instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<vHUDController>();
                //Tell unity not to destroy this object when loading a new scene
                //DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    void Start()
    {
        HideActionText();
        HideEquipText();
        InitFadeText();
    }

    void Update()
    {
        FadeEffect();
        ChangeInputDisplay();
    }

    //**********************************************************************************//
    // ACTION TEXT																		//
    // show/hide text collected by FindTarget.cs and the sprite button 					//
    //**********************************************************************************//
    public void ShowActionText(string name)
	{
		showInteractiveText = true;
		if(controllerInput)
			interactableIcon.sprite = joystickButton;
		else
			interactableIcon.sprite = keyboardButton;
		interactableIcon.enabled = true;
		interactableText.enabled = true;
		interactableText.text = name;
	}
	
	public void HideActionText()
	{
		showInteractiveText = false;
		interactableIcon.enabled = false;
		interactableText.enabled = false;
		interactableText.text = "";
	}

    //**********************************************************************************//
    // EQUIP WEAPON TEXT																//
    // show/hide text collected by FindTarget.cs and the sprite button 					//
    //**********************************************************************************//
    public void ShowEquipText(string name, vMeleeWeapon.MeleeType meleeType, int side)
    {
        if (showEquipText) return;
        showEquipText = true;
     
        if(meleeType == vMeleeWeapon.MeleeType.All)
        {
            equipLeftIcon.sprite = controllerInput ? j_leftSideButton : k_leftSideButton;
            equipRightIcon.sprite = controllerInput ? j_rightSideButton : k_rightSideButton;
            equipLeftIcon.enabled = (side == -1 || side == 2);
            equipRightIcon.enabled = (side == 1 || side == 2);
        }
        else if(meleeType == vMeleeWeapon.MeleeType.Attack)
        {
            equipRightIcon.sprite = controllerInput ? j_rightSideButton : k_rightSideButton;
            equipRightIcon.enabled = (side == 1 || side == 2);
        }
        else
        {
            equipLeftIcon.sprite = controllerInput ? j_leftSideButton : k_leftSideButton;
            equipLeftIcon.enabled = (side == -1 || side == 2);
        }            

        equipText.enabled = true;
        equipText.text = name;
    }

    public void HideEquipText()
    {
        showEquipText = false;
        equipLeftIcon.enabled = false;
        equipRightIcon.enabled = false;
        equipText.enabled = false;
        equipText.text = "";
    }

    //**********************************************************************************//
    // DISPLAY CONTROLS                                                                 //
    // show the hud with the img controls			 									//
    //**********************************************************************************//
    void ChangeInputDisplay()
	{
		#if MOBILE_INPUT
		displayControls.enabled = false;
		#else
		if(controllerInput)		
			displayControls.sprite = joystickControls;		
		else		
			displayControls.sprite = keyboardControls;
		#endif
	}

	//**********************************************************************************//
	// SHOW CHANGE INPUT TEXT															//
	// fadeIn text, show text during 'x' time then fadeOut							    //
	//**********************************************************************************//
    void InitFadeText()
    {
        if (fadeText != null)
        {
            startColor = fadeText.color;
            endColor.a = 0f;
            fadeText.color = endColor;
        }
        else
            Debug.Log("Please assign a Text object on the field Fade Text");
    }
	
	void FadeEffect()
	{
		if(fadeText != null)
		{
			if(fade)
			{
				fadeText.color = Color.Lerp(endColor, startColor, timer);
				
				if(timer < 1)			
					timer += Time.deltaTime/fadeDuration;			
				
				if(fadeText.color.a >= 1)
				{			
					fade = false;
					timer = 0f;
				}
			}
			else
			{
				if(fadeText.color.a >= 1)
					durationTimer += Time.deltaTime;
				
				if(durationTimer >= textDuration)
				{
					fadeText.color = Color.Lerp(startColor, endColor, timer);
					if(timer < 1)			
						timer += Time.deltaTime/fadeDuration;				
				}
			}
		}
	}
	
	public void FadeText(string textToFade, float textTime, float fadeTime)
	{
		if(fadeText != null)
		{
			fadeText.text = textToFade; 	
			textDuration = textTime;	
			fadeDuration = fadeTime;
			durationTimer = 0f;
			timer = 0f;	
			fade = true;
		}
		else
			Debug.Log("Please assign a Text object on the field Fade Text");
	}    
}
