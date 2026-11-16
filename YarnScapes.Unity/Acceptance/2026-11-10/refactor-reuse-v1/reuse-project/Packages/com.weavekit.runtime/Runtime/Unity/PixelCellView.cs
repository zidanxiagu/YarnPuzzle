using System;
using UnityEngine;
using UnityEngine.UI;

namespace WeaveKit.Unity
{
    public sealed class PixelCellView : MonoBehaviour
    {
        private Image image;
        private Text label;
        private Button button;
        private Color baseColor = Color.white;
        private int cellIndex;
        private Action<int> clicked;
        private UiFeedbackAnimator feedback;

        public void Initialize(int index, Image targetImage, Text targetLabel, Button targetButton, Action<int> onClicked)
        {
            cellIndex = index;
            image = targetImage;
            label = targetLabel;
            button = targetButton;
            clicked = onClicked;
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        public void SetUnfilled(Sprite sprite, Color tint, string text)
        {
            image.enabled = true;
            image.sprite = sprite;
            image.color = tint;
            label.text = text;
            label.color = new Color(0.12f, 0.13f, 0.15f, 1f);
            label.fontStyle = FontStyle.Bold;
            baseColor = tint;
            if (button != null) button.interactable = true;
        }

        public void SetFilled(Sprite sprite, Color tint)
        {
            image.enabled = true;
            image.sprite = sprite;
            image.color = tint;
            label.text = string.Empty;
            label.color = new Color(1f, 1f, 1f, 0f);
            baseColor = tint;
        }

        public void SetEmpty(Sprite sprite)
        {
            image.sprite = sprite;
            image.color = new Color(1f, 1f, 1f, 0f);
            label.text = string.Empty;
            label.color = new Color(1f, 1f, 1f, 0f);
            if (button != null) button.interactable = false;
            baseColor = image.color;
        }

        public void SetFilledGeometry()
        {
            image.enabled = false;
            label.text = string.Empty;
            if (button != null) button.interactable = false;
        }

        public void SetInteractable(bool value)
        {
            if (button != null) button.interactable = value;
        }

        public void SetLabelColor(Color color) { label.color = color; }

        public void ResetAnimation()
        {
            if (feedback != null) feedback.Cancel();
            transform.localScale = Vector3.one;
        }

        public void Pulse(float seconds)
        {
            Feedback.Pulse(transform, seconds);
        }

        public void Flash(float seconds)
        {
            Feedback.Flash(image, baseColor, seconds);
        }

        private void HandleClick()
        {
            clicked?.Invoke(cellIndex);
        }

        private UiFeedbackAnimator Feedback => feedback != null ? feedback : (feedback = gameObject.AddComponent<UiFeedbackAnimator>());
        private void OnDisable() { if (feedback != null) feedback.Cancel(); }
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            if (feedback != null) Destroy(feedback);
        }
    }
}
