using System;
using UnityEngine;
using UnityEngine.UI;

namespace DebugTools.ProfilingTool
{
    public class ProfilingTool : MonoBehaviour
    {
        public static readonly int NumberOfCounters = Enum.GetValues(typeof(ProfilingCounterTypes)).Length;

        public static readonly int[] Counters = new int[NumberOfCounters];

        public GameObject Canvas;

        private readonly Text[] CounterTextBoxes = new Text[NumberOfCounters];

        public static void SetCounter(ProfilingCounterTypes counterType, int value)
        {
            Counters[(int) counterType] = value;
        }

        public void Start()
        {
            var textBoxObject = (GameObject) Resources.Load("TextBox");

            var i = 0;
            foreach (var counter in Counters)
            {
                var text = Instantiate(textBoxObject, Canvas.transform).GetComponent<Text>();
                text.text = $"{((ProfilingCounterTypes) i).ToString()}: {counter}";
                CounterTextBoxes[i] = text;
                i++;
            }
        }

        public void Update()
        {
            var profilingCounterType = (ProfilingCounterTypes) 0;
            var height = -5.0f;
            foreach (var counter in Counters)
            {
                var text = CounterTextBoxes[(int) profilingCounterType];
                text.text = $"{profilingCounterType.ToString()}: {counter}";
                var textTransform = (RectTransform) text.transform;
                height -= textTransform.rect.height;
                textTransform.anchoredPosition = new Vector3(5.0f, height, 0.0f);
                profilingCounterType++;
            }
        }
    }
}