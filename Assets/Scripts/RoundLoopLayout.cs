using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundLoopLayout : MonoBehaviour {

    public Action<GameObject, int, int> onInitlizeItem;

    public ScrollRound m_Scroll;
    
    /// <summary>
    /// 是否顺时针排列
    /// </summary>
    public bool m_IsClockwise = true;
    public bool m_KeepChildRotation = true;
    public float m_StartAngle = 0;

    public float m_CullTopAngle = 0;
    public float m_CullTrailAngle = 360;

    public float m_CellAngle = 45;

    public int topRealIndex
    {
        get;
        private set;
    }
    
    private List<RectTransform> m_ChildItems;
    private RectTransform m_RectTrans;
    private float m_OrgAngle;
    private float m_Radius;

    void Awake()
    {
        InitData();
    }

    float Normalize(float angle)
    {
        return (angle % 360 + 360) % 360;
    }

    void InitData()
    {
        m_RectTrans = transform as RectTransform;
        m_Radius = m_RectTrans.rect.size.x * 0.5f;
        m_OrgAngle = m_RectTrans.localEulerAngles.z;
        m_StartAngle = Normalize(m_StartAngle);
        InitLimitAngle();

        InitChildItems();

        if(m_Scroll != null)
        {
            m_Scroll.onScroll.RemoveAllListeners();
            m_Scroll.onScroll.AddListener(OnWrap);
        }
    }

    void InitLimitAngle()
    {
        m_CullTopAngle = Normalize(m_CullTopAngle - m_OrgAngle);
        m_CullTrailAngle = Normalize(m_CullTrailAngle - m_OrgAngle);
    }

    void InitChildItems()
    {
        m_ChildItems = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            child.anchorMin = Vector2.one * 0.5f;
            child.anchorMax = Vector2.one * 0.5f;
            m_ChildItems.Add(child);
            SetChildPosition(i);
            UpdateItemData(i);
        }
    }

    

    public int ChildCount()
    {
        return m_ChildItems != null ? m_ChildItems.Count : 0;
    }

    public RectTransform GetChild(int index)
    {
        if(m_ChildItems != null && index >=0 && index < m_ChildItems.Count)
        {
            return m_ChildItems[index];
        }
        return null;
    }

    public float GetItemAngleRelaScroll(GameObject go)
    {
        Vector3 pos = m_Scroll.transform.InverseTransformPoint(go.transform.position);
        float angle;
        if (pos.x == 0)
        {
            angle = pos.y > 0 ? 90 : 270;
        }else if(pos.y == 0)
        {
            angle = pos.x > 0 ? 0 : 180;
        }
        else
        {
            float rad = Mathf.Atan(pos.y / pos.x);
            angle = Mathf.Rad2Deg * rad;
            if(angle < 0)
            {
                angle += 180;
            }
            if (pos.y < 0)
            {
                angle += 180;
            }
        }
        return angle;
    }

    public void RefreshChildData()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            SetChildPosition(i);
            UpdateItemData(i);
            if (m_KeepChildRotation)
            {
                child.rotation = Quaternion.identity;
            }
        }
    }

    public void ResetTopRealIndex(int realIndex)
    {
        this.topRealIndex = realIndex;
    }

    public void LocationToRealIndexOnTop(int realIndex)
    {
        ResetTopRealIndex(realIndex);
        Vector3 angles = transform.localEulerAngles;
        float topAngle = m_OrgAngle + (topRealIndex * m_CellAngle) * (m_IsClockwise ? -1 : 1);
        angles.z = -topAngle;
        transform.localEulerAngles = angles;
        RePosition();
        RefreshChildData();
    }

    [ContextMenu("ResetChildPosition")]
    public void ResetPosition()
    {
        if (!Application.isPlaying || m_ChildItems == null)
        {
            InitData();
        }
        else
        {
            LocationToRealIndexOnTop(0);
        }
    }

    public void RePosition()
    {
        for (int i = 0; i < m_ChildItems.Count; i++)
        {
            SetChildPosition(i);
        }
    }

    public int GetRealIndex(int wrapIndex)
    {
        return wrapIndex + topRealIndex;
    }

    public int GetWrapIndexByRealIndex(int realIndex)
    {
        int count = m_ChildItems.Count;
        int index = ((realIndex - topRealIndex) % count + count) % count;
        return index;
    }

    void SetChildPosition(int index)
    {
        if (index < 0 || index >= m_ChildItems.Count) return;
        int realIndex = GetRealIndex(index);
        float angle = m_StartAngle + (realIndex * m_CellAngle) * (m_IsClockwise ? -1 :1);
        float rad = Mathf.Deg2Rad * angle;
        RectTransform child = m_ChildItems[index];
        float x = m_Radius * Mathf.Cos(rad);
        float y = m_Radius * Mathf.Sin(rad);
        child.localPosition = new Vector3(x, y, 0);
    }

    float delta;
    void OnWrap(float delta)
    {
#if UNITY_EDITOR
        InitLimitAngle();
#endif
        if(m_IsClockwise)
        {
            if(delta > 0)
            {
                CheckTop();
            }else
            {
                CheckTrail();
            }
        }
        else
        {
            if(delta > 0)
            {
                CheckTrail();
            }
            else
            {
                CheckTop();
            }
        }

        if(m_KeepChildRotation)
        {
            for(int i=0; i < m_ChildItems.Count; i++)
            {
                m_ChildItems[i].rotation = Quaternion.identity;
            }
        }

    }

    bool CheckTop()
    {
        int count = m_ChildItems.Count;
        bool have = false;
        for(int i = 0; i < count ; i++)
        {
            if(CheckOutOfTop(0))
            {
                RectTransform item = m_ChildItems[0];
                m_ChildItems.RemoveAt(0);
                m_ChildItems.Add(item);
                topRealIndex++;
                int wrapIndex = count - 1;
                SetChildPosition(wrapIndex);
                item.transform.SetAsLastSibling();
                UpdateItemData(wrapIndex);
                have = true;
            }else
            {
                break;
            }
        }
        return have;
    }

    bool CheckOutOfTop(int index)
    {
        RectTransform item = m_ChildItems[index];
        float angle = GetItemAngleRelaScroll(item.gameObject);
       
        if(m_IsClockwise)
        {
            if (m_CullTrailAngle > m_CullTopAngle)
            {
                return angle > m_CullTopAngle && angle <= m_CullTrailAngle;
            }
            else
            {
                return angle > m_CullTopAngle || angle <= m_CullTrailAngle;
            }
        }else
        {
            if(m_CullTrailAngle > m_CullTopAngle)
            {
                return angle < m_CullTopAngle || angle >= m_CullTrailAngle;
            }else
            {
                return angle < m_CullTopAngle && angle >= m_CullTrailAngle;
            }
        }
    }

    bool CheckTrail()
    {
        int count = m_ChildItems.Count;
        bool have = false;
        for (int i = 0; i < m_ChildItems.Count; i++)
        {
            if (CheckOutOfTrail(count - 1))
            {
                RectTransform item = m_ChildItems[count - 1];
                m_ChildItems.RemoveAt(count - 1);
                m_ChildItems.Insert(0, item);
                topRealIndex--;
                int wrapIndex = 0;
                SetChildPosition(wrapIndex);
                item.transform.SetAsFirstSibling();
                UpdateItemData(wrapIndex);
                have = true;
            }
            else
            {
                break;
            }
        }
        return have;
    }

    bool CheckOutOfTrail(int index)
    {
        RectTransform item = m_ChildItems[index];
        float angle = GetItemAngleRelaScroll(item.gameObject);

        if (m_IsClockwise)
        {
            if (m_CullTrailAngle > m_CullTopAngle)
            {
                return angle < m_CullTrailAngle && angle >= m_CullTopAngle;
            }
            else
            {
                return angle < m_CullTrailAngle || angle >= m_CullTopAngle;
            }
        }
        else
        {
            if (m_CullTrailAngle > m_CullTopAngle)
            {
                return angle > m_CullTrailAngle || angle <= m_CullTopAngle;
            }
            else
            {
                return angle > m_CullTrailAngle && angle <= m_CullTopAngle;
            }
        }
    }

    void UpdateItemData(int wrapIndex)
    {
        RectTransform item = m_ChildItems[wrapIndex];
        item.Find("text").GetComponent<UnityEngine.UI.Text>().text = GetRealIndex(wrapIndex).ToString();
        if(onInitlizeItem != null)
        {
            onInitlizeItem(item.gameObject, wrapIndex, GetRealIndex(wrapIndex));
        }
    }
}
