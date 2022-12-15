using SonicBloom.Koreo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [EventID]
    public string eventID;

    private float laneNum;

    private void OnEnable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.RegisterForEvents(eventID, TransformGameObject);
        }
    }

    private void OnDisable()
    {
        if (Koreographer.Instance != null)
        {
            Koreographer.Instance.UnregisterForAllEvents(this);
        }
    }

    private void TransformGameObject(KoreographyEvent evt)
    {
        laneNum = Mathf.Sin(Mathf.PI * 0.5f * (evt.GetIntValue() / 127f)) * 5;
        transform.localScale = Vector3.one * laneNum;
    }
}
