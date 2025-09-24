using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class InfoiconInteractable : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    [SerializeField] string info_Content;
    public Outline outline;
    public Outline[] outlineObjs;
    public GameObject detailsUI;
    public GameObject infoIcon;
    public bool clicked;
    [SerializeField] bool isPhone;
    [SerializeField] Image fillerImage;
    Coroutine timerCoroutine;
    float currCountdownValue;

    [Obsolete]
    void Start()
    {
        if (interactionManager == null)
        {
            interactionManager = FindObjectOfType<XRInteractionManager>();
        }
        hoverEntered.AddListener(HoverEntered);
        hoverExited.AddListener(HoverExited);
        selectEntered.AddListener(SelectEntered);
        /*if (outline!=null)
        {
            outline.enabled = false;
        }
            if (outline==null)
        {
          //  outline = GetComponentInChildren<Outline>();
        }*/
        if (infoIcon == null)
        {
          //  infoIcon= transform.Find("IconAnimObj").gameObject;
        }
        if (detailsUI == null)
        {
         //  detailsUI = GetComponentInChildren<Canvas>(true).gameObject;
        }
        OnHoverLazer(false);
        if (detailsUI!=null)
        {
            detailsUI.SetActive(false);
        }
        if (infoIcon!=null)
        {
            infoIcon.SetActive(true);
        }
        if (fillerImage==null)
        {
          fillerImage=  infoIcon.GetComponentInChildren<Image>(true);
            if (fillerImage!=null)
            {
               
            }
        }
       
    }

    public bool isCurrentObj;

    private void SelectEntered(SelectEnterEventArgs selectEnterEvent)
    {
        OnEnableUI();
        InfoIconManager.OnPopupInteractiveChanged(this, clicked);
    }

    private void HoverExited(HoverExitEventArgs hoverExitEvent)
    {
        OnHoverLazer(false);
       //// DisableUI();
   
    }

    private void HoverEntered(HoverEnterEventArgs hoverEnteredEvent)
    {
        OnHoverLazer(true);
      //  timerCoroutine = StartCoroutine(StartCountdown(60));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            OnHoverLazer(true);
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            OnHoverLazer(false);
        }
    }

    public void OnHoverLazer(bool value)
    {
        if (outline == null)
            return;

        if (isPhone)
        {
            foreach (Outline obj in outlineObjs)
            {
                //obj.enabled = value; 
                obj.enabled = false; // Temp fix need to check outline, then this shoud delete
            }
        }
        else
        {
            //outline.enabled = value;
            outline.enabled = false; // Temp fix need to check outline, then this shoud delete
        }
    }

    public void OnEnableUI()
    {
       // if (infoIcon != null)
           // infoIcon.SetActive(clicked);
        Debug.Log("clicked = " + clicked);
        clicked = !clicked;
        if(detailsUI != null)
            detailsUI.SetActive(clicked);
    }

    public void AssignRefs()
    {
        Debug.Log("Perform operation");
       /* if (outline == null)
        {
            outline = GetComponentInChildren<Outline>();
        }*/
        if (infoIcon == null)
        {
            infoIcon = transform.Find("IconAnimObj").gameObject;//
            
        }
        if (detailsUI == null)
        {
            detailsUI = GetComponentInChildren<Canvas>(true).gameObject;
        }
        if (!string.IsNullOrWhiteSpace(info_Content))
        {
            detailsUI.GetComponent<InfoPanelContent>().AssetText(info_Content);
        }
    }

    public void DisableUI()
    {
        if (detailsUI!=null)
        {
            detailsUI.SetActive(false);
        }
        if (infoIcon!=null)
        {
            infoIcon.SetActive(true);
        }
        clicked = false;
      
        print("Diable UI called");
    }

    public IEnumerator StartCountdown(float countdownValue = 120)
    {
        if(fillerImage!= null)
            fillerImage.gameObject.SetActive(true);

        currCountdownValue = countdownValue;
        while (currCountdownValue > 0)
        {
            Debug.Log("Countdown: " + currCountdownValue);
            float fillerValue = Mathf.Abs( (currCountdownValue / countdownValue) - 1);
            print(fillerValue);
            fillerImage.fillAmount = fillerValue;
            yield return null;
            currCountdownValue--;
        }
        print("CountDownCompleted");
        fillerImage.fillAmount = 1;
        OnEnableUI();
    }

    void CancelCountDown()
    {
        StopCoroutine(timerCoroutine);
        fillerImage.gameObject.SetActive(false);
    }
}
