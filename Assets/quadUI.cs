using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class quadUI : MonoBehaviour
{
    public bool auto = true;
    public bool hasFather = false;
    RectTransform ObjTrans;
    float ScrSql;
    float W;
    public float wdif = 1f;
    float H;
    RectTransform parentTrans;

    void Awake()
    {
        ObjTrans = this.GetComponentInParent<RectTransform>();
        if (hasFather){
            parentTrans = ObjTrans.parent as RectTransform;
        }
        Updatequad();
    }

    void OnRectTransformDimensionsChange()
    {
        
        if (auto&&ObjTrans) Updatequad();
    }

    public void Updatequad()
    {
        
        if (hasFather)
        {
            ScrSql = parentTrans.rect.height;
            if (ScrSql > parentTrans.rect.width)
            {
                ScrSql = parentTrans.rect.width;
            }
            W = ((parentTrans.rect.width - ScrSql) / 2) / parentTrans.rect.width*wdif;
            H = ((parentTrans.rect.height - ScrSql) / 2) / parentTrans.rect.height;
        }
        else
        {
            ScrSql = Screen.height;
            if (ScrSql > Screen.width)
            {
                ScrSql = Screen.width;
            }
            W = ((Screen.width - ScrSql) / 2) / Screen.width*wdif;
            H = ((Screen.height - ScrSql) / 2) / Screen.height;
        }
        
        ObjTrans.anchorMin = new Vector2(W, H);
        ObjTrans.anchorMax = new Vector2(1 - W, 1 - H);
    }
}