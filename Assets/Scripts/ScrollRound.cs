using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;


public class ScrollRound : UIBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IEventSystemHandler, IPointerExitHandler, IPointerEnterHandler
{
    [Serializable] public class OnScrollBegin : UnityEvent { }
    [Serializable] public class OnScroll : UnityEvent<float> { }
    [Serializable] public class OnScrollEnd : UnityEvent { }
    [Serializable] public class OnMovementEnd : UnityEvent { }

    [HideInInspector]
    public OnScrollBegin onScrollBegin;
    [HideInInspector]
    public OnScroll onScroll;
    [HideInInspector]
    public OnScrollEnd onScrollEnd;
    [HideInInspector]
    public OnMovementEnd onMovementEnd;

    public bool inertia = false;
    public float decelerationRate = 0.135f;
    public Transform content;

    private RectTransform m_ViewRect;
    public RectTransform viewRect
    {
        get
        {
            if (m_ViewRect == null)
                m_ViewRect = (RectTransform)transform;
            return m_ViewRect;
        }
    }

    //protected Vector2 m_BeginDragPos;
    //protected Vector3 m_StartDragAngles;
    protected float m_PreEulerAngles;
    protected float m_RotateVelocity;
    protected bool m_Dragging = false;
    protected bool m_MouseIn = false;

    public float velocity
    {
        get
        {
            return m_RotateVelocity;
        }
    }
    

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (this.IsActive())
            {
                //Vector2 pos = eventData.position - eventData.delta;
                //RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, pos, eventData.pressEventCamera, out m_BeginDragPos);
                //m_StartDragAngles = this.content.localEulerAngles;
                this.m_Dragging = true;
                if(this.onScrollBegin != null)
                {
                    this.onScrollBegin.Invoke();
                }
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (this.IsActive() && this.m_Dragging && this.m_MouseIn)
            {
                Vector2 orgVector;
                Vector2 vector;
                bool org = RectTransformUtility.ScreenPointToLocalPointInRectangle(this.viewRect, (eventData.position - eventData.delta), eventData.pressEventCamera, out orgVector);
                bool now = RectTransformUtility.ScreenPointToLocalPointInRectangle(this.viewRect, eventData.position, eventData.pressEventCamera, out vector);
                if (org && now)
                {
                    float angle = Vector2.Angle(orgVector, vector);
                    Vector3 cross = Vector3.Cross(orgVector, vector);
                    angle = cross.z < 0 ? -angle : angle;
                    Vector3 eulerAngles = this.content.localEulerAngles;
                    eulerAngles.z += angle;
                    this.content.localEulerAngles = eulerAngles;
                    if (this.inertia)
                    {
                        float v = angle / Time.unscaledDeltaTime;
                        this.m_RotateVelocity = Mathf.Lerp(this.m_RotateVelocity, v, Time.unscaledDeltaTime * 10f);
                    }
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        this.m_Dragging = false;
        if(this.onScrollEnd != null)
        {
            this.onScrollEnd.Invoke();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        this.m_MouseIn = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        this.m_MouseIn = true;
    }

    protected void LateUpdate()
    {
        if (this.content)
        {
            float unscaledDeltaTime = Time.unscaledDeltaTime;
            if (!this.m_Dragging && this.m_RotateVelocity != 0)
            {
                Vector3 vector3 = this.content.localEulerAngles;
                if (this.inertia)
                {
                    this.m_RotateVelocity = this.m_RotateVelocity * Mathf.Pow(this.decelerationRate, unscaledDeltaTime);
                    if (Mathf.Abs(m_RotateVelocity) < 1f)
                    {
                        this.m_RotateVelocity = 0;
                    }
                    vector3.z = vector3.z + this.m_RotateVelocity * unscaledDeltaTime;
                }
                else
                {
                    this.m_RotateVelocity = 0;
                    if(this.onMovementEnd !=null)
                    {
                        this.onMovementEnd.Invoke();
                    }
                }
                this.content.localEulerAngles = vector3;
            }
            if(this.content.localEulerAngles.z != this.m_PreEulerAngles)
            {
                if(this.onScroll != null)
                {
                    float now = Notmalize(this.content.localEulerAngles.z);
                    float pre = Notmalize(this.m_PreEulerAngles);
                    float offset = now - pre;
                    if (Mathf.Abs(offset) > 180) //穿越180度轴
                    {
                        offset = offset - (offset / Mathf.Abs(offset)) * 360;
                    }
                    this.onScroll.Invoke(offset);
                }
                this.UpdatePrevEulerAngles();
            }
            
        }
    }

    protected void UpdatePrevEulerAngles()
    {
        if (this.content == null)
        {
            this.m_PreEulerAngles = 0;
        }
        else
        {
            this.m_PreEulerAngles = this.content.localEulerAngles.z;
        }
    }

    float Notmalize(float angle)
    {
        return (angle % 360 + 360) % 360;
    }

    public void StopMovement()
    {
        this.m_RotateVelocity = 0;
    }
}
