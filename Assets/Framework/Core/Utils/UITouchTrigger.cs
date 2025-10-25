using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridGame
{
    public class UITouchTrigger
        : MonoBehaviour,
            IBeginDragHandler,
            IDragHandler,
            IEndDragHandler,
            IPointerDownHandler,
            IPointerUpHandler,
            IPointerClickHandler,
            IInitializePotentialDragHandler

    {

        static public void AddEvent(GameObject gameObject, EventType eventType, Action<PointerEventData> callback)
        {
            var eventTrigger = gameObject.GetComponent<UITouchTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = gameObject.AddComponent<UITouchTrigger>();
            }
            switch (eventType)
            {
                case EventType.Down:
                    eventTrigger.Down += callback;
                    break;
                case EventType.InitDrag:
                    eventTrigger.InitDrag += callback;
                    break;
                case EventType.StartDrag:
                    eventTrigger.StartDrag += callback;
                    break;
                case EventType.Drag:
                    eventTrigger.Drag += callback;
                    break;
                case EventType.Up:
                    eventTrigger.Up += callback;
                    break;
                case EventType.Click:
                    eventTrigger.Click += callback;
                    break;
                case EventType.EndDrag:
                    eventTrigger.EndDrag += callback;
                    break;
            }
        }
        static public void RemoveEvent(GameObject gameObject, EventType eventType, Action<PointerEventData> callback)
        {
            var eventTrigger = gameObject.GetComponent<UITouchTrigger>();
            if (eventTrigger != null)
            {
                switch (eventType)
                {
                    case EventType.Down:
                        eventTrigger.Down -= callback;
                        break;
                    case EventType.StartDrag:
                        eventTrigger.StartDrag -= callback;
                        break;
                    case EventType.Drag:
                        eventTrigger.Drag -= callback;
                        break;
                    case EventType.Up:
                        eventTrigger.Up -= callback;
                        break;
                    case EventType.Click:
                        eventTrigger.Click -= callback;
                        break;
                    case EventType.EndDrag:
                        eventTrigger.EndDrag -= callback;
                        break;
                }
            }
        }


        //ִ��˳��---1
        // ��ui�ɽ��շ�Χ�� ������Ļ                           
        public void OnPointerDown(PointerEventData eventData)
        {
            if (Down != null) Down.Invoke(eventData);
        }
        //ִ��˳��---2
        // ��ui�ɽ��շ�Χ�� ������Ļ                           
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (InitDrag != null) InitDrag.Invoke(eventData);
        }
        //ִ��˳��---3
        // ��ָ���º� ��ui�ɽ��շ�Χ��,��ָ�״��ƶ�ʱ����һ��         
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (StartDrag != null) StartDrag.Invoke(eventData);
        }
        //ִ��˳��---4
        // ����BeginDrag������Ļ���ƶ�������������ָ�뿪��Ļֹͣ     
        public void OnDrag(PointerEventData eventData)
        {
            if (Drag != null) Drag.Invoke(eventData);
        }
        //ִ��˳��---5
        // ֻҪ������Down������ָ�뿪��Ļʱ�ᴥ��                    
        public void OnPointerUp(PointerEventData eventData)
        {
            if (Up != null) Up.Invoke(eventData);
        }
        //ִ��˳��---6
        // ��ui�ɽ��շ�Χ�� ���������¼��� ���Ҵ���̧���¼�           
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Click != null) Click.Invoke(eventData);
        }
        //ִ��˳��---7
        // ������ק������ָ�뿪��Ļʱ�ᴥ��  ��up֮ǰ                 
        public void OnEndDrag(PointerEventData eventData)
        {
            if (EndDrag != null) EndDrag.Invoke(eventData);
        }

        event Action<PointerEventData> Down;
        event Action<PointerEventData> InitDrag;
        event Action<PointerEventData> StartDrag;
        event Action<PointerEventData> Drag;
        event Action<PointerEventData> Up;
        event Action<PointerEventData> Click;
        event Action<PointerEventData> EndDrag;

        public enum EventType
        {
            /// <summary> ����(��ui��) </summary>
            Down,
            /// <summary> ����(��ui��) </summary>
            InitDrag,
            /// <summary> ��ʼ�϶�(��ui��) </summary>
            StartDrag,
            /// <summary> �϶�(��Ļ��) </summary>
            Drag,
            /// <summary> ̧��(�뿪��Ļ) </summary>
            Up,
            /// <summary> ���(��ui�ڰ��²��뿪��Ļ) </summary>
            Click,
            /// <summary> �����϶�(�뿪��Ļ) </summary>
            EndDrag,
        }

    }

}