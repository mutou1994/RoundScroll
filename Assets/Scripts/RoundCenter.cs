using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


//该组件必须与ScrollRound挂在同一个物体上 否则OnBeginDrag与OnEndDrag会检测不到
public class RoundCenter : MonoBehaviour, IEndDragHandler, IBeginDragHandler
{
    public Action<int> onPageCenterCallBack;
    public Action onPageCenterFinished;
    public Action<GameObject> onCenterCallBack;
    public Action onCenterFinished;

    public ScrollRound m_Scroll;
    public RoundLoopLayout m_CircleLayout;

    public bool m_CenterOnStart = true;
    public bool m_AutoCenter = true;
    public bool m_PageCenter = false;
    public int m_PageSize = 4;
    public float m_PageOffset = 150;

    public float m_CenterSpeed = 10;
    public float m_StopSpeed = 100;


    private RectTransform m_ScrollTrans;
    private RectTransform m_CircleLayoutTrans;

    private float m_AngleStep;
    private float m_StepOffset;

    private bool m_Centering = false;
    private bool m_PageCetering = false;
    private bool m_DelayCentering = false;
    private float m_TargetAngle = 0;
    private float m_CurAngle = 0;
    private float m_StartAngleRelaToScroll;
    private int? m_PageIndex = null;

    void Awake()
    {
        m_ScrollTrans = m_Scroll.transform as RectTransform;
        m_CircleLayoutTrans = m_CircleLayout.transform as RectTransform;

        m_StartAngleRelaToScroll = Normalize(Normalize(m_CircleLayout.m_StartAngle) - Normalize(m_CircleLayoutTrans.localEulerAngles.z));
        m_AngleStep = m_CircleLayout.m_CellAngle;
        m_StepOffset = m_AngleStep * 0.5f;

        if(m_PageCenter)
        {
            m_CircleLayout.onInitlizeItem += OnInitlizeItemData; 
        }
    }
    
    void OnDestroy()
    {
        if(m_CircleLayout != null)
        {
            m_CircleLayout.onInitlizeItem -= OnInitlizeItemData;
        }
    }

    void Start()
    {
        if(m_CenterOnStart)
        {
            ReCenter();
        }
    }


    float Normalize(float angle)
    {
        return (angle % 360 + 360) % 360;
    }

    void ReCenter()
    {
        RectTransform target = m_CircleLayout.GetChild(0);
        if(!m_PageCenter)
        {
            float minOffset = 360;
            int count = m_CircleLayout.ChildCount();
            for(int i=0; i < count; i++)
            {
                RectTransform child = m_CircleLayout.GetChild(i);
                float angle = m_CircleLayout.GetItemAngleRelaScroll(child.gameObject);
                float offset = m_StartAngleRelaToScroll - angle;
                if(Mathf.Abs(offset) < Mathf.Abs(minOffset))
                {
                    minOffset = offset;
                    target = child;
                }
                else
                {
                    break;
                }
            }
        }
        
        if(target != null)
        {
            CenterWithObject(target.gameObject);
        }
    }

    public void CenterWithObject(GameObject obj)
    {
        if (obj == null) return;

        float angle = m_CircleLayout.GetItemAngleRelaScroll(obj);
        float offset = m_StartAngleRelaToScroll - angle;
       
        m_CurAngle = m_CircleLayoutTrans.localEulerAngles.z;
        m_TargetAngle = m_CircleLayoutTrans.localEulerAngles.z + offset;
        m_Scroll.StopMovement();
        m_Centering = true;
        if(onCenterCallBack !=null)
        {
            onCenterCallBack(obj);
        }
    }
    
    void OnInitlizeItemData(GameObject obj, int wrapIndex, int realIndex)
    {
        
        //插入到头部  实际上是下一轮尾部的Item
        if(wrapIndex == 0)
        {
            int mode = (realIndex % m_PageSize + m_PageSize) % m_PageSize;
            if(mode == m_PageSize - 1)
            {
                //转了超过一圈
                if(m_PageIndex != null && onPageCenterCallBack != null)
                {
                    onPageCenterCallBack(m_PageIndex.Value);
                }
                m_PageIndex = realIndex;
            }else if(m_PageIndex != null && mode == 0 && m_PageIndex.Value % m_PageSize == 0)
            {
                m_PageIndex = null;
                m_PageCetering = false;
            }
        }else
        {//插入到尾部 实际上是下一轮头部的Item
            int mode = (realIndex % m_PageSize + m_PageSize) % m_PageSize;
            if (mode == 0)
            {
                //转了超过一圈
                if (m_PageIndex != null && onPageCenterCallBack != null)
                {
                    onPageCenterCallBack(m_PageIndex.Value);
                }
                m_PageIndex = realIndex;
            }else if(m_PageIndex != null && mode == m_PageSize -1 && m_PageIndex.Value % m_PageSize != 0)
            {
                m_PageIndex = null;
                m_PageCetering = false;
            }
        }
    }

