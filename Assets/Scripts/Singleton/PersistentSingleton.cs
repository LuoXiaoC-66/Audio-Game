using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
{
    private static T _instance = null;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType(typeof(T)) as T;
                if (_instance == null)
                {
                    _instance = new GameObject("_" + typeof(T).Name).AddComponent<T>();
                }
                DontDestroyOnLoad(_instance);
                if (_instance == null)
                    Debug.Log("Failed to create instance of " + typeof(T).FullName + ".");
            }
            return _instance;
        }
    }
}
