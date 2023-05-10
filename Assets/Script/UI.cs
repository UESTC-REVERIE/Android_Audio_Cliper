using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 设计UI，包括提示弹窗等
/// <para>@author reverie</para>
/// </summary>
[System.Serializable]
public class UI : MonoBehaviour
{
    [SerializeField] public InputField inputField;
    [Header("淡出后的颜色")] [SerializeField] private Color targetColor = new Color();
    [Header("淡入淡出的停留时长")][SerializeField] private float waitTime = 2.0f;
    [Header("淡入或淡出的动画时长")][SerializeField] private float fadeTime = 1.0f;
    [SerializeField] public GameObject errorWin;//错误提示窗口
    //[SerializeField] public GameObject tips;//提示窗口
    [SerializeField] private AnimationCurve curve;//曲线
    /// <summary>
    /// 窗口淡出效果
    /// </summary>
    /// <param name="_win"></param>
    public void fadeOut(GameObject _win){
        IEnumerator ie;
        ie = ie_fadeout(_win);
        StartCoroutine(ie);
    }
    /// <summary>
    /// 窗口淡入效果
    /// </summary>
    /// <param name="_win"></param>
    public void fadeIn(GameObject _win){
        IEnumerator ie;
        ie = ie_fadein(_win);
        StartCoroutine(ie);
    }
    public void fade(GameObject _win){
        IEnumerator ie = ie_fade(_win);
        StartCoroutine(ie);
    }
    private IEnumerator ie_fade(GameObject _win){
        fadeOut(_win);
        yield return new WaitForSeconds(waitTime);
        fadeIn(_win);
    }
    private IEnumerator ie_fadeout(GameObject _win){
        float timer = 0.0f;
        Vector3 origin = _win.transform.localScale;
        Color originColor = _win.GetComponent<Image>().color;
        Vector3 target = new Vector3(1,1,1);
        Color _targetColor = targetColor;
        while(timer < fadeTime){
            timer +=Time.deltaTime;
            if(timer > fadeTime) timer = fadeTime;
            float lerp_k = curve.Evaluate(timer / fadeTime);
            _win.transform.localScale = Vector3.Lerp(origin,target,lerp_k);
            _win.GetComponent<Image>().color = Color.Lerp(originColor,_targetColor,lerp_k);
            yield return null;
        }
        
    } 

    private IEnumerator ie_fadein(GameObject _win){
        float timer = 0.0f;
        Vector3 origin = _win.transform.localScale;
        Color originColor = _win.GetComponent<Image>().color;
        Vector3 target = new Vector3(0,0,0);
        Color _targetColor = new Color(originColor.r,originColor.g,originColor.b,0);
        while(timer < fadeTime){
            timer +=Time.deltaTime;
            if(timer > fadeTime) timer = fadeTime;
            float lerp_k = curve.Evaluate(timer / fadeTime);
            _win.transform.localScale = Vector3.Lerp(origin,target,lerp_k);
            _win.GetComponent<Image>().color = Color.Lerp(originColor,_targetColor,lerp_k);
            yield return null;
        }
        
    } 
}
