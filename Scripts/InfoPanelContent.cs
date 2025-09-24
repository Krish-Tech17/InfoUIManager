using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class InfoPanelContent : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI bodyText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AssetText(string txt)
    {
        bodyText.text = txt;
    }
}