    void PageCenter()
    {
        int realIndex = m_PageIndex.Value;
        
        int wrapIndex = m_CircleLayout.GetWrapIndexByRealIndex(realIndex);
        RectTransform obj = m_CircleLayout.GetChild(wrapIndex);
        float offset = 0;
        if(realIndex % m_PageSize == 0)
        {
            float angle = m_CircleLayout.GetItemAngleRelaScroll(obj.gameObject);
            if(Mathf.Abs(angle - m_StartAngleRelaToScroll) < m_PageOffset)
            {
                offset = m_StartAngleRelaToScroll - angle;
            }
            else
            {
                float targetAngle = Normalize(m_StartAngleRelaToScroll + m_CircleLayout.m_CellAngle * m_PageSize * (m_CircleLayout.m_IsClockwise ? -1 : 1));
                offset = targetAngle - angle;
                realIndex = realIndex - m_PageSize;
            }
        }
        else
        {
            float angle = m_CircleLayout.GetItemAngleRelaScroll(obj.gameObject);
            float targetAngle = Normalize(m_StartAngleRelaToScroll + m_CircleLayout.m_CellAngle * (m_PageSize - 1) * (m_CircleLayout.m_IsClockwise ? -1 : 1));
            if(Mathf.Abs(angle - targetAngle) < m_PageOffset)
            {
                offset = targetAngle - angle;
            }
            else
            {
                targetAngle = Normalize(m_StartAngleRelaToScroll + m_CircleLayout.m_CellAngle * -(m_CircleLayout.m_IsClockwise ? -1 : 1));
                offset = targetAngle - angle;
                realIndex = realIndex + m_PageSize;
            }
        }

        m_CurAngle = m_CircleLayoutTrans.localEulerAngles.z;
        m_TargetAngle = m_CurAngle + offset;
        m_Scroll.StopMovement();
        m_Centering = true;
        m_PageCetering = true;
        m_PageIndex = null;
        if(onPageCenterCallBack != null)
        {
            onPageCenterCallBack(realIndex);
        }
    }


    void Update()
    {
        if(m_Centering)
        {
            m_CurAngle = Mathf.Lerp(m_CurAngle, m_TargetAngle, m_CenterSpeed * Time.unscaledDeltaTime);
            if (Mathf.Abs(m_CurAngle - m_TargetAngle) < 0.01f)
            {
                m_Centering = false;

                Vector3 v3 = m_CircleLayoutTrans.localEulerAngles;
                v3.z = m_TargetAngle;
                m_CircleLayoutTrans.localEulerAngles = v3;
                if(m_PageCetering)
                {
                    m_PageCetering = false;
                    if(onPageCenterFinished != null)
                    {
                        onPageCenterFinished();
                    }
                    
                    ReCenter();
                }
                else
                {
                    if(onCenterFinished != null)
                    {
                        onCenterFinished();
                    }
                }
            }
            else
            {
                Vector3 v3 = m_CircleLayoutTrans.localEulerAngles;
                v3.z = m_CurAngle;
                m_CircleLayoutTrans.localEulerAngles = v3;
            }
        }
        else if(m_DelayCentering)
        {
            if (Mathf.Abs(m_Scroll.velocity) < this.m_StopSpeed)
            {
                m_Scroll.StopMovement();
                m_DelayCentering = false;
                ReCenter();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        m_Centering = false;
        m_DelayCentering = false;
    }

    public  void OnEndDrag(PointerEventData eventData)
    {
        m_CurAngle = m_CircleLayoutTrans.localEulerAngles.z;
        if (m_PageIndex != null)
        {
            PageCenter();
        }else if(m_PageCetering)
        {
            m_Centering = true;
        }
        else
        {
            m_DelayCentering = m_AutoCenter;
        }
    }

    public void StopCentering()
    {
        m_Centering = false;
        m_DelayCentering = false;
        m_PageCetering = false;
        m_PageIndex = null;
    }
}
