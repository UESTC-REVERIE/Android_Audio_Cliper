using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对数值的监听
/// <para>@author reverie</para>
/// </summary>
[System.Serializable]
public class Listener
{
    public delegate void listener(float val);
    public event listener onBeginValueChanged;
    public event listener onEndValueChanged;
    [SerializeField] private float _beginTime = 0.0f;
    [SerializeField] private float _endTime = 0.0f;
    public float beginTime{
        get{
            return _beginTime;
        }
        set{
            if(_beginTime == value) return;

            onBeginValueChanged?.Invoke(value);
            
            _beginTime = value;
        }
    }
    
    public float endTime{
        get{
            return _endTime;
        }
        set{
            if(_endTime == value) return;

            onEndValueChanged?.Invoke(value);

            _endTime = value;
            
        }
    }

}
