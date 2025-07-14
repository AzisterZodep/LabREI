using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Title : MonoBehaviour
{
    public Text txt;
    public string defout;
    public Color defoutColor;
    public Color emphasisColor;

    void Start(){
        Reset();
    }
    public void Reset(){
        Set(defout);
        SetColor(false);
    }
    public void Set(string t){
        txt.text = t;
    }
    public void SetColor(bool e){
        if(e)
            txt.color = emphasisColor;
        else
            txt.color = defoutColor;
    }
}
