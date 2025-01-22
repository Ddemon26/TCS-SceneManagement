using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
namespace TCS.Bootstrapper {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class TipJsonData {
        public string title;
        [TextArea(2, 6)]
        public string info;
    }
    [Serializable]
    public class LoadingScreenSetup {
        public TextAsset m_tipsJson;
        public Sprite[] m_backgrounds;
        public TipJsonData[] m_textTips;
        
        Sprite m_lastBackground;
        TipJsonData m_lastText;

        public void SetTipsArray() {
            if (!m_tipsJson) return;
            var serializer = new JsonSerializer();
            var wrapper = serializer.DeserializeFromTextAsset<TipsWrapper>(m_tipsJson);
            m_textTips = wrapper.tips;
        }

        /// <summary>
        /// Gets a random background sprite from the available backgrounds.
        /// </summary>
        /// <returns>A random Sprite that is not the same as the last returned Sprite.</returns>
        public Sprite GetRandomBackground() {
            m_lastBackground = GetRandomElement(m_backgrounds, m_lastBackground);
            return m_lastBackground;
        }

        /// <summary>
        /// Gets a random loading text tip from the available tips.
        /// </summary>
        /// <returns>A random string that is not the same as the last returned string.</returns>
        public TipJsonData GetRandomLoadingText() {
            m_lastText = GetRandomElement(m_textTips, m_lastText);
            return m_lastText;
        }

        /// <summary>
        /// Gets a random element from the provided array that is not equal to the last element.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="elements">The array of elements to choose from.</param>
        /// <param name="lastElement">The last element that was chosen.</param>
        /// <returns>A random element from the array that is not equal to the last element.</returns>
        static T GetRandomElement<T>(T[] elements, T lastElement) {
            switch (elements.Length) {
                case 0:
                    return default;
                case 1:
                    return elements[0];
                case 2:
                    return Equals(elements[0], lastElement) ? elements[1] : elements[0];
            }
            T randomElement;
            do {
                randomElement = elements[UnityEngine.Random.Range(0, elements.Length)];
            } while (Equals(randomElement, lastElement));
            return randomElement;
        }
        #region JsonData
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Serializable]
        class TipsWrapper {
            public TipJsonData[] tips;
        }
        #endregion
    }
}