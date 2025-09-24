using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class InfoIconManager : MonoBehaviour
{
    [SerializeField] InfoiconInteractable[] popupInteractables;

    public static Action<InfoiconInteractable,bool> OnPopupInteractiveChanged;

    public static InfoIconManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        OnPopupInteractiveChanged += InfoInteractiveChange;
        // popupInteractables = FindObjectsOfType<PopupInteractable>(true);
             
    }


    private void InfoInteractiveChange(InfoiconInteractable arg1, bool arg2)
    {
        Debug.Log(popupInteractables.Length+" is length");
        bool isPopupChanged = false;
        for (int i = 0; i < popupInteractables.Length; i++)
        {
            if (arg1 == popupInteractables[i])
            {
                
                
                Debug.Log("Popup ID is " + i);
                isPopupChanged = true;
               // break;
            }
            else
            {
                popupInteractables[i].DisableUI();
            }
        }
        if (!isPopupChanged)
        {
            Debug.LogError("Not Popuped - ID not found");
        }
    }

   

    // Update is called once per frame
    void Update()
    {
        
    }



    [ContextMenu("AssignData")]
    [Obsolete]
    void AssignData()
    {
        Debug.Log("Perform operation");
        popupInteractables = FindObjectsOfType<InfoiconInteractable>(true);
        foreach (var item in popupInteractables)
        {
            item.AssignRefs();
        }
    }

   


    

    



}
